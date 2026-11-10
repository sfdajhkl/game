using System;
using System.Collections.Generic;
using System.Linq;

namespace Arvito.Core
{
    /// <summary>Deterministic game rules. No Unity dependency; shared by the player and C# checks.</summary>
    public sealed class Game
    {
        public Catalog Data { get; }
        public GameState State { get; private set; }
        public Game(Catalog data, GameState state) { Data = data; State = state; }
        public int Age => 16 + State.weeks / 52;
        public string AgeLabel => Age + "세 " + State.weeks % 52 + "주";
        public static string Duration(int weeks) => weeks < 52 ? weeks + "주" : weeks / 52 + "년" + (weeks % 52 == 0 ? "" : " " + weeks % 52 + "주");
        public static int Clamp(int n, int min, int max) => Math.Max(min, Math.Min(max, n));
        static int Round(double v) => (int)Math.Round(v, MidpointRounding.AwayFromZero);
        public List<Actor> Party => State.formation.Select(id => id == "hero" ? State.hero : State.companions.Find(a => a.id == id)).Where(a => a != null && a.hp > 0).ToList();
        public StoryEvent Event => Data.Event(State.eventId);
        public static Game New(Catalog data, string name, uint seed)
        {
            name = string.IsNullOrWhiteSpace(name) ? "여행자" : name.Trim();
            if (name.Length > 12) name = name.Substring(0, 12);
            var s = new GameState { rng = seed == 0 ? 1u : seed, startedAt = DateTime.UtcNow.ToString("O"), hero = new Actor {
                id = "hero", name = name, subtitle = "이름 없는 모험가", weapon = "sword", portrait = "hero", maxHp = 38, hp = 38, speed = 3, power = 1
            }};
            var g = new Game(data, s); s.vitality = .8 + g.Random() * .4;
            g.Record("첫 장", "열여섯의 봄, 당신은 브린 마을을 떠났다.", "birth"); return g;
        }
        public double Random()
        {
            unchecked { uint x = State.rng; x ^= x << 13; x ^= x >> 17; x ^= x << 5; State.rng = x; }
            return State.rng / 4294967296.0;
        }
        int Roll(int a, int b) => a + (int)Math.Floor(Random() * (b - a + 1));
        public static double AnnualRisk(double age, double vitality = 1) => age < 40 ? 0 : Math.Min(.98, .0015 * Math.Exp((age - 40) / 8) / vitality);
        public static double WeeklyRisk(double age, double vitality = 1) => 1 - Math.Pow(1 - AnnualRisk(age, vitality), 1.0 / 52);
        public static int Count(List<Counter> list, string id) => list.Find(c => c.id == id)?.value ?? 0;
        static void CountSet(List<Counter> list, string id, int value) { var c = list.Find(x => x.id == id); if (c == null) list.Add(new Counter(id, value)); else c.value = value; }
        public void Record(string title, string body, string type = "event")
        {
            State.journal.Insert(0, new JournalEntry { week = State.weeks, title = title, body = body, type = type });
            if (State.journal.Count > 350) State.journal.RemoveRange(350, State.journal.Count - 350);
        }
        public void EndLife(string cause)
        {
            var s = State; s.mode = "ended"; s.combat = null;
            string legacy = s.flags.Contains("dragon_defeated") ? "재를 걷어낸 전설" : s.flags.Contains("mentor") ? "다음 세대의 등불" : s.rep >= 15 ? "오래 기억될 선한 이름" : s.flags.Contains("warden_defeated") ? "새벽의 수호자" : s.victories >= 8 ? "수많은 길을 건넌 자" : "자신의 길을 걸었던 사람";
            s.ending = new Ending { cause = cause, title = cause == "old_age" ? "마지막으로 넘기는 페이지" : "길 위에 남겨진 이름", legacy = legacy, age = Age,
                body = cause == "old_age" ? "어느 고요한 아침, 당신은 더 이상 가방을 챙기지 않았다. 길은 끝났지만 당신이 건넨 온기와 함께 걸었던 이들의 기억은 남았다." : "당신의 발걸음은 이곳에서 멈췄다. 그러나 한 생은 마지막 순간 하나로 정해지지 않는다. 당신이 선택했던 모든 길이 이 책에 남아 있다." };
            Record(s.ending.title, AgeLabel + " · " + legacy, "end");
        }
        public bool AdvanceTime(int weeks)
        {
            if (State.mode == "ended") return false;
            if (weeks < 1) throw new ArgumentOutOfRangeException(nameof(weeks));
            for (int i = 0; i < weeks; i++) { State.weeks++; if (Random() < WeeklyRisk(16 + State.weeks / 52.0, State.vitality)) { EndLife("old_age"); return false; } }
            return true;
        }
        public int CheckChance(Check check) => Clamp(62 + (State.hero.stats.Get(check.stat) - check.dc) * 7 + State.rep / 5 - State.hero.stress / 20 * 3, 15, 95);
        public string ChoiceLock(Choice choice)
        {
            var n = choice.need; if (n == null) return "";
            if (n.gold > State.gold) return n.gold + "골드 필요";
            if (n.rations > State.rations) return "식량이 부족합니다";
            if (n.partySpace && Party.Count >= 4) return "일행은 최대 4명입니다";
            if (!string.IsNullOrEmpty(n.flag) && !State.flags.Contains(n.flag)) return "단서가 필요합니다";
            return "";
        }
        static void Adjust(ref int value, int delta, string label, List<string> changes)
        {
            if (delta == 0) return; int old = value; value = Math.Max(0, value + delta);
            changes.Add(label + " " + (value - old >= 0 ? "+" : "") + (value - old));
        }
        public List<string> Apply(Effects e)
        {
            e = e ?? new Effects(); var s = State; var changes = new List<string>();
            Adjust(ref s.gold, e.gold, "골드", changes); Adjust(ref s.rations, e.rations, "식량", changes); Adjust(ref s.potions, e.potions, "회복약", changes);
            Adjust(ref s.rep, e.rep, "평판", changes); Adjust(ref s.bond, e.bond, "유대", changes);
            if (!string.IsNullOrEmpty(e.flag) && !s.flags.Contains(e.flag)) s.flags.Add(e.flag);
            if (!string.IsNullOrEmpty(e.weapon)) { if (!s.inventory.Contains(e.weapon)) { s.inventory.Add(e.weapon); changes.Add(Data.Weapon(e.weapon).name + " 획득"); } else { s.gold += 12; changes.Add("중복 장비 판매 +12골드"); } }
            if (!string.IsNullOrEmpty(e.skill) && !s.hero.skills.Contains(e.skill)) { s.hero.skills.Add(e.skill); changes.Add(Data.Skill(e.skill).name + " 습득"); }
            if (!string.IsNullOrEmpty(e.recruit) && !s.companions.Any(a => a.id == e.recruit) && !s.fallen.Contains(e.recruit) && Party.Count < 4)
            {
                var actor = Data.companions.First(a => a.id == e.recruit).Copy(); s.companions.Add(actor);
                if (actor.id == "borin") s.formation.Insert(0, actor.id); else s.formation.Add(actor.id);
                changes.Add(actor.name + " 동행 시작");
            }
            if (!string.IsNullOrEmpty(e.stat)) { s.hero.stats.Raise(e.stat); changes.Add(Stats.Label(e.stat) + " +1"); }
            if (e.heal != 0) { foreach (var a in Party) a.hp = Clamp(a.hp + e.heal, 0, a.maxHp); changes.Add(e.heal >= 99 ? "일행 완전 회복" : "일행 체력 +" + e.heal); }
            if (e.hp != 0) { s.hero.hp = Clamp(s.hero.hp + e.hp, 0, s.hero.maxHp); changes.Add("체력 " + e.hp); }
            if (e.stress != 0) { foreach (var a in Party) a.stress = Clamp(a.stress + e.stress, 0, 100); changes.Add("긴장 " + (e.stress > 0 ? "+" : "") + e.stress); }
            if (e.xp != 0)
            {
                s.hero.xp += e.xp; changes.Add("경험 +" + e.xp);
                while (s.hero.xp >= s.hero.level * 45) { s.hero.xp -= s.hero.level * 45; s.hero.level++; s.hero.maxHp += 4; s.hero.hp = Math.Min(s.hero.maxHp, s.hero.hp + 8); s.hero.power++; changes.Add("성장 단계 " + s.hero.level); }
            }
            if (s.hero.hp <= 0) EndLife("wounds"); return changes;
        }
        public bool Choose(int index)
        {
            var s = State; if (s.mode != "story" || index < 0 || index >= Event.choices.Length) return false;
            var e = Event; var c = e.choices[index]; if (ChoiceLock(c) != "") return false;
            bool hasCheck = c.check != null && !string.IsNullOrEmpty(c.check.stat);
            int chance = hasCheck ? CheckChance(c.check) : 100;
            bool success = !hasCheck || Random() * 100 < chance;
            var o = new Outcome { title = e.title, body = success ? c.body : c.failure.body, effects = success ? c.effects : c.failure.effects, next = c.next, hasCheck = hasCheck, success = success, chance = chance, checkStat = hasCheck ? Stats.Label(c.check.stat) : "", weeks = c.weeks };
            s.events++; CountSet(s.seen, s.eventId, c.repeat ? 0 : Count(s.seen, s.eventId) + 1);
            if (!AdvanceTime(c.weeks)) return true;
            if (success && c.battle != null && c.battle.Length > 0) { StartCombat(c.battle, o); return true; }
            o.changes = Apply(o.effects); if (s.mode == "ended") return true;
            s.mode = "outcome"; s.outcome = o; Record(e.title, c.label + " · " + o.body); return true;
        }
        public List<StoryEvent> AvailableEvents()
        {
            var s = State;
            return Data.events.Where(e => e.id != "departure" && e.id != "first_hunt" && e.id != "crossroads" && (!e.once || Count(s.seen, e.id) == 0) && s.weeks >= e.minWeeks && s.hero.level >= e.minLevel && (string.IsNullOrEmpty(e.requiredFlag) || s.flags.Contains(e.requiredFlag)) && (string.IsNullOrEmpty(e.excludedFallen) || !s.fallen.Contains(e.excludedFallen)) && !s.recent.Contains(e.id)).ToList();
        }
        public bool Continue()
        {
            var s = State; if (s.mode != "outcome") return false; var next = s.outcome.next;
            s.recent.Add(s.eventId); if (s.recent.Count > 5) s.recent.RemoveAt(0);
            var candidates = AvailableEvents();
            var followups = candidates.Where(e => new[] { "reunion", "warden", "dragon", "shrine", "shieldbearer" }.Contains(e.id)).ToList();
            if (followups.Count > 0 && Random() < .65) candidates = followups;
            s.eventId = !string.IsNullOrEmpty(next) ? next : candidates.Count == 0 ? "spring" : candidates[Roll(0, candidates.Count - 1)].id;
            s.mode = "story"; s.outcome = null; return true;
        }
        public string Camp()
        {
            var s = State;
            if (s.mode != "story") return "현재 사건을 마친 뒤 야영할 수 있습니다.";
            if (s.rations == 0 && s.gold < 8) return "식량 1개 또는 8골드가 필요합니다.";
            string cost = s.rations > 0 ? "식량 1" : "8골드"; if (s.rations > 0) s.rations--; else s.gold -= 8;
            if (!AdvanceTime(1)) return "마지막 계절이 기록되었습니다.";
            foreach (var a in Party) { a.hp = Math.Min(a.maxHp, a.hp + (int)Math.Ceiling(s.hero.maxHp * .45)); a.stress = Math.Max(0, a.stress - 20); }
            Record("불가의 작은 쉼", cost + "를 쓰고 1주 동안 쉬었다. 일행의 상처와 긴장이 가라앉았다.", "camp");
            return "1주 경과 · 일행 회복 · 긴장 -20";
        }
        public bool Equip(string id) { if (State.mode == "combat" || State.mode == "ended" || !State.inventory.Contains(id)) return false; State.hero.weapon = id; return true; }
        public bool Reorder(string id, int delta)
        {
            var s = State; int p = s.formation.IndexOf(id), q = p + delta;
            if (s.mode == "combat" || s.mode == "ended" || p < 0 || q < 0 || q >= s.formation.Count) return false;
            string previous = s.formation[p]; s.formation[p] = s.formation[q]; s.formation[q] = previous; return true;
        }
        public Actor Actor(string id) => Party.Find(a => a.id == id);
        public int Rank(string id) => Party.FindIndex(a => a.id == id) + 1;
        public List<string> Skills(Actor a) => Data.Weapon(a.weapon).skills.Concat(a.skills).Concat(new[] { "guard", "focus", "potion", "move" }).Distinct().ToList();
        public List<string> Targets(string skillId, string actorId = null)
        {
            var c = State.combat; if (c == null) return new List<string>();
            var a = Actor(actorId ?? c.active); if (a == null) return new List<string>(); var sk = Data.Skill(skillId);
            if (sk.target == "self") return new List<string> { a.id };
            if (sk.target == "ally") return Party.Select(p => p.id).ToList();
            if (sk.target == "otherAlly") return Party.Where(p => p.id != a.id).Select(p => p.id).ToList();
            return c.enemies.Where(e => e.hp > 0).Where((e, i) => sk.to.Contains(i + 1)).Select(e => e.id).ToList();
        }
        public string SkillLock(string skillId)
        {
            if (State.mode != "combat") return "전투 중에만 사용";
            var a = Actor(State.combat.active); var sk = Data.skills.FirstOrDefault(v => v.id == skillId);
            if (a == null || sk == null || !Skills(a).Contains(skillId)) return "사용 불가";
            if (!sk.from.Contains(Rank(a.id))) return string.Join("·", sk.from) + "열에서 사용";
            if (a.energy < sk.cost) return "기력 부족";
            if (sk.item == "potions" && State.potions == 0) return "회복약 없음";
            if (Targets(skillId).Count == 0) return "대상 없음";
            return "";
        }
        void Log(string message) { State.combat.log.Add(message); if (State.combat.log.Count > 80) State.combat.log.RemoveAt(0); }
        public void StartCombat(string[] types, Outcome pending)
        {
            State.mode = "combat"; State.outcome = null;
            State.combat = new Combat { pending = pending, log = new List<string> { "일행이 진형을 갖춥니다." }, enemies = types.Select((t, i) => Data.enemies.First(e => e.id == t).Copy("enemy_" + i)).ToList() };
            foreach (var a in Party) { a.energy = 4; a.status = new Conditions(); } AdvanceTurn();
        }
        void NewRound()
        {
            var c = State.combat; c.round++;
            var initiative = new List<KeyValuePair<string, int>>();
            foreach (var a in Party) initiative.Add(new KeyValuePair<string, int>(a.id, a.speed + Data.Weapon(a.weapon).speed + Roll(0, 4)));
            foreach (var e in c.enemies.Where(e => e.hp > 0)) initiative.Add(new KeyValuePair<string, int>(e.id, e.speed + Roll(0, 4)));
            c.queue = initiative.OrderByDescending(p => p.Value).Select(p => p.Key).ToList(); Log("— " + c.round + "번째 라운드 —");
        }
        bool CheckDeaths()
        {
            var s = State; var c = s.combat; if (c == null) return true;
            if (s.hero.hp <= 0) { Record("마지막 전투", string.Join(" ", c.log.Skip(Math.Max(0, c.log.Count - 5))), "battle"); EndLife("battle"); return true; }
            foreach (var a in s.companions.Where(a => a.hp <= 0 && !s.fallen.Contains(a.id)))
            {
                s.fallen.Add(a.id); s.formation.Remove(a.id); Record(a.name + "와의 작별", "함께했던 동료가 전투 중 쓰러졌다.", "loss"); Log(a.name + "이(가) 쓰러졌습니다. 다시 돌아오지 못합니다.");
                foreach (var p in Party) p.stress = Clamp(p.stress + 15, 0, 100);
            }
            if (c.enemies.All(e => e.hp <= 0))
            {
                var o = c.pending; string encounter = o.title; s.victories++; s.combat = null; o.changes = Apply(o.effects);
                if (s.mode == "ended") return true;
                foreach (var a in Party) a.status = new Conditions();
                o.title = "살아서, 다음 페이지로"; o.combatWin = true; s.outcome = o; s.mode = "outcome"; Record(encounter, o.body, "victory"); return true;
            }
            return false;
        }
        void EnemyAction(Enemy e)
        {
            var pool = e.ranged ? Party : Party.Take(2).ToList(); var t = pool[Roll(0, pool.Count - 1)];
            if (Random() < .08) { Log(e.name + "의 " + e.attack + " — " + t.name + " 회피!"); return; }
            int damage = Math.Max(1, Round((e.power + Roll(-1, 2)) * (t.status.guard ? .35 : 1)));
            int tension = Math.Max(1, (e.stress > 0 ? e.stress : 3) - (t.id == "hero" ? State.hero.stats.will : 4) / 4);
            t.hp = Math.Max(0, t.hp - damage); t.stress = Clamp(t.stress + tension, 0, 100);
            if (e.bleed && Random() < .3) t.status.bleed = 2;
            Log(e.name + " · " + e.attack + " → " + t.name + " 체력 -" + damage + (e.stress > 0 ? ", 긴장 +" + tension : ""));
        }
        void AdvanceTurn()
        {
            for (int safety = 0; State.mode == "combat" && safety < 100; safety++)
            {
                if (CheckDeaths()) return; var c = State.combat; if (c.queue.Count == 0) NewRound();
                string id = c.queue[0]; c.queue.RemoveAt(0);
                var ally = Actor(id); var enemy = c.enemies.Find(e => e.id == id && e.hp > 0);
                if (ally == null && enemy == null) continue;
                var status = ally != null ? ally.status : enemy.status; string name = ally != null ? ally.name : enemy.name;
                status.guard = false;
                if (status.bleed > 0)
                {
                    status.bleed--; if (ally != null) ally.hp = Math.Max(0, ally.hp - 2); else enemy.hp = Math.Max(0, enemy.hp - 2);
                    Log(name + " · 출혈 피해 2"); if (CheckDeaths()) return;
                    if ((ally != null ? ally.hp : enemy.hp) <= 0) continue;
                }
                if (status.stun > 0) { status.stun--; Log(name + " · 기절로 차례를 넘깁니다."); continue; }
                if (ally != null && ally.stress >= 100) { ally.stress = 65; Log(name + " · 압도된 마음. 한 차례를 잃습니다."); continue; }
                if (enemy != null) { EnemyAction(enemy); continue; }
                c.active = id; return;
            }
            if (State.mode == "combat") throw new InvalidOperationException("전투 턴 진행 한도 초과");
        }
        public bool Act(string skillId, string targetId)
        {
            if (State.mode != "combat" || string.IsNullOrEmpty(skillId) || SkillLock(skillId) != "" || !Targets(skillId).Contains(targetId)) return false;
            var c = State.combat; var a = Actor(c.active); var sk = Data.Skill(skillId); var ally = Actor(targetId); var enemy = c.enemies.Find(e => e.id == targetId);
            a.energy -= sk.cost; if (sk.item == "potions") State.potions--;
            if (sk.move)
            {
                int p = State.formation.IndexOf(a.id), q = State.formation.IndexOf(ally.id); State.formation[p] = ally.id; State.formation[q] = a.id;
                Log(a.name + " ↔ " + ally.name + " 자리 교대");
            }
            else if (sk.heal > 0) { int amount = Math.Min(ally.maxHp - ally.hp, sk.item == "potions" ? sk.heal : sk.heal + Roll(-2, 2)); ally.hp += amount; ally.status.bleed = 0; Log(a.name + " · " + sk.name + " → " + ally.name + " 체력 +" + amount + ", 출혈 해제"); }
            else if (sk.calm > 0) { ally.stress = Math.Max(0, ally.stress - sk.calm); ally.energy = Math.Min(5, ally.energy + 1); Log(a.name + " · " + sk.name + " → " + ally.name + " 긴장 -" + sk.calm); }
            else if (sk.focus) { a.energy = Math.Min(5, a.energy + 2); a.stress = Math.Max(0, a.stress - 6); Log(a.name + " · 숨 고르기, 기력 +2"); }
            else if (sk.power > 0)
            {
                if (Random() * 100 > sk.accuracy) Log(a.name + " · " + sk.name + " → " + enemy.name + " 빗나감");
                else
                {
                    int mastery = a.id == "hero" ? Math.Min(3, Count(a.mastery, a.weapon) / 8) : 0;
                    int stat = a.id == "hero" ? a.stats.Get(sk.magic ? "wit" : (a.weapon == "bow" || a.weapon == "dagger") ? "agility" : "strength") / 3 : 0;
                    bool critical = Random() < .08;
                    double raw = (Data.Weapon(a.weapon).power + a.power + mastery + stat + Roll(-1, 2)) * sk.power;
                    int damage = Math.Max(1, Round((raw - (sk.magic ? 0 : enemy.armor)) * (critical ? 1.6 : 1) * (enemy.status.guard ? .35 : 1)));
                    enemy.hp = Math.Max(0, enemy.hp - damage); if (sk.bleed > 0) enemy.status.bleed = sk.bleed;
                    bool stun = sk.stun > 0 && Random() * 100 < sk.stun; if (stun) enemy.status.stun = 1;
                    Log(a.name + " · " + sk.name + " → " + enemy.name + (critical ? " 치명타!" : "") + " -" + damage + (sk.bleed > 0 ? " · 출혈" : "") + (stun ? " · 기절" : ""));
                }
                if (a.id == "hero") CountSet(a.mastery, a.weapon, Count(a.mastery, a.weapon) + 1);
            }
            if (sk.guard) { a.status.guard = true; if (sk.power == 0) a.energy = Math.Min(5, a.energy + 1); Log(a.name + " · 다음 차례까지 피해 65% 감소"); }
            c.active = null; if (!CheckDeaths()) AdvanceTurn(); return true;
        }
        public bool Flee()
        {
            var s = State; if (s.mode != "combat") return false;
            int lost = Math.Min(s.gold, (int)Math.Ceiling(s.gold * .2)); s.gold -= lost;
            foreach (var a in Party) a.stress = Clamp(a.stress + 12, 0, 100);
            string next = s.combat.pending.next; if (Event.once) CountSet(s.seen, s.eventId, 0);
            s.combat = null; s.mode = "outcome"; s.outcome = new Outcome { title = "살아남는 것도 선택이다", body = "동료들과 함께 전장을 벗어났다. 잃은 것은 다시 얻을 수 있다. 아직 다음 이야기가 남아 있다.", next = next, changes = new List<string> { "골드 -" + lost, "일행 긴장 +12" } };
            Record("물러선 발걸음", "전투에서 퇴각했다. " + lost + "골드를 잃었지만 일행은 살아남았다.", "flee"); return true;
        }
        public static bool Valid(GameState s, Catalog data)
        {
            try
            {
                if (s == null || s.version != 1 || s.rng == 0 || s.weeks < 0 || s.hero == null || s.hero.id != "hero" || !data.events.Any(e => e.id == s.eventId)) return false;
                if (!new[] { "story", "combat", "outcome", "ended" }.Contains(s.mode) || double.IsNaN(s.vitality) || s.vitality <= 0) return false;
                if (s.formation.Count < 1 || s.formation.Count > 4 || s.formation.Distinct().Count() != s.formation.Count || !s.formation.Contains("hero")) return false;
                var actors = new[] { s.hero }.Concat(s.companions).ToList();
                if (actors.Select(a => a.id).Distinct().Count() != actors.Count || s.companions.Any(a => !data.companions.Any(c => c.id == a.id))) return false;
                if (actors.Any(a => a.hp < 0 || a.maxHp < 1 || a.hp > a.maxHp || a.stress < 0 || a.stress > 100 || a.energy < 0 || a.energy > 5 || a.skills == null || a.stats == null || a.mastery == null || a.status == null || !data.weapons.Any(w => w.id == a.weapon) || a.skills.Any(id => !data.skills.Any(sk => sk.id == id)))) return false;
                if (s.mode != "ended" && s.hero.hp == 0) return false;
                if (s.formation.Any(id => actors.All(a => a.id != id)) || s.inventory.Any(id => data.weapons.All(w => w.id != id))) return false;
                if (s.gold < 0 || s.rations < 0 || s.potions < 0 || s.hero.level < 1 || s.hero.stats == null || s.journal == null || s.flags == null || s.seen == null || s.recent == null || s.fallen == null) return false;
                if (s.mode == "combat")
                {
                    var c = s.combat;
                    if (c == null || c.pending == null || c.enemies == null || c.queue == null || c.log == null || !s.formation.Contains(c.active) || actors.First(a => a.id == c.active).hp == 0) return false;
                    if (c.enemies.Count < 1 || c.enemies.Count > 4 || c.enemies.All(e => e.hp == 0) || c.enemies.Select(e => e.id).Distinct().Count() != c.enemies.Count) return false;
                    if (c.enemies.Any(e => e.status == null || e.maxHp < 1 || e.hp < 0 || e.hp > e.maxHp || !e.id.StartsWith("enemy_") || !data.enemies.Any(t => t.id == e.type))) return false;
                    if (c.queue.Any(id => !actors.Any(a => a.id == id) && !c.enemies.Any(e => e.id == id))) return false;
                }
                if (s.mode == "outcome" && s.outcome == null || s.mode == "ended" && s.ending == null) return false;
                return true;
            }
            catch { return false; }
        }
    }
}
