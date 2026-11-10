using System.Linq;

namespace Arvito.Core
{
    public sealed partial class Game
    {
        // Loading a legacy save on a companion's turn resumes it automatically once.
        void ResumeCombat()
        {
            if (State.mode != "combat" || State.combat == null || State.combat.active == "hero") return;
            var a = Actor(State.combat.active);
            if (a != null) CompanionAction(a);
            State.combat.active = null;
            if (!CheckDeaths()) AdvanceTurn();
        }
        void CompanionAction(Actor actor)
        {
            var legal = Skills(actor).Where(id => SkillLock(id) == "").Select(Data.Skill).ToList();
            var wounded = Party.OrderBy(a => (double)a.hp / a.maxHp).First();
            var heal = legal.FirstOrDefault(s => s.heal > 0 && s.item != "potions");
            if (heal != null && (wounded.hp < wounded.maxHp * .6 || wounded.status.bleed > 0)) { ResolveAction(heal.id, wounded.id); return; }
            if (wounded.hp < wounded.maxHp * .30 && legal.Any(s => s.id == "potion")) { ResolveAction("potion", wounded.id); return; }
            bool canLearnHeal = Skills(actor).Select(Data.Skill).Any(s => s.heal > 0 && s.item != "potions" && s.from.Contains(Rank(actor.id)));
            if (canLearnHeal && heal == null && (wounded.hp < wounded.maxHp * .6 || wounded.status.bleed > 0)) { ResolveAction("focus", actor.id); return; }
            var anxious = Party.OrderByDescending(a => a.stress).First();
            var calm = legal.FirstOrDefault(s => s.calm > 0);
            if (calm != null && anxious.stress >= 75) { ResolveAction(calm.id, anxious.id); return; }
            var attacks = legal.Where(s => s.power > 0).ToList();
            if (attacks.All(s => s.id == "strike") && legal.Any(s => s.move))
            {
                var partner = Party.FirstOrDefault(a => a.id != actor.id && Data.Weapon(actor.weapon).skills.Any(id => Data.Skill(id).power > 0 && Data.Skill(id).from.Contains(Rank(a.id))));
                if (partner != null) { ResolveAction("move", partner.id); return; }
            }
            bool threat = State.combat.enemies.Any(e => e.hp > 0 && e.power >= 7 && e.status.stun == 0 && State.combat.queue.Contains(e.id));
            var attack = attacks.OrderByDescending(s => s.power * s.accuracy / 100.0 + s.bleed * .12 + s.stun * (threat ? .008 : .004) - s.cost * .08).FirstOrDefault();
            if (attack != null)
            {
                string target = Targets(attack.id).OrderBy(id => attack.stun > 0 && State.combat.enemies.First(e => e.id == id).status.stun > 0 ? 1 : 0).ThenBy(id => State.combat.enemies.First(e => e.id == id).hp).First();
                ResolveAction(attack.id, target); return;
            }
            ResolveAction(actor.energy < 3 ? "focus" : "guard", actor.id);
        }
    }
}
