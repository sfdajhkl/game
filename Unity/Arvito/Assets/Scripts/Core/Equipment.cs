using System;

namespace Arvito
{
    public enum Socket { Hull, Core, Weapon, Who, When, Where, What, Why, How }

    public enum ItemId
    {
        Empty, Mule, Skater, Bastion, Cell, Dynamo, Furnace, Riveter, Repeater, Rail,
        Chassis, Relay, Always, Moving, Stationary, InRange, ForwardArc, NearField,
        Hostile, Damaged, Closest, Weakest, Strongest, Fire, Lead
    }

    public sealed class Item
    {
        public readonly ItemId Id;
        public readonly Socket Socket;
        public readonly string Name, Description, Model;
        public readonly float Hull, Speed, Range, Damage, ShotsPerSecond, BoltSpeed;
        public readonly int Draw, Output;

        public Item(ItemId id, Socket socket, string name, string description, string model = "",
            float hull = 0, float speed = 0, float range = 0, float damage = 0,
            float shots = 0, float bolt = 0, int draw = 0, int output = 0)
        {
            Id = id; Socket = socket; Name = name; Description = description; Model = model;
            Hull = hull; Speed = speed; Range = range; Damage = damage; ShotsPerSecond = shots;
            BoltSpeed = bolt; Draw = draw; Output = output;
        }

        public string Specs()
        {
            switch (Socket)
            {
                case Socket.Hull: return $"내구 {Hull:0}  ·  이동 {Speed:0.0} m/s  ·  소비 {Draw} W";
                case Socket.Core: return $"공급 출력 {Output} W";
                case Socket.Weapon: return $"피해 {Damage:0}  ·  사거리 {Range:0} m  ·  초당 {ShotsPerSecond:0.0}발  ·  소비 {Draw} W";
                default: return Description;
            }
        }
    }

    public static class Parts
    {
        public static readonly string[] Labels = { "차체", "동력원", "무기", "누가", "언제", "어디서", "무엇을", "왜", "어떻게" };

        public static readonly Item[] All = {
            null,
            new Item(ItemId.Mule, Socket.Hull, "노새 궤도차", "공사장의 운반 로봇에서 떼어 낸 균형 잡힌 차체.", "Mule", hull:100, speed:4.4f, draw:3),
            new Item(ItemId.Skater, Socket.Hull, "질주 차대", "가벼운 네 바퀴로 빠르게 자리를 바꾼다.", "Skater", hull:68, speed:5.7f, draw:2),
            new Item(ItemId.Bastion, Socket.Hull, "방벽 궤도차", "두꺼운 장갑이 충격을 버틴다. 이동은 느리다.", "Bastion", hull:155, speed:3.4f, draw:5),
            new Item(ItemId.Cell, Socket.Core, "축전 셀", "폐쇄된 중계소의 표준 전력 카세트.", "Cell", output:9),
            new Item(ItemId.Dynamo, Socket.Core, "유도 발전기", "노출된 구리 코일에서 추가 출력을 얻는다.", "Dynamo", output:13),
            new Item(ItemId.Furnace, Socket.Core, "산업 반응로", "무거운 장비에 전력을 공급하는 산업용 코어.", "Furnace", output:18),
            new Item(ItemId.Riveter, Socket.Weapon, "리벳 발사기", "굵은 리벳을 일정한 간격으로 밀어낸다.", "Riveter", range:11, damage:16, shots:1.8f, bolt:19, draw:5),
            new Item(ItemId.Repeater, Socket.Weapon, "속사 코일건", "가벼운 탄을 짧은 간격으로 발사한다.", "Repeater", range:9, damage:8, shots:3.4f, bolt:21, draw:6),
            new Item(ItemId.Rail, Socket.Weapon, "장거리 레일건", "두 레일 사이에서 중탄을 가속한다.", "Rail", range:16, damage:30, shots:0.8f, bolt:26, draw:10),
            new Item(ItemId.Chassis, Socket.Who, "본체", "내 차체를 사거리와 발사의 기준점으로 삼는다."),
            new Item(ItemId.Relay, Socket.Who, "중계 드론", "본체 주위를 도는 드론에서 탐색하고 발사한다."),
            new Item(ItemId.Always, Socket.When, "항상", "이동 상태와 관계없이 명령을 실행한다."),
            new Item(ItemId.Moving, Socket.When, "이동 중", "차체가 움직이는 동안에만 실행한다."),
            new Item(ItemId.Stationary, Socket.When, "정지 중", "차체가 멈췄을 때만 실행한다."),
            new Item(ItemId.InRange, Socket.Where, "사거리 안", "발사 주체 주변의 무기 사거리 전체를 탐색한다."),
            new Item(ItemId.ForwardArc, Socket.Where, "전방 부채꼴", "차체 진행 방향을 기준으로 전방 120도 안에서 탐색한다."),
            new Item(ItemId.NearField, Socket.Where, "근거리", "무기 사거리의 안쪽 절반에서만 표적을 찾는다."),
            new Item(ItemId.Hostile, Socket.What, "적 기체", "반대 진영의 작동 중인 기체를 대상으로 삼는다."),
            new Item(ItemId.Damaged, Socket.What, "손상된 적", "내구도가 일부 소모된 적만 대상으로 삼는다."),
            new Item(ItemId.Closest, Socket.Why, "가장 가까운", "발사 주체와의 거리가 짧은 표적을 우선한다."),
            new Item(ItemId.Weakest, Socket.Why, "내구가 낮은", "남은 내구 수치가 가장 낮은 표적을 우선한다."),
            new Item(ItemId.Strongest, Socket.Why, "화력이 높은", "장착 무기의 초당 피해가 높은 표적을 우선한다."),
            new Item(ItemId.Fire, Socket.How, "직선 발사", "표적의 현재 위치를 향해 발사한다."),
            new Item(ItemId.Lead, Socket.How, "예측 발사", "표적의 속도로 탄착 위치를 예측해 발사한다.")
        };

        public static Item Get(ItemId id) => All[(int)id];
        public static string Name(ItemId id) => Get(id)?.Name ?? "빈 소켓";
    }

    public sealed class Loadout
    {
        private readonly ItemId[] slots = new ItemId[Parts.Labels.Length];
        public ItemId this[Socket socket] => slots[(int)socket];
        public Item At(Socket slot) => Parts.Get(this[slot]);
        public int Demand => (At(Socket.Hull)?.Draw ?? 0) + (At(Socket.Weapon)?.Draw ?? 0);
        public int Capacity => At(Socket.Core)?.Output ?? 0;
        public bool HasDrive => At(Socket.Hull) != null && At(Socket.Core) != null && Demand <= Capacity;

        public Loadout(params ItemId[] items)
        {
            foreach (ItemId id in items)
                if (id != ItemId.Empty) Put(Parts.Get(id).Socket, id);
        }

        internal void Put(Socket slot, ItemId id) => slots[(int)slot] = id;
        public ItemId[] Contents() => (ItemId[])slots.Clone();

        public bool Online
        {
            get
            {
                if (Demand > Capacity) return false;
                foreach (ItemId id in slots) if (id == ItemId.Empty) return false;
                return true;
            }
        }

        public string Sentence()
        {
            if (!Online) return "회로 대기  /  빈 소켓 또는 출력 부족";
            return $"{Parts.Name(this[Socket.Who])}  ·  {Parts.Name(this[Socket.When])}  ·  {Parts.Name(this[Socket.Where])}에서 " +
                $"{Parts.Name(this[Socket.Why])} {Parts.Name(this[Socket.What])}에 {Parts.Name(this[Socket.How])}";
        }
    }

    public sealed class Inventory
    {
        private readonly int[] counts = new int[Parts.All.Length];
        public int Count(ItemId id) => counts[(int)id];
        public void Add(ItemId id) { if (id != ItemId.Empty) counts[(int)id]++; }

        public bool Install(Loadout rig, Socket slot, ItemId id, out string reason)
        {
            if (id != ItemId.Empty && (Parts.Get(id).Socket != slot || Count(id) == 0))
            { reason = "이 소켓에 장착할 수 있는 재고가 없습니다."; return false; }

            ItemId old = rig[slot];
            if (old == id) { reason = "이미 장착되어 있습니다."; return false; }
            int draw = rig.Demand - (Parts.Get(old)?.Draw ?? 0) + (Parts.Get(id)?.Draw ?? 0);
            int capacity = slot == Socket.Core ? Parts.Get(id)?.Output ?? 0 : rig.Capacity;
            if (id != ItemId.Empty && slot <= Socket.Weapon && draw > capacity)
            { reason = $"출력 {draw - capacity} W 부족  ·  동력원을 바꾸거나 장비를 분리하세요."; return false; }

            if (id != ItemId.Empty) counts[(int)id]--;
            Add(old);
            rig.Put(slot, id);
            reason = id == ItemId.Empty ? $"{Parts.Name(old)} 분리" : $"{Parts.Name(id)} 장착";
            return true;
        }
    }

    public static class Rigs
    {
        public static Loadout Surveyor() => new Loadout(ItemId.Mule, ItemId.Cell, ItemId.Riveter, ItemId.Chassis, ItemId.Always, ItemId.InRange, ItemId.Hostile, ItemId.Closest, ItemId.Fire);
        public static Loadout Runner() => new Loadout(ItemId.Skater, ItemId.Cell, ItemId.Repeater, ItemId.Chassis, ItemId.Moving, ItemId.NearField, ItemId.Hostile, ItemId.Closest, ItemId.Lead);
        public static Loadout Sentry() => new Loadout(ItemId.Mule, ItemId.Dynamo, ItemId.Riveter, ItemId.Relay, ItemId.Stationary, ItemId.ForwardArc, ItemId.Hostile, ItemId.Weakest, ItemId.Fire);
        public static Loadout Warden() => new Loadout(ItemId.Bastion, ItemId.Furnace, ItemId.Rail, ItemId.Chassis, ItemId.Stationary, ItemId.InRange, ItemId.Hostile, ItemId.Strongest, ItemId.Fire);
    }
}
