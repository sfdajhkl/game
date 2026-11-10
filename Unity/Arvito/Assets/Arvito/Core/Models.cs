using System;
using System.Collections.Generic;
using System.Linq;

namespace Arvito.Core
{
    [Serializable] public class Catalog
    {
        public Weapon[] weapons = Array.Empty<Weapon>();
        public Skill[] skills = Array.Empty<Skill>();
        public Actor[] companions = Array.Empty<Actor>();
        public Enemy[] enemies = Array.Empty<Enemy>();
        public StoryEvent[] events = Array.Empty<StoryEvent>();
        public Weapon Weapon(string id) => weapons.First(w => w.id == id);
        public Skill Skill(string id) => skills.First(s => s.id == id);
        public StoryEvent Event(string id) => events.First(e => e.id == id);
        public void Normalize()
        {
            // JsonUtility can leave omitted fields null because the authored JSON is sparse.
            foreach (var w in weapons) w.skills = w.skills ?? Array.Empty<string>();
            foreach (var a in companions) a.skills = a.skills ?? new List<string>();
            foreach (var s in skills) { s.from = s.from ?? Array.Empty<int>(); s.to = s.to ?? Array.Empty<int>(); if (s.accuracy == 0) s.accuracy = 100; }
            foreach (var e in events) foreach (var c in e.choices) { c.effects = c.effects ?? new Effects(); if (c.failure != null) c.failure.effects = c.failure.effects ?? new Effects(); }
        }
    }
    [Serializable] public class Weapon
    {
        public string id, name, type, icon, rarity, desc;
        public int power, speed;
        public string[] skills = Array.Empty<string>();
    }
    [Serializable] public class Skill
    {
        public string id, name, desc, icon, target, item;
        public int[] from = Array.Empty<int>(), to = Array.Empty<int>();
        public double power;
        public int accuracy = 100, cost, stun, bleed, heal, calm;
        public bool guard, magic, focus, move;
    }
    [Serializable] public class Stats
    {
        public int strength = 4, agility = 4, wit = 4, will = 4;
        public int Get(string id) => id == "strength" ? strength : id == "agility" ? agility : id == "wit" ? wit : will;
        public void Raise(string id) { if (id == "strength") strength++; else if (id == "agility") agility++; else if (id == "wit") wit++; else if (id == "will") will++; }
        public static string Label(string id) => id == "strength" ? "힘" : id == "agility" ? "민첩" : id == "wit" ? "지혜" : "의지";
    }
    [Serializable] public class Counter { public string id; public int value; public Counter() { } public Counter(string key, int n = 0) { id = key; value = n; } }
    [Serializable] public class Conditions { public int bleed, stun; public bool guard; }
    [Serializable] public class Actor
    {
        public string id, name, subtitle, weapon, portrait, story;
        public int maxHp, hp, stress, energy = 4, speed, power, level = 1, xp;
        public Stats stats = new Stats();
        public List<string> skills = new List<string>();
        public List<Counter> mastery = new List<Counter>();
        public Conditions status = new Conditions();
        public Actor Copy() => new Actor { id = id, name = name, subtitle = subtitle, weapon = weapon, portrait = portrait, story = story, maxHp = maxHp, hp = maxHp, speed = speed, power = power, skills = new List<string>(skills) };
    }
    [Serializable] public class Enemy
    {
        public string id, type, name, portrait, attack;
        public int maxHp, hp, power, speed, armor, stress;
        public bool bleed, ranged;
        public Conditions status = new Conditions();
        public Enemy Copy(string instance) => new Enemy { id = instance, type = id, name = name, portrait = portrait, attack = attack, maxHp = maxHp, hp = maxHp, power = power, speed = speed, armor = armor, stress = stress, bleed = bleed, ranged = ranged };
    }
    [Serializable] public class Effects
    {
        public int gold, rations, potions, rep, bond, xp, hp, heal, stress;
        public string flag, weapon, skill, recruit, stat;
    }
    [Serializable] public class Need { public int gold, rations; public bool partySpace; public string flag; }
    [Serializable] public class Check { public string stat; public int dc; }
    [Serializable] public class Failure { public string body; public Effects effects = new Effects(); }
    [Serializable] public class Choice
    {
        public string label, body, hint, next;
        public int weeks;
        public bool repeat;
        public Effects effects = new Effects();
        public Need need;
        public Check check;
        public Failure failure;
        public string[] battle;
    }
    [Serializable] public class StoryEvent
    {
        public string id, title, chapter, place, art, tag, requiredFlag, excludedFallen;
        public bool once;
        public int minWeeks, minLevel;
        public string[] text = Array.Empty<string>();
        public Choice[] choices = Array.Empty<Choice>();
    }
    [Serializable] public class Outcome
    {
        public string title, body, next, checkStat;
        public bool combatWin, hasCheck, success;
        public int weeks, chance;
        public Effects effects = new Effects();
        public List<string> changes = new List<string>();
    }
    [Serializable] public class Combat
    {
        public int round;
        public string active;
        public List<string> queue = new List<string>(), log = new List<string>();
        public List<Enemy> enemies = new List<Enemy>();
        public Outcome pending;
    }
    [Serializable] public class JournalEntry
    {
        public int week;
        public string title, body, type;
    }
    [Serializable] public class Ending { public string cause, title, legacy, body; public int age; }
    [Serializable] public class GameState
    {
        public int version = 1;
        public uint rng;
        public int weeks, gold = 24, rations = 4, potions = 3, rep, bond, events, victories;
        public double vitality = 1;
        public string mode = "story", eventId = "departure", startedAt;
        public Actor hero;
        public List<Actor> companions = new List<Actor>();
        public List<string> formation = new List<string> { "hero" }, fallen = new List<string>(), inventory = new List<string> { "sword" }, flags = new List<string>(), recent = new List<string>();
        public List<Counter> seen = new List<Counter>();
        public List<JournalEntry> journal = new List<JournalEntry>();
        public Outcome outcome;
        public Combat combat;
        public Ending ending;
    }
}
