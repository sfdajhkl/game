using System;
using System.Collections.Generic;
using V2 = System.Numerics.Vector2;

namespace Arvito
{
    public enum Outcome { Active, Extracted, Destroyed }
    public enum FieldEvent { Shot, Hit, Wreck, Salvage, Wave, Extract }

    public sealed class Bolt
    {
        public V2 Position, Direction;
        public float Speed, Remaining, Damage;
        public bool Friendly;
        public int Id;
    }

    public sealed class Wreck
    {
        public readonly int Id;
        public readonly V2 Position;
        public readonly ItemId[] Cargo;
        public readonly string Name;
        public readonly bool Cache;

        public Wreck(int id, V2 p, string name, bool cache, ItemId[] cargo)
        { Id = id; Position = p; Name = name; Cache = cache; Cargo = cargo; }
    }

    public readonly struct Cover
    {
        public readonly V2 Position;
        public readonly float Radius;
        public Cover(float x, float z, float radius) { Position = new V2(x, z); Radius = radius; }
    }

    public sealed class Field
    {
        public const float Limit = 18.5f;
        public const float UnitRadius = .68f;
        public const int TotalWaves = 3;
        public static readonly V2 Beacon = new V2(0, -3);
        public static readonly Cover[] Barriers = {
            new Cover(-6, 3, 1.4f), new Cover(6, -5, 1.35f),
            new Cover(4, 7, 1.25f), new Cover(-10, -7, 1.5f)
        };

        public readonly List<Unit> Units = new List<Unit>();
        public readonly List<Bolt> Bolts = new List<Bolt>();
        public readonly List<Wreck> Wrecks = new List<Wreck>();
        public readonly Inventory Inventory = new Inventory();
        public readonly Unit Player;
        public event Action<FieldEvent, V2, bool> Signal;
        public Outcome Result { get; private set; }
        public int Wave { get; private set; }
        public int Recoveries { get; private set; }
        public float Time { get; private set; }
        public float UntilWave { get; private set; } = 9f;
        public bool CanExtract => Wave == TotalWaves && Hostiles == 0;
        public bool AtBeacon => V2.DistanceSquared(Player.Position, Beacon) <= 2.7f * 2.7f;
        public int Hostiles => Units.Count - 1;
        public string LastRecovery { get; private set; }
        private int nextId;
        private readonly Func<V2, V2, bool> blocked;

        public Field()
        {
            blocked = Blocked;
            Player = Spawn(ChassisRole.Player, Beacon, Rigs.Surveyor());
            Player.Forward = Player.Aim = V2.Normalize(new V2(.7f, -.8f));
            Wrecks.Add(new Wreck(++nextId, new V2(1.8f, -3.2f), "정비 보관함", true,
                new[] { ItemId.Dynamo, ItemId.Repeater, ItemId.Relay, ItemId.Weakest, ItemId.Lead, ItemId.NearField }));
            Wrecks.Add(new Wreck(++nextId, new V2(-8, 6), "매몰된 정찰기", true,
                new[] { ItemId.Skater, ItemId.Rail, ItemId.ForwardArc, ItemId.Damaged, ItemId.Stationary }));
        }

        private Unit Spawn(ChassisRole role, V2 position, Loadout rig)
        {
            var unit = new Unit(++nextId, role, position, rig);
            if (role != ChassisRole.Player)
                unit.Integrity = role == ChassisRole.Warden ? .88f : .44f + .10f * Wave;
            Units.Add(unit);
            return unit;
        }

        public Wreck Nearby()
        {
            Wreck best = null;
            float least = 2.9f * 2.9f;
            foreach (Wreck wreck in Wrecks)
            {
                float d = V2.DistanceSquared(wreck.Position, Player.Position);
                if (d <= least) { least = d; best = wreck; }
            }
            return best;
        }

        public bool Interact()
        {
            if (Result != Outcome.Active) return false;
            Wreck wreck = Nearby();
            if (wreck != null)
            {
                Wrecks.Remove(wreck);
                foreach (ItemId id in wreck.Cargo) Inventory.Add(id);
                Recoveries++;
                Item found = null;
                foreach (ItemId id in wreck.Cargo)
                    if (id != ItemId.Empty && Parts.Get(id).Socket == Socket.Weapon) found = Parts.Get(id);
                LastRecovery = $"{wreck.Name} 회수  ·  {found?.Name ?? "부품"} 등 {wreck.Cargo.Length}개";
                if (!wreck.Cache && Player.MaxHull > 0)
                {
                    Player.Integrity = Math.Min(1, Player.Integrity + 22 / Player.MaxHull);
                    LastRecovery += "  ·  현장 수리";
                }
                Signal?.Invoke(FieldEvent.Salvage, wreck.Position, true);
                return true;
            }
            if (!CanExtract || !AtBeacon) return false;
            Result = Outcome.Extracted;
            Signal?.Invoke(FieldEvent.Extract, Player.Position, true);
            return true;
        }

        private void StartWave()
        {
            Wave++;
            if (Wave == 1)
            {
                Spawn(ChassisRole.Runner, new V2(-12, 11), Rigs.Runner());
                Spawn(ChassisRole.Sentry, new V2(15, 9), Rigs.Sentry());
            }
            else if (Wave == 2)
            {
                Spawn(ChassisRole.Runner, new V2(-15, -13), Rigs.Runner());
                Spawn(ChassisRole.Sentry, new V2(14, 13), Rigs.Sentry());
                Spawn(ChassisRole.Sentry, new V2(-15, 13), Rigs.Sentry());
            }
            else
            {
                Spawn(ChassisRole.Warden, new V2(0, 17), Rigs.Warden());
                Spawn(ChassisRole.Runner, new V2(-16, -7), Rigs.Runner());
                Spawn(ChassisRole.Sentry, new V2(16, 10), Rigs.Sentry());
            }
            UntilWave = 9;
            Signal?.Invoke(FieldEvent.Wave, Player.Position, false);
        }

        public void Step(float dt, V2 input)
        {
            if (Result != Outcome.Active || dt <= 0) return;
            Time += dt;
            if (Hostiles == 0 && Wave < TotalWaves)
            {
                UntilWave -= dt;
                if (UntilWave <= 0) StartWave();
            }

            Move(Player, input, dt);
            for (int i = 1; i < Units.Count; i++)
            {
                Unit enemy = Units[i];
                Move(enemy, Steering(enemy), dt);
                // A sentry can pivot toward its contact while it holds its ground.
                if (enemy.Role != ChassisRole.Runner)
                    enemy.Forward = Ballistics.Direction(Player.Position - enemy.Position);
            }
            foreach (Unit unit in Units)
            {
                unit.Reload = Math.Max(0, unit.Reload - dt);
                V2 source = Ballistics.Source(unit, Time);
                Unit target = Ballistics.Select(unit, Units, source, blocked);
                if (target == null) continue;
                unit.Aim = Ballistics.AimAt(unit, target, source);
                if (unit.Reload > 0 || unit.Aim == V2.Zero) continue;
                Item gun = unit.Rig.At(Socket.Weapon);
                unit.Reload = 1 / gun.ShotsPerSecond;
                Bolts.Add(new Bolt { Id = ++nextId, Position = source, Direction = unit.Aim,
                    Speed = gun.BoltSpeed, Remaining = gun.Range, Damage = gun.Damage, Friendly = unit.Friendly });
                Signal?.Invoke(FieldEvent.Shot, source, unit.Friendly);
            }
            AdvanceBolts(dt);
        }

        private V2 Steering(Unit enemy)
        {
            V2 toward = Player.Position - enemy.Position;
            float distance = toward.Length();
            V2 dir = Ballistics.Direction(toward);
            float searchRange = enemy.Rig.At(Socket.Weapon).Range * (enemy.Rig[Socket.Where] == ItemId.NearField ? .5f : 1);
            float standOff = searchRange * .76f;
            if (distance <= standOff && !Blocked(enemy.Position, Player.Position))
            {
                if (enemy.Role != ChassisRole.Runner) return V2.Zero;
                dir = new V2(-dir.Y, dir.X) * (enemy.Id % 2 == 0 ? 1 : -1) + dir * ((distance - standOff * .78f) * .4f);
            }
            foreach (Cover obstacle in Barriers)
            {
                V2 toCover = obstacle.Position - enemy.Position;
                float ahead = V2.Dot(toCover, dir);
                if (ahead <= 0 || ahead >= obstacle.Radius + 2) continue;
                V2 side = new V2(-dir.Y, dir.X);
                float cross = V2.Dot(toCover, side);
                if (Math.Abs(cross) < obstacle.Radius + UnitRadius)
                    dir += side * (cross >= 0 ? -1.8f : 1.8f);
            }
            return Ballistics.Direction(dir) * (enemy.Role == ChassisRole.Runner ? .72f : .67f);
        }

        private void Move(Unit unit, V2 motion, float dt)
        {
            V2 start = unit.Position;
            if (motion.LengthSquared() > 1) motion = V2.Normalize(motion);
            float speed = unit.Rig.HasDrive ? unit.Rig.At(Socket.Hull).Speed : 0;
            V2 end = start + motion * (speed * dt);
            foreach (Cover barrier in Barriers)
                end = PushOutside(end, barrier.Position, barrier.Radius + UnitRadius);
            foreach (Unit other in Units)
                if (other != unit && other.Alive) end = PushOutside(end, other.Position, UnitRadius * 1.7f);
            end = V2.Clamp(end, new V2(-Limit), new V2(Limit));
            unit.Position = end;
            unit.Velocity = (end - start) / dt;
            if (unit.Velocity.LengthSquared() > .04f) unit.Forward = Ballistics.Direction(unit.Velocity);
        }

        private static V2 PushOutside(V2 point, V2 center, float radius)
        {
            V2 delta = point - center;
            float dist = delta.LengthSquared();
            if (dist >= radius * radius) return point;
            return center + (dist < .000001f ? V2.UnitX : delta / (float)Math.Sqrt(dist)) * radius;
        }

        private static float CoverHit(V2 start, V2 dir, float length)
        {
            float nearest = float.PositiveInfinity;
            foreach (Cover cover in Barriers)
                nearest = Math.Min(nearest, Ballistics.HitDistance(start, dir, length, cover.Position, cover.Radius));
            return nearest;
        }

        private static bool Blocked(V2 from, V2 to)
        {
            V2 delta = to - from;
            return delta.LengthSquared() > .001f && CoverHit(from, V2.Normalize(delta), delta.Length()) < float.PositiveInfinity;
        }

        private void AdvanceBolts(float dt)
        {
            for (int i = Bolts.Count - 1; i >= 0; i--)
            {
                Bolt b = Bolts[i];
                float travel = Math.Min(b.Speed * dt, b.Remaining);
                float first = CoverHit(b.Position, b.Direction, travel);
                Unit hit = null;
                foreach (Unit unit in Units)
                {
                    if (!unit.Alive || unit.Friendly == b.Friendly) continue;
                    float d = Ballistics.HitDistance(b.Position, b.Direction, travel, unit.Position, UnitRadius);
                    if (d < first) { first = d; hit = unit; }
                }
                if (first < float.PositiveInfinity)
                {
                    if (hit != null) Damage(hit, b.Damage);
                    Signal?.Invoke(FieldEvent.Hit, b.Position + b.Direction * first, b.Friendly);
                    Bolts.RemoveAt(i);
                }
                else
                {
                    b.Position += b.Direction * travel;
                    b.Remaining -= travel;
                    if (b.Remaining <= 0) Bolts.RemoveAt(i);
                }
                if (Result == Outcome.Destroyed) break;
            }
        }

        private void Damage(Unit unit, float damage)
        {
            unit.Integrity = Math.Max(0, unit.Integrity - damage / Math.Max(1, unit.MaxHull));
            if (unit.Alive) return;
            if (unit.Friendly) Result = Outcome.Destroyed;
            else
            {
                Wrecks.Add(new Wreck(++nextId, unit.Position, unit.Name, false, unit.Rig.Contents()));
                Units.Remove(unit);
            }
            Signal?.Invoke(FieldEvent.Wreck, unit.Position, unit.Friendly);
        }
    }
}
