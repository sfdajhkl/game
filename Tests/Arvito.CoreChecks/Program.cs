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
        Check("catalog: 23 events, 7 weapons, 3 companions, 17 skills", () => { Assert(data.events.Length == 23); Assert(data.weapons.Length == 7); Assert(data.companions.Length == 3); Assert(data.skills.Length == 17); foreach (var e in data.events) foreach (var c in e.choices) Assert(c.weeks >= 1); });
        Check("starts at sixteen with no fixed class", () => { var g = New(); Assert(g.Age == 16 && g.State.hero.weapon == "sword"); Assert(Game.Valid(g.State, data)); });
        Check("recruitment and authored follow-up", () => { var g = New(); Assert(g.Choose(0)); Assert(g.Party.Count == 2 && g.State.weeks == 1); g.Continue(); Assert(g.State.eventId == "first_hunt"); });
        Check("resource gate does not mutate time or RNG", () => { var g = New(); g.State.eventId = "market"; g.State.gold = 0; var before = Encode(g.State); Assert(!g.Choose(0)); Assert(before == Encode(g.State)); });
        Check("duplicate recruitment / maximum party / tank joins front", () => { var g = New(); foreach (string id in new[] { "mira", "elian", "borin", "mira" }) g.Apply(new Effects { recruit = id }); Assert(g.Party.Count == 4 && g.State.formation[0] == "borin"); });
        Check("weekly mortality increases with age", () => { Assert(Game.WeeklyRisk(39) == 0); for (int age = 40; age < 100; age++) Assert(Game.WeeklyRisk(age + 1) >= Game.WeeklyRisk(age)); });
        Check("long time jump equals individual weeks", () => { var a = New(91); var b = New(91); a.State.weeks = b.State.weeks = 44 * 52; a.AdvanceTime(104); for (int i = 0; i < 104 && b.State.mode != "ended"; i++) b.AdvanceTime(1); Assert(a.State.weeks == b.State.weeks && a.State.rng == b.State.rng); });
        Check("death interrupts long event before rewards", () => { var g = New(87); g.State.weeks = 90 * 52; g.State.eventId = "quiet_years"; g.Choose(0); Assert(g.State.mode == "ended" && g.State.gold == 24); });
        Check("earlier choices unlock the reunion", () => { var g = New(); g.State.weeks = 104; Assert(!g.AvailableEvents().Any(e => e.id == "reunion")); g.State.flags.Add("saved_child"); Assert(g.AvailableEvents().Any(e => e.id == "reunion")); });
        Check("weapon switch retains learned skill", () => { var g = New(); g.Apply(new Effects { weapon = "staff", skill = "rally" }); g.Equip("staff"); Assert(g.Skills(g.State.hero).Contains("ember") && g.Skills(g.State.hero).Contains("rally") && !g.Skills(g.State.hero).Contains("slash")); });
        Check("rank, target and energy gates reject invalid turns", () => { var g = Battle(); g.State.combat.active = "hero"; g.State.hero.weapon = "bow"; var before = Encode(g.State); Assert(!g.Act("shot", "enemy_0") && before == Encode(g.State)); g.State.hero.weapon = "sword"; Assert(!g.Act("slash", "mira")); g.State.combat.active = "mira"; g.Actor("mira").energy = 0; Assert(!g.Act("pin", "enemy_0")); });
        Check("position swap consumes a turn", () => { var g = Battle(); g.State.combat.active = "hero"; g.State.combat.queue = new System.Collections.Generic.List<string> { "mira" }; Assert(g.Act("move", "mira")); Assert(g.State.formation[0] == "mira" && g.State.combat.active == "mira"); });
        Check("potion consumes one item and cures bleed", () => { var g = Battle(); g.State.combat.active = "hero"; g.State.combat.queue = new System.Collections.Generic.List<string> { "mira" }; g.State.hero.hp -= 3; g.State.hero.status.bleed = 3; g.Act("potion", "hero"); Assert(g.State.potions == 2 && g.State.hero.hp == 38 && g.State.hero.status.bleed == 0); });
        Check("repeated choice cannot duplicate rewards", () => { var g = New(); g.Choose(0); var before = Encode(g.State); Assert(!g.Choose(0) && before == Encode(g.State)); });
        Check("30 seeded first battles reach victory and next event", () => { for (uint seed = 1; seed <= 30; seed++) { var g = Battle(seed); int turns = 0; while (g.State.mode == "combat" && turns++ < 100) Assert(AutoAction(g)); Assert(g.State.mode == "outcome" && g.State.victories == 1 && g.State.gold == 40, "seed " + seed); g.Continue(); Assert(g.State.eventId == "crossroads"); } });
        Check("battle save preserves next actor and RNG", () => { var a = Battle(9); AutoAction(a); var b = new Game(data, Decode(Encode(a.State))); Assert(Game.Valid(b.State, data)); AutoAction(a); AutoAction(b); Assert(Encode(a.State) == Encode(b.State)); });
        Check("broken combat snapshots are rejected", () => { var g = Battle(); g.State.combat.enemies[0].status = null; Assert(!Game.Valid(g.State, data)); g = Battle(); g.State.combat.queue.Add("missing_enemy"); Assert(!Game.Valid(g.State, data)); });
        Check("bleed can end the hero's life", () => { var g = Battle(); g.State.hero.hp = 2; g.State.hero.status.bleed = 1; g.State.combat.active = "mira"; g.State.combat.queue = new System.Collections.Generic.List<string> { "hero" }; g.Act("guard", "mira"); Assert(g.State.mode == "ended" && g.State.ending.cause == "battle"); });
        Check("companion death is permanent and closes formation", () => { var g = Battle(); var m = g.Actor("mira"); m.hp = 2; m.status.bleed = 1; g.State.combat.active = "hero"; g.State.combat.queue = new System.Collections.Generic.List<string> { "mira", "hero" }; g.Act("guard", "hero"); Assert(g.State.fallen.Contains("mira") && g.Party.Count == 1); g.Apply(new Effects { recruit = "mira" }); Assert(g.Party.Count == 1); });
        Check("stun / guard expire at actor turn", () => { var g = Battle(); g.Actor("mira").status.stun = 1; g.State.combat.active = "hero"; g.State.combat.queue = new System.Collections.Generic.List<string> { "mira", "hero" }; g.Act("guard", "hero"); Assert(g.State.combat.active == "hero" && !g.State.hero.status.guard && g.Actor("mira").status.stun == 0); });
        Check("stress breakdown loses one turn", () => { var g = Battle(); g.Actor("mira").stress = 100; g.State.combat.active = "hero"; g.State.combat.queue = new System.Collections.Generic.List<string> { "mira", "hero" }; g.Act("guard", "hero"); Assert(g.State.combat.active == "hero" && g.Actor("mira").stress == 65); });
        Check("retreat has cost and no victory reward", () => { var g = Battle(); g.State.gold = 31; g.Flee(); Assert(g.State.gold == 24 && g.State.victories == 0 && g.State.mode == "outcome"); });
        Check("camp heals, spends supplies and advances time", () => { var g = New(); g.State.hero.hp = 10; g.State.hero.stress = 35; g.Camp(); Assert(g.State.rations == 3 && g.State.weeks == 1 && g.State.hero.stress == 15 && g.State.hero.hp > 10); });
        Check("atomic save and corrupted-primary backup recovery", () => {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "arvito-test-" + Guid.NewGuid()); string file = System.IO.Path.Combine(dir, "save.json");
            try { var store = new SaveStore(file, Encode, Decode, s => Game.Valid(s, data)); var g = New(); store.Save(g.State); g.Choose(0); store.Save(g.State); Assert(store.Load(out _)?.weeks == 1); File.WriteAllText(file, "{broken"); var restored = store.Load(out string notice); Assert(restored != null && restored.weeks == 0 && notice.Length > 0); }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        });
        Console.WriteLine($"{passed} C# checks passed.");
    }
}
