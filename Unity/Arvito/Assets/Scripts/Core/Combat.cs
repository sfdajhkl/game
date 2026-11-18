using System;
using System.Collections.Generic;
using V2 = System.Numerics.Vector2;

namespace Arvito
{
    public enum ChassisRole { Player, Runner, Sentry, Warden }

    public sealed class Unit
    {
        public readonly int Id;
        public readonly ChassisRole Role;
        public readonly Loadout Rig;
        public V2 Position, Velocity, Forward = V2.UnitY, Aim = V2.UnitY;
        public float Integrity = 1f, Reload;
        public bool Alive => Integrity > 0;
        public bool Friendly => Role == ChassisRole.Player;
        public float MaxHull => Rig.At(Socket.Hull)?.Hull ?? 0;
        public float Health => MaxHull * Integrity;
        public string Name => Role == ChassisRole.Runner ? "질주병" : Role == ChassisRole.Sentry ? "중계 감시기" : Role == ChassisRole.Warden ? "방벽 수호기" : "탐사기";

        public Unit(int id, ChassisRole role, V2 xy, Loadout rig)
        { Id = id; Role = role; Position = xy; Rig = rig; }
    }

    public static class Ballistics
    {
        public static V2 Direction(V2 v) => v.LengthSquared() > 0.00001f ? V2.Normalize(v) : V2.Zero;

        public static V2 Source(Unit owner, float time)
        {
            if (owner.Rig[Socket.Who] != ItemId.Relay) return owner.Position;
            float angle = time * 1.7f + owner.Id;
            return owner.Position + new V2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 1.5f;
        }

        public static Unit Select(Unit owner, IReadOnlyList<Unit> units, V2 source, Func<V2, V2, bool> blocked = null)
        {
            Loadout rig = owner.Rig;
            if (!owner.Alive || !rig.Online) return null;
            bool moving = owner.Velocity.LengthSquared() > .04f;
            if (rig[Socket.When] == ItemId.Moving && !moving || rig[Socket.When] == ItemId.Stationary && moving)
                return null;

            float range = rig.At(Socket.Weapon).Range * (rig[Socket.Where] == ItemId.NearField ? .5f : 1f);
            Unit best = null;
            float bestValue = float.PositiveInfinity, bestDistance = float.PositiveInfinity;
            foreach (Unit target in units)
            {
                if (!target.Alive || target.Friendly == owner.Friendly) continue;
                if (rig[Socket.What] == ItemId.Damaged && target.Integrity >= 1f) continue;
                V2 delta = target.Position - source;
                float distance = delta.LengthSquared();
                if (distance > range * range) continue;
                if (rig[Socket.Where] == ItemId.ForwardArc && V2.Dot(owner.Forward, Direction(delta)) < .5f) continue;
                if (blocked != null && blocked(source, target.Position)) continue;

                float value = distance;
                if (rig[Socket.Why] == ItemId.Weakest) value = target.Health;
                if (rig[Socket.Why] == ItemId.Strongest)
                {
                    Item gun = target.Rig.At(Socket.Weapon);
                    value = gun != null ? -gun.Damage * gun.ShotsPerSecond : 0;
                }
                if (value < bestValue || value == bestValue && (distance < bestDistance ||
                    distance == bestDistance && (best == null || target.Id < best.Id)))
                { best = target; bestValue = value; bestDistance = distance; }
            }
            return best;
        }

        public static V2 AimAt(Unit owner, Unit target, V2 source)
        {
            V2 delta = target.Position - source;
            if (owner.Rig[Socket.How] != ItemId.Lead) return Direction(delta);
            float speed = owner.Rig.At(Socket.Weapon).BoltSpeed;
            V2 velocity = target.Velocity;
            float a = velocity.LengthSquared() - speed * speed;
            float b = 2 * V2.Dot(delta, velocity);
            float c = delta.LengthSquared();
            float t = 0;
            if (Math.Abs(a) < .0001f)
            {
                if (b < 0) t = -c / b;
            }
            else
            {
                float discriminant = b * b - 4 * a * c;
                if (discriminant >= 0)
                {
                    float root = (float)Math.Sqrt(discriminant);
                    float t1 = (-b - root) / (2 * a), t2 = (-b + root) / (2 * a);
                    t = t1 > 0 && t2 > 0 ? Math.Min(t1, t2) : Math.Max(0, Math.Max(t1, t2));
                }
            }
            return Direction(delta + velocity * t);
        }

        // Distance to the first intersection, including a segment starting inside the circle.
        public static float HitDistance(V2 start, V2 direction, float length, V2 center, float radius)
        {
            V2 d = center - start;
            float along = V2.Dot(d, direction);
            float side = d.LengthSquared() - along * along;
            float rr = radius * radius;
            if (side > rr) return float.PositiveInfinity;
            float halfChord = (float)Math.Sqrt(Math.Max(0, rr - side));
            if (along + halfChord < 0) return float.PositiveInfinity;
            float entry = Math.Max(0, along - halfChord);
            return entry <= length ? entry : float.PositiveInfinity;
        }
    }
}
