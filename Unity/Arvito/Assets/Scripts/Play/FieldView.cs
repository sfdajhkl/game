using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using V2 = System.Numerics.Vector2;

namespace Arvito
{
    internal sealed class FieldView
    {
        internal static Vector3 World(V2 p, float height = 0) => new Vector3(p.X, height, p.Y);
        private static Quaternion Heading(V2 dir) => Quaternion.LookRotation(World(dir), Vector3.up);

        private readonly Transform root;
        private readonly Material[] paint = new Material[3];
        private readonly Material mint, amber, soot, brass;
        private readonly Dictionary<string, Mesh> models = new Dictionary<string, Mesh>();
        private readonly Dictionary<int, RigView> robots = new Dictionary<int, RigView>();
        private readonly Dictionary<int, Transform> wrecks = new Dictionary<int, Transform>();
        private readonly Dictionary<int, Transform> shots = new Dictionary<int, Transform>();
        private readonly HashSet<int> present = new HashSet<int>();
        private readonly List<int> expired = new List<int>();
        private readonly List<Spark> sparks = new List<Spark>();
        private readonly Shader metal;
        private readonly Texture2D palette;

        private sealed class RigView
        {
            public Transform Root, Hull, Turret, Drone, WeaponMount, Health, Bar;
            public ItemId Body, Gun, Core, Who;
            public float LastHp;
        }
        private sealed class Spark
        {
            public Transform View;
            public Vector3 Velocity;
            public float Life;
        }

        public FieldView(Transform scene)
        {
            root = Child("제9 중계소", scene);
            metal = Resources.Load<Shader>("Arvito/FieldMetal");
            palette = Resources.Load<Texture2D>("Arvito/FieldPalette");
            palette.filterMode = FilterMode.Point;
            palette.wrapMode = TextureWrapMode.Clamp;
            for (int i = 0; i < paint.Length; i++)
            {
                paint[i] = new Material(metal);
                paint[i].SetTexture("_Palette", palette);
                paint[i].SetFloat("_Row", i);
            }
            mint = Solid(new Color(.42f, .96f, .83f), true);
            amber = Solid(new Color(1f, .49f, .22f), true);
            brass = Solid(new Color(.85f, .66f, .35f), true);
            soot = Solid(new Color(.11f, .16f, .19f), false);
            Model("Basin", root, paint[0]);
            for (int i = 0; i < Field.Barriers.Length; i++)
            {
                Cover cover = Field.Barriers[i];
                Transform obstacle = Model("Roadblock", root, paint[0]);
                obstacle.position = World(cover.Position, .08f);
                obstacle.localScale = Vector3.one * (cover.Radius / 1.2f);
                obstacle.rotation = Quaternion.Euler(0, i % 2 == 0 ? 6 : 95, 0);
            }
            // The antenna stands beside the vehicle docking circle.
            Transform beacon = Model("RelayPost", root, paint[0]);
            beacon.position = World(Field.Beacon + new V2(-2.55f, 0));
        }

        private Material Solid(Color color, bool glow)
        {
            Material material = new Material(metal);
            material.SetTexture("_Palette", palette);
            material.SetColor("_Tint", color);
            material.SetFloat("_Solid", 1);
            material.SetFloat("_Glow", glow ? 1 : 0);
            return material;
        }

        private static Transform Child(string name, Transform parent)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        private Transform Model(string name, Transform parent, Material skin)
        {
            if (!models.TryGetValue(name, out Mesh mesh))
                models[name] = mesh = Resources.Load<Mesh>("Arvito/Models/" + name);
            Transform t = Child(name, parent);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = skin;
            return t;
        }

        private Transform Box(string name, Transform parent, Vector3 size, Material material)
        {
            Transform t = Model("Indicator", parent, material);
            t.name = name;
            t.localScale = size;
            t.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return t;
        }

        private void BuildRobot(Unit unit, RigView v)
        {
            if (v.Hull != null)
            {
                Object.Destroy(v.Hull.gameObject);
                Object.Destroy(v.Turret.gameObject);
                Object.Destroy(v.Drone.gameObject);
            }
            Material skin = paint[unit.Friendly ? 0 : 1];
            v.Hull = Child("차체", v.Root);
            v.Turret = Child("포탑", v.Root);
            v.Drone = Child("중계 드론", v.Root);
            v.Body = unit.Rig[Socket.Hull]; v.Gun = unit.Rig[Socket.Weapon];
            v.Core = unit.Rig[Socket.Core]; v.Who = unit.Rig[Socket.Who];
            if (v.Body != ItemId.Empty) Model(Parts.Get(v.Body).Model, v.Hull, skin);
            if (v.Core != ItemId.Empty)
                Model(Parts.Get(v.Core).Model, v.Hull, skin).localPosition = new Vector3(0, .86f, -.52f);
            v.Turret.localPosition = new Vector3(0, 1.08f, .04f);
            v.WeaponMount = v.Who == ItemId.Relay ? v.Drone : v.Turret;
            v.Drone.gameObject.SetActive(v.Who == ItemId.Relay);
            if (v.Who == ItemId.Relay)
            {
                Box("동체", v.Drone, new Vector3(.60f,.17f,.42f), soot);
                for (int side = -1; side <= 1; side += 2)
                    Box("회전익", v.Drone, new Vector3(.33f,.05f,.16f), unit.Friendly ? mint : amber).localPosition = new Vector3(side*.39f,0,0);
            }
            if (v.Gun != ItemId.Empty)
            {
                Transform gun = Model(Parts.Get(v.Gun).Model, v.WeaponMount, skin);
                if (v.Who == ItemId.Relay) gun.localScale = Vector3.one * .72f;
            }
        }

        public void Sync(Field f, Camera camera, float dt)
        {
            present.Clear();
            foreach (Unit u in f.Units)
            {
                present.Add(u.Id);
                if (!robots.TryGetValue(u.Id, out RigView v))
                {
                    v = new RigView { Root = Child(u.Name, root), LastHp = u.Integrity };
                    v.Health = Child("내구", v.Root);
                    Box("바탕", v.Health, new Vector3(1.22f,.11f,.05f), soot);
                    v.Bar = Box("내구량", v.Health, Vector3.one, u.Friendly ? mint : amber);
                    robots.Add(u.Id, v);
                }
                if (v.Hull == null || v.Body != u.Rig[Socket.Hull] || v.Gun != u.Rig[Socket.Weapon] ||
                    v.Core != u.Rig[Socket.Core] || v.Who != u.Rig[Socket.Who]) BuildRobot(u, v);
                v.Root.position = World(u.Position);
                v.Hull.rotation = Quaternion.RotateTowards(v.Hull.rotation, Heading(u.Forward), 580 * dt);
                v.WeaponMount.rotation = Quaternion.RotateTowards(v.WeaponMount.rotation, Heading(u.Aim), 980 * dt);
                if (v.Who == ItemId.Relay)
                    v.Drone.position = World(Ballistics.Source(u, f.Time), 1.44f + .09f * Mathf.Sin(f.Time * 4));
                v.Health.position = v.Root.position + Vector3.up * 2.06f;
                v.Health.rotation = camera.transform.rotation;
                v.Health.gameObject.SetActive(u.Integrity < .995f && u.Alive);
                v.Bar.localPosition = new Vector3((u.Integrity - 1) * .57f, 0, -.045f);
                v.Bar.localScale = new Vector3(1.14f * u.Integrity, .055f, .025f);
                if (u.Integrity < v.LastHp) Emit(v.Root.position + Vector3.up * .85f, 5, u.Friendly ? mint : amber);
                v.LastHp = u.Integrity;
            }
            expired.Clear();
            foreach (var p in robots) if (!present.Contains(p.Key)) expired.Add(p.Key);
            foreach (int id in expired) { Object.Destroy(robots[id].Root.gameObject); robots.Remove(id); }

            present.Clear();
            foreach (Wreck wreck in f.Wrecks)
            {
                present.Add(wreck.Id);
                if (wrecks.ContainsKey(wreck.Id)) continue;
                Transform site = Child(wreck.Name, root);
                site.position = World(wreck.Position);
                Transform body = Model(wreck.Cache ? "Cache" : Parts.Get(wreck.Cargo[(int)Socket.Hull]).Model, site, paint[wreck.Cache ? 0 : 2]);
                body.localRotation = Quaternion.Euler(wreck.Cache ? 0 : -13f, wreck.Id * 47 % 360, wreck.Cache ? 0 : 17f);
                Transform lamp = Box("회수 신호", site, new Vector3(.18f,.18f,.18f), brass);
                lamp.localPosition = Vector3.up * 1.45f;
                lamp.localRotation = Quaternion.Euler(0, 45, 45);
                wrecks.Add(wreck.Id, site);
            }
            Sweep(wrecks);
            present.Clear();
            foreach (Bolt bolt in f.Bolts)
            {
                present.Add(bolt.Id);
                if (!shots.TryGetValue(bolt.Id, out Transform tracer))
                {
                    tracer = Box("탄광", root, new Vector3(.09f,.09f,.59f), bolt.Friendly ? mint : amber);
                    shots.Add(bolt.Id, tracer);
                }
                tracer.position = World(bolt.Position, 1.13f);
                tracer.rotation = Heading(bolt.Direction);
            }
            Sweep(shots);
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                Spark s = sparks[i];
                s.Life -= dt;
                if (s.Life <= 0) { Object.Destroy(s.View.gameObject); sparks.RemoveAt(i); continue; }
                s.Velocity += Vector3.down * (10 * dt);
                s.View.position += s.Velocity * dt;
            }
        }

        private void Sweep(Dictionary<int, Transform> views)
        {
            expired.Clear();
            foreach (var p in views) if (!present.Contains(p.Key)) expired.Add(p.Key);
            foreach (int id in expired) { Object.Destroy(views[id].gameObject); views.Remove(id); }
        }

        private void Emit(Vector3 position, int count, Material material)
        {
            for (int i = 0; i < count; i++)
            {
                Transform t = Box("불꽃", root, Vector3.one * Random.Range(.065f,.13f), material);
                t.position = position;
                sparks.Add(new Spark { View = t, Velocity = Random.insideUnitSphere * 3.2f + Vector3.up * 1.2f, Life = Random.Range(.12f,.34f) });
            }
        }

        public void Effect(FieldEvent e, V2 point, bool friendly)
        {
            if (e == FieldEvent.Wreck || e == FieldEvent.Salvage || e == FieldEvent.Hit)
                Emit(World(point, .85f), e == FieldEvent.Wreck ? 13 : 4, friendly ? mint : amber);
        }

        public void Dispose()
        {
            Object.Destroy(root.gameObject);
            foreach (Material m in paint) Object.Destroy(m);
            Object.Destroy(mint); Object.Destroy(amber); Object.Destroy(soot); Object.Destroy(brass);
        }
    }
}
