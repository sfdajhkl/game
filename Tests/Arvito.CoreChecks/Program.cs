using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Arvito.Core;

static class Program
{
    static readonly JsonSerializerOptions Json = new JsonSerializerOptions { IncludeFields = true };
    static Catalog data;
    static int passed;
    static void Assert(bool value, string reason = "assertion") { if (!value) throw new Exception(reason); }
    static void Check(string name, Action action) { action(); passed++; Console.WriteLine("PASS " + name); }
    static Game New(uint seed = 42) => Game.New(data, "루엔", seed);
    static Game Story(string id, uint seed = 42) { var g = New(seed); g.State.eventId = id; g.State.region = data.Event(id).regions.FirstOrDefault() ?? g.State.region; return g; }
    static Game Battle(uint seed = 42)
    {
        var g = New(seed); g.State.eventId = "first_hunt"; g.Apply(new Effects { recruit = "mira" });
        g.StartCombat(new[] { "wolf", "wolf" }, new Outcome { title = "첫 전투", body = "승리", effects = new Effects { gold = 16, xp = 20 }, next = "crossroads" }); return g;
    }
    static bool AutoAction(Game g)
    {
        var a = g.Actor(g.State.combat.active);
        string id = g.Skills(a).FirstOrDefault(k => data.Skill(k).power > 0 && g.SkillLock(k) == "") ?? "guard";
        if (a.hp < a.maxHp * .4 && g.State.potions > 0) id = "potion";
        return g.Act(id, id == "potion" ? a.id : g.Targets(id)[0]);
    }
    static string Encode(GameState s) => JsonSerializer.Serialize(s, Json);
    static GameState Decode(string s) { try { return JsonSerializer.Deserialize<GameState>(s, Json); } catch (JsonException) { return null; } }
    static void Main(string[] args)
    {
        data = JsonSerializer.Deserialize<Catalog>(File.ReadAllText(args[0]), Json);
        data.Normalize();
        Check("catalog: 500 events, 10 regions, 7 weapons, 3 companions, 18 skills", () => { Assert(data.events.Length == 500 && data.regions.Length == 10); Assert(data.weapons.Length == 7); Assert(data.companions.Length == 3); Assert(data.skills.Length == 18); foreach (var e in data.events) foreach (var c in e.choices) Assert(c.weeks >= 1); });
        Check("starts at sixteen with no fixed class", () => { var g = New(); Assert(g.Age == 16 && g.State.hero.weapon == "sword"); Assert(Game.Valid(g.State, data)); });
        Check("recruitment event remains available without a forced next event", () => { var g = Story("departure"); Assert(g.Choose(0)); Assert(g.Party.Count == 2 && g.State.weeks == 1); g.Continue(); Assert(g.State.mode == "story" && Game.Valid(g.State,data)); });
        Check("resource gate does not mutate time or RNG", () => { var g = New(); g.State.eventId = "market"; g.State.gold = 0; var before = Encode(g.State); Assert(!g.Choose(0)); Assert(before == Encode(g.State)); });
        Check("duplicate recruitment / maximum party / tank joins front", () => { var g = New(); foreach (string id in new[] { "mira", "elian", "borin", "mira" }) g.Apply(new Effects { recruit = id }); Assert(g.Party.Count == 4 && g.State.formation[0] == "borin"); });
        Check("weekly mortality increases with age", () => { Assert(Game.WeeklyRisk(39) == 0); for (int age = 40; age < 100; age++) Assert(Game.WeeklyRisk(age + 1) >= Game.WeeklyRisk(age)); });
        Check("long time jump equals individual weeks", () => { var a = New(91); var b = New(91); a.State.weeks = b.State.weeks = 44 * 52; a.AdvanceTime(104); for (int i = 0; i < 104 && b.State.mode != "ended"; i++) b.AdvanceTime(1); Assert(a.State.weeks == b.State.weeks && a.State.rng == b.State.rng); });
        Check("death interrupts long event before rewards", () => { var g = Story("quiet_years"); g.State.rng=87; g.State.weeks = 90 * 52; g.Choose(0); Assert(g.State.mode == "ended" && g.State.gold == 24); });
        Check("reunion waits two years after a late child rescue, including after loading", () => {
            var g = Story("lost_child"); g.State.weeks = 1040; g.State.region = "village";
            Assert(!g.AvailableEvents().Any(e => e.id == "reunion")); g.Choose(0); int rescuedAt = g.State.weeks;
            Assert(!g.AvailableEvents().Any(e => e.id == "reunion"));
            g = new Game(data, Decode(Encode(g.State))); Assert(Game.Valid(g.State, data));
            g.State.weeks = rescuedAt + 103; Assert(!g.AvailableEvents().Any(e => e.id == "reunion"));
            g.State.weeks++; Assert(g.AvailableEvents().Any(e => e.id == "reunion"));
        });
        Check("old saves recover rescue timing from the journal or conservatively wait", () => {
            var g = Story("lost_child"); g.State.weeks = 1040; g.State.region = "village"; g.Choose(0); int rescuedAt = g.State.weeks;
            g.State.weeks += 104; g.State.flagWeeks = null; Assert(Game.Valid(g.State, data));
            var restored = new Game(data, Decode(Encode(g.State)));
            Assert(Game.Count(restored.State.flagWeeks, "saved_child") == rescuedAt && restored.AvailableEvents().Any(e => e.id == "reunion"));
            g.State.journal.Clear(); restored = new Game(data, Decode(Encode(g.State)));
            Assert(!restored.AvailableEvents().Any(e => e.id == "reunion"));
            restored.State.weeks += 104; Assert(restored.AvailableEvents().Any(e => e.id == "reunion"));
        });
        Check("lethal event failure ends life before level-up rewards", () => {
            var g = Story("bridge"); g.State.rng=123456789; g.State.hero.hp = 1; g.State.hero.xp = 40;
            Assert(g.Choose(0)); Assert(g.State.mode == "ended" && g.State.ending.cause == "wounds" && g.State.hero.hp == 0);
            Assert(g.State.hero.level == 1 && g.State.hero.xp == 40 && Game.Valid(g.State, data));
            var before = Encode(g.State); g.Apply(new Effects { fullHeal = true, xp = 100 }); Assert(Encode(g.State) == before);
        });
        Check("full recovery reaches maximum after level-up and never revives companions", () => {
            var g = New(); g.Apply(new Effects { recruit = "mira" }); var m = g.Actor("mira"); m.hp = 0;
            g.State.fallen.Add("mira"); g.State.formation.Remove("mira");
            g.State.hero.maxHp = 114; g.State.hero.hp = 1; g.State.hero.level = 20; g.State.hero.xp = 890;
            g.State.eventId = "mentor"; g.Choose(1);
            Assert(g.State.hero.level == 21 && g.State.hero.hp == 118 && g.State.hero.hp == g.State.hero.maxHp);
            Assert(g.State.outcome.changes.Contains("일행 완전 회복") && m.hp == 0 && g.Party.Count == 1);
        });
        Check("numeric full-heal effects in old pending combat saves remain supported", () => {
            var g = New(); g.State.hero.maxHp = 114; g.State.hero.hp = 1;
            g.StartCombat(new[] { "wolf" }, new Outcome { title = "old save", effects = new Effects { heal = 99 } });
            var restored = new Game(data, Decode(Encode(g.State))); restored.Apply(restored.State.combat.pending.effects);
            Assert(restored.State.hero.hp == 114);
        });
        Check("weapon switch retains learned skill", () => { var g = New(); g.Apply(new Effects { weapon = "staff", skill = "rally" }); g.Equip("staff"); Assert(g.Skills(g.State.hero).Contains("ember") && g.Skills(g.State.hero).Contains("rally") && !g.Skills(g.State.hero).Contains("slash")); });
        Check("rank, target and energy gates reject invalid turns", () => { var g = Battle(); g.State.combat.active = "hero"; g.State.hero.weapon = "bow"; var before = Encode(g.State); Assert(!g.Act("shot", "enemy_0") && before == Encode(g.State)); g.State.hero.weapon = "sword"; Assert(!g.Act("slash", "mira")); g.State.combat.active = "mira"; g.Actor("mira").energy = 0; Assert(!g.Act("pin", "enemy_0")); });
        Check("position swap consumes a turn and companions automatically continue", () => { var g = Battle(); g.State.combat.active = "hero"; g.State.combat.queue = new System.Collections.Generic.List<string> { "mira", "hero" }; Assert(g.Act("move", "mira")); Assert(g.State.combat.active == "hero" && g.State.combat.log.Any(x=>x.Contains("미라"))); });
        Check("potion consumes one item and cures bleed", () => { var g = Battle(); g.State.combat.active = "hero"; g.State.combat.queue = new System.Collections.Generic.List<string> { "mira" }; g.State.hero.hp -= 3; g.State.hero.status.bleed = 3; g.Act("potion", "hero"); Assert(g.State.potions == 2 && g.State.hero.hp == 38 && g.State.hero.status.bleed == 0); });
        Check("repeated choice cannot duplicate rewards", () => { var g = Story("departure"); g.Choose(0); var before = Encode(g.State); Assert(!g.Choose(0) && before == Encode(g.State)); });
        Check("30 seeded battles reach victory with hero-only commands", () => { for (uint seed = 1; seed <= 30; seed++) { var g = Battle(seed); int turns = 0; while (g.State.mode == "combat" && turns++ < 100) { Assert(g.State.combat.active=="hero"); Assert(AutoAction(g)); } Assert(g.State.mode == "outcome" && g.State.victories == 1 && g.State.gold == 40, "seed " + seed); g.Continue(); Assert(g.State.mode=="story"); } });
        Check("battle save preserves next actor and RNG", () => { var a = Battle(9); AutoAction(a); var b = new Game(data, Decode(Encode(a.State))); Assert(Game.Valid(b.State, data)); AutoAction(a); AutoAction(b); Assert(Encode(a.State) == Encode(b.State)); });
        Check("broken combat snapshots are rejected", () => { var g = Battle(); g.State.combat.enemies[0].status = null; Assert(!Game.Valid(g.State, data)); g = Battle(); g.State.combat.queue.Add("missing_enemy"); Assert(!Game.Valid(g.State, data)); });
        Check("bleed can end the hero's life", () => { var g = Battle(); g.State.hero.hp = 2; g.State.hero.status.bleed = 1; g.State.combat.active = "hero"; g.State.combat.queue = new System.Collections.Generic.List<string> { "hero" }; g.Act("guard", "hero"); Assert(g.State.mode == "ended" && g.State.ending.cause == "battle"); });
        Check("companion death is permanent and closes formation", () => { var g = Battle(); var m = g.Actor("mira"); m.hp = 2; m.status.bleed = 1; g.State.combat.active = "hero"; g.State.combat.queue = new System.Collections.Generic.List<string> { "mira", "hero" }; g.Act("guard", "hero"); Assert(g.State.fallen.Contains("mira") && g.Party.Count == 1); g.Apply(new Effects { recruit = "mira" }); Assert(g.Party.Count == 1); });
        Check("stun / guard expire at actor turn", () => { var g = Battle(); g.Actor("mira").status.stun = 1; g.State.combat.active = "hero"; g.State.combat.queue = new System.Collections.Generic.List<string> { "mira", "hero" }; g.Act("guard", "hero"); Assert(g.State.combat.active == "hero" && !g.State.hero.status.guard && g.Actor("mira").status.stun == 0); });
        Check("stress breakdown loses one turn", () => { var g = Battle(); g.Actor("mira").stress = 100; g.State.combat.active = "hero"; g.State.combat.queue = new System.Collections.Generic.List<string> { "mira", "hero" }; g.Act("guard", "hero"); Assert(g.State.combat.active == "hero" && g.Actor("mira").stress == 65); });
        Check("retreat has cost and no victory reward", () => { var g = Battle(); g.State.gold = 31; g.Flee(); Assert(g.State.gold == 24 && g.State.victories == 0 && g.State.mode == "outcome"); });
        Check("camp heals, spends supplies and advances time", () => { var g = New(); g.State.hero.hp = 10; g.State.hero.stress = 35; g.Camp(); Assert(g.State.rations == 3 && g.State.weeks == 1 && g.State.hero.stress == 15 && g.State.hero.hp > 10); });
        Check("atomic save and corrupted-primary backup recovery", () => {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "arvito-test-" + Guid.NewGuid()); string file = System.IO.Path.Combine(dir, "save.json");
            try { var store = new SaveStore(file, Encode, Decode, s => Game.Valid(s, data)); var g = Story("departure"); store.Save(g.State); g.Choose(0); store.Save(g.State); Assert(store.Load(out _)?.weeks == 1); File.WriteAllText(file, "{broken"); var restored = store.Load(out string notice); Assert(restored != null && restored.weeks == 0 && notice.Length > 0); }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        });
        WorldChecks();
        Console.WriteLine($"{passed} C# checks passed.");
    }
    static void WorldChecks()
    {
        Check("starts vary from the very first event across five starting regions",()=>{
            var starts=new System.Collections.Generic.HashSet<string>();var regions=new System.Collections.Generic.HashSet<string>();
            var frequency=new System.Collections.Generic.Dictionary<string,int>();
            for(uint i=1;i<=400;i++){var g=New(unchecked(i*2654435761u));starts.Add(g.State.eventId);regions.Add(g.State.region);frequency[g.State.eventId]=frequency.ContainsKey(g.State.eventId)?frequency[g.State.eventId]+1:1;Assert(g.Event.regions.Length==0||g.Event.regions.Contains(g.State.region));Assert(g.Event.minStage==0&&Game.Valid(g.State,data));}
            Assert(starts.Count>=15&&regions.Count==5,"Too few starting possibilities: "+starts.Count);
            Assert(frequency.Values.Max()<120,"A single opening event dominates the pool.");
        });
        Check("events obey region, stage, flag and repetition conditions",()=>{
            var g=Story("spring");g.State.region="forest";Assert(g.AvailableEvents().All(e=>e.regions.Length==0||e.regions.Contains("forest")));
            Assert(!g.AvailableEvents().Any(e=>e.minStage>0||!string.IsNullOrEmpty(e.requiredFlag)));
            g.State.recent.Add("spring");Assert(!g.AvailableEvents().Any(e=>e.id=="spring"));
            g.State.region="frontier";g.State.hero.level=7;Assert(g.AvailableEvents().Any(e=>e.minStage==2));
        });
        Check("weights change with progression and repeated events lose weight",()=>{
            var g=Story("departure");var e=g.Event;double early=g.EventWeight(e);g.State.hero.level=7;Assert(g.EventWeight(e)<early);
            double first=g.EventWeight(e);g.State.seen.Add(new Counter(e.id,1));Assert(g.EventWeight(e)==0);
            e=data.Event("roaming_post");first=g.EventWeight(e);g.State.seen.Add(new Counter(e.id,2));Assert(g.EventWeight(e)<first);
        });
        Check("travel spends supplies and time, persists region and enforces routes",()=>{
            var g=Story("spring");var before=Encode(g.State);Assert(g.Travel("frontier")=="아직 갈 수 없는 길입니다."&&Encode(g.State)==before);
            int week=g.State.weeks;g.Travel("village");Assert(g.State.region=="village"&&g.State.weeks==week+2&&g.State.rations==3);
            var restored=new Game(data,Decode(Encode(g.State)));Assert(restored.State.region=="village"&&restored.State.eventId==g.State.eventId);
        });
        Check("quest branches and delayed epilogues unlock only from their own choices",()=>{
            var g=Story("q001_offer");Assert(!g.AvailableEvents().Any(e=>e.id=="q001_trail"));g.Choose(0);g.State.weeks+=2;
            Assert(g.AvailableEvents().Any(e=>e.id=="q001_trail")&&!g.AvailableEvents().Any(e=>e.id=="q001_bargain"));
            g.Apply(new Effects{flag="q001_solved_a"});Assert(!g.AvailableEvents().Any(e=>e.id=="q001_echo_a"));g.State.weeks+=4;
            Assert(g.AvailableEvents().Any(e=>e.id=="q001_echo_a")&&!g.AvailableEvents().Any(e=>e.id=="q001_echo_b"));
        });
        Check("all regions keep a legal event pool even after local one-off quests are exhausted",()=>{
            foreach(var r in data.regions){var g=New();g.State.region=r.id;g.State.hero.level=7;foreach(var e in data.events.Where(e=>e.once))g.State.seen.Add(new Counter(e.id,1));
                g.State.recent=data.events.Where(e=>e.regions.Length==0||e.regions.Contains(r.id)).Select(e=>e.id).ToList();
                g.State.mode="outcome";g.State.outcome=new Outcome();Assert(g.Continue());Assert(g.Event.regions.Length==0||g.Event.regions.Contains(r.id));}
        });
        Check("legacy saves infer region and automatically finish a companion's pending turn",()=>{
            var g=Battle();g.State.region=null;g.State.combat.active="mira";g.State.combat.queue=new System.Collections.Generic.List<string>{"hero"};
            Assert(!g.Act("guard","mira"));var restored=new Game(data,Decode(Encode(g.State)));
            Assert(restored.State.region=="forest"&&restored.State.combat.active=="hero"&&Game.Valid(restored.State,data));
        });
        Check("AI healer prioritizes injured hero without consuming shared potions",()=>{
            var g=Battle();g.Apply(new Effects{recruit="elian"});g.State.hero.hp=5;g.State.combat.queue=new System.Collections.Generic.List<string>{"elian","hero"};
            int hp=g.State.hero.hp,potions=g.State.potions;g.Act("guard","hero");Assert(g.State.hero.hp>hp&&g.State.potions==potions&&g.State.combat.active=="hero");
        });
        Check("a solo ranged hero always has a legal offensive fallback",()=>{
            var g=New();g.Apply(new Effects{weapon="bow"});g.Equip("bow");g.StartCombat(new[]{"slime"},new Outcome{title="test"});
            Assert(g.SkillLock("strike")==""&&g.Targets("strike").Count==1);
        });
        Check("AI healer regains energy before healing on its next turn",()=>{
            var g=Battle();g.Apply(new Effects{recruit="elian"});g.Actor("elian").energy=0;g.State.hero.hp=15;
            g.State.combat.queue=new System.Collections.Generic.List<string>{"elian","hero"};g.Act("guard","hero");
            Assert(g.Actor("elian").energy==2&&g.State.hero.hp==15);
            g.State.combat.queue=new System.Collections.Generic.List<string>{"elian","hero"};g.Act("guard","hero");
            Assert(g.Actor("elian").energy==0&&g.State.hero.hp>15);
        });
        Check("every authored event choice can execute and produce a valid save",()=>{
            foreach(var e in data.events)for(int i=0;i<e.choices.Length;i++){
                var g=Story(e.id);g.State.gold=10000;g.State.rations=100;g.State.hero.maxHp=g.State.hero.hp=500;
                g.State.weeks=e.minWeeks;g.State.hero.level=e.minStage==2?7:e.minStage==1?3:1;
                if(e.minParty>1)g.Apply(new Effects{recruit="mira"});
                if(!string.IsNullOrEmpty(e.requiredFlag))g.Apply(new Effects{flag=e.requiredFlag});
                var n=e.choices[i].need;if(n!=null){if(!string.IsNullOrEmpty(n.flag))g.Apply(new Effects{flag=n.flag});if(!string.IsNullOrEmpty(n.weapon))g.Apply(new Effects{weapon=n.weapon});g.State.rep=Math.Max(g.State.rep,n.rep);}
                Assert(g.Choose(i),e.id+" choice "+i);if(g.State.mode=="combat")Assert(g.State.combat.active=="hero",e.id);
                Assert(Game.Valid(g.State,data),"Invalid save: "+e.id+" choice "+i);
            }
        });
    }
}
