using System;
using System.Collections.Generic;
using System.Linq;

namespace Arvito.Core
{
    public sealed partial class Game
    {
        public int Stage => State.hero.level >= 7 || State.events >= 65 || State.weeks >= 1040 ? 2 : State.hero.level >= 3 || State.events >= 18 || State.weeks >= 260 ? 1 : 0;
        public string StageName => new[] { "초반 · 낯선 길", "중반 · 쌓이는 이름", "후반 · 남길 이야기" }[Stage];
        public string RegionName => Data.regions.FirstOrDefault(r => r.id == State.region)?.name ?? "브린 근교";
        bool Eligible(StoryEvent e)
        {
            var s = State;
            return (!e.once || Count(s.seen, e.id) == 0) && s.weeks >= e.minWeeks && s.hero.level >= e.minLevel && Stage >= e.minStage
                && s.rep >= e.minRep && Party.Count >= e.minParty
                && (e.regions.Length == 0 || e.regions.Contains(s.region))
                && (string.IsNullOrEmpty(e.requiredFlag) || s.flags.Contains(e.requiredFlag))
                && (string.IsNullOrEmpty(e.excludedFlag) || !s.flags.Contains(e.excludedFlag))
                && (string.IsNullOrEmpty(e.requiredWeapon) || s.inventory.Contains(e.requiredWeapon))
                && (e.minFlagWeeks <= 0 || s.flagWeeks.Any(c => c.id == e.requiredFlag && s.weeks - c.value >= e.minFlagWeeks))
                && (string.IsNullOrEmpty(e.excludedFallen) || !s.fallen.Contains(e.excludedFallen));
        }
        public double EventWeight(StoryEvent e) => Eligible(e) ? Math.Max(0, e.stageWeights[Stage]) / (1.0 + Count(State.seen, e.id) * .75) : 0;
        string DrawEvent()
        {
            var candidates = AvailableEvents().Where(e => EventWeight(e) > 0).ToList();
            // Relax only the repetition window when a small local pool is exhausted.
            // Region, progression and quest conditions are never bypassed.
            if (candidates.Count == 0) candidates = Data.events.Where(e => EventWeight(e) > 0).ToList();
            if (candidates.Count == 0) throw new InvalidOperationException("현재 지역에 가능한 사건이 없습니다: " + State.region);
            double ticket = Random() * candidates.Sum(EventWeight);
            foreach (var e in candidates) { ticket -= EventWeight(e); if (ticket < 0) return e.id; }
            return candidates[candidates.Count - 1].id;
        }
        public List<Region> Routes() => Data.regions.Where(r => r.id != State.region && r.minStage <= Stage &&
            (Data.regions.FirstOrDefault(x => x.id == State.region)?.routes.Contains(r.id) ?? true)).ToList();
        public string Travel(string destination)
        {
            if (State.mode != "story") return "현재 사건을 마친 뒤 이동할 수 있습니다.";
            if (!Routes().Any(r => r.id == destination)) return "아직 갈 수 없는 길입니다.";
            if (State.rations < 1 && State.gold < 6) return "식량 1개 또는 6골드가 필요합니다.";
            if (State.rations > 0) State.rations--; else State.gold -= 6;
            if (!AdvanceTime(2)) return "마지막 여정이 기록되었습니다.";
            State.region = destination; State.eventId = DrawEvent();
            Record("새로운 땅의 바람", RegionName + "에 도착했다. 2주가 흘렀다.", "travel");
            return RegionName + " 도착 · 2주 경과";
        }
    }
}
