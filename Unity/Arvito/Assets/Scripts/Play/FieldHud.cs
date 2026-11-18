using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using V2 = System.Numerics.Vector2;

namespace Arvito
{
    internal sealed class FieldHud
    {
        private static readonly Color Ink = Hex("111d24"), Plate = Hex("1b2b33"), SlotColor = Hex("253942"),
            Line = Hex("3c5158"), TextColor = Hex("e5e6d8"), Muted = Hex("a2b6b7"), Mint = Hex("8fe5cd"),
            Gold = Hex("f0bc74"), Orange = Hex("f08157");
        private static readonly Vector2 TopLeft = new Vector2(0,1), TopRight = Vector2.one,
            BottomLeft = Vector2.zero, BottomRight = Vector2.right, Center = new Vector2(.5f,.5f);
        private readonly RectTransform canvas;
        private readonly Font font;
        private readonly Action begin, toggleBench, restart;
        private readonly GameObject hud, intro, bench, end;
        private readonly RectTransform benchCard, map;
        private readonly Text hull, power, wave, threat, rule, prompt, notice, endTitle, endInfo;
        private readonly RectTransform healthBar;
        private readonly List<Image> dots = new List<Image>();
        private RectTransform benchPage;
        private Field field;
        private Socket selection = Socket.Who;
        private bool chips = true;
        private string benchMessage = "";
        private float messageUntil;
        private string cachedRule;
        private int lastHp = -1, lastMax = -1, lastDemand = -1, lastCapacity = -1;
        private int lastWave = -1, lastHostiles = -1, lastCountdown = -2;
        public readonly RawImage Pixels;

        public FieldHud(Transform parent, Action onBegin, Action onBench, Action onRestart)
        {
            begin = onBegin; toggleBench = onBench; restart = onRestart;
            font = Resources.Load<Font>("Arvito/FieldSans");
            var obj = new GameObject("계기판", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            obj.transform.SetParent(parent, false);
            canvas = obj.GetComponent<RectTransform>();
            obj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            RectTransform surface = Fill(canvas, "도트 화면");
            Pixels = surface.gameObject.AddComponent<RawImage>();
            Pixels.raycastTarget = false;

            RectTransform h = Fill(canvas, "현장 계기판");
            hud = h.gameObject;
            RectTransform vitals = Panel(h, TopLeft, 27,-23, 268,90, Ink);
            Strip(vitals, 0,0,4,90,Mint);
            Label(vitals, 16,5,151,25,"탐사기  AR-01",16,Mint,true);
            hull = Label(vitals, 155,5,96,25,"",16,TextColor,true,TextAnchor.MiddleRight);
            Strip(vitals, 16,36,235,7,Line);
            healthBar = Strip(vitals, 16,36,235,7,Mint);
            power = Label(vitals, 16,48,235,30,"",14,Muted);

            RectTransform station = Panel(h, TopRight, -26,-23, 265,111, Ink);
            map = Panel(station, TopLeft, 8,-8,94,94,SlotColor);
            Strip(map, 46,4,1,86,Line); Strip(map, 4,46,86,1,Line);
            Label(station, 116,5,143,23,"제9 중계소",14,Muted);
            wave = Label(station, 115,32,142,32,"",22,TextColor,true);
            threat = Label(station, 115,69,146,28,"",14,Gold);

            RectTransform circuit = Panel(h, BottomLeft, 27,21, 759,63,Ink);
            Label(circuit, 15,4,724,20,"행동 회로",12,Mint,true);
            rule = Label(circuit, 15,24,724,32,"",16,TextColor);
            RectTransform benchButton = Panel(h, BottomRight, -27,21, 166,51,SlotColor);
            Button(benchButton,0,0,166,51,"Tab   조립대",Mint,Ink,toggleBench);
            prompt = Label(h, 0,0,690,42,"",20,Gold,true,TextAnchor.MiddleCenter);
            Place(prompt.rectTransform,new Vector2(.5f,0),0,98,690,42);
            OutlineText(prompt);
            notice = Label(h,0,0,760,39,"",18,TextColor,true,TextAnchor.MiddleCenter);
            Place(notice.rectTransform,new Vector2(.5f,1),0,-30,760,39);
            OutlineText(notice);

            RectTransform landing = Fill(canvas,"출격");
            intro = landing.gameObject;
            var landingCard = Panel(landing, new Vector2(0,.5f), 54,0, 458,410,Ink);
            Strip(landingCard,0,0,5,410,Gold);
            Label(landingCard,30,25,400,30,"제9 중계소   /   회수 작전",16,Gold,true);
            Label(landingCard,26,55,410,91,"ARVITO",77,TextColor,true);
            Label(landingCard,32,148,392,36,"아르비토",25,Mint,true);
            Strip(landingCard,32,200,393,1,Line);
            Label(landingCard,32,214,395,67,"멸망한 지구에서 깨어난 탐사기. 고철에서 부품과 행동 칩을 회수하고, 세 번의 습격을 견뎌 중계소로 귀환하세요.",17,TextColor);
            Label(landingCard,32,286,400,30,"WASD  이동      E  회수      Tab  조립",15,Muted);
            Button(landingCard,32,332,392,50,"Enter   출격",Mint,Ink,begin);

            RectTransform workshop = Fill(canvas, "조립대");
            bench = workshop.gameObject;
            Shade(workshop, new Color(.024f,.045f,.055f,.76f));
            benchCard = Panel(workshop, Center, 0,0, 1050,628,Ink);
            Strip(benchCard,0,0,1050,4,Mint);
            Label(benchCard,29,18,205,44,"현장 조립대",30,TextColor,true);
            Label(benchCard,244,25,350,33,"정비 중  /  전투 일시정지",15,Mint);
            Button(benchCard,846,20,175,40,"Tab   현장 복귀",SlotColor,TextColor,toggleBench);

            RectTransform result = Fill(canvas,"작전 결과");
            end = result.gameObject;
            Shade(result,new Color(.024f,.045f,.055f,.66f));
            RectTransform endCard = Panel(result,Center,0,0,530,280,Ink);
            Strip(endCard,0,0,530,4,Gold);
            Label(endCard,30,19,467,30,"제9 중계소  /  작전 기록",16,Gold,true);
            endTitle = Label(endCard,30,55,467,67,"",40,TextColor,true);
            endInfo = Label(endCard,32,129,466,50,"",17,Muted);
            Button(endCard,30,200,470,50,"Enter   다시 출격",Mint,Ink,restart);
        }

        public void Bind(Field value)
        {
            field = value;
            lastHp = lastMax = lastDemand = lastCapacity = lastWave = lastHostiles = lastCountdown = -2;
            cachedRule = null;
            benchMessage = "";
            notice.text = "";
        }

        public void Mode(PlayMode mode)
        {
            hud.SetActive(mode == PlayMode.Field);
            intro.SetActive(mode == PlayMode.Landing);
            bench.SetActive(mode == PlayMode.Bench);
            end.SetActive(mode == PlayMode.Ended);
            if (mode == PlayMode.Bench) Rebuild();
            if (mode == PlayMode.Ended)
            {
                endTitle.text = field.Result == Outcome.Extracted ? "귀환 신호 수신" : "기체 신호 두절";
                endInfo.text = $"습격 {field.Wave} / {Field.TotalWaves}    ·    회수 {field.Recoveries}회    ·    작동 {Mathf.FloorToInt(field.Time)}초";
            }
        }

        public void Message(string text)
        {
            notice.text = text;
            messageUntil = UnityEngine.Time.unscaledTime + 3;
        }

        public void BenchHint(string text) { benchMessage = text; Rebuild(); }

        public void UpdateField(Vector3 right, Vector3 forward)
        {
            var u = field.Player;
            int hp = Mathf.CeilToInt(u.Health), max = Mathf.RoundToInt(u.MaxHull);
            if (hp != lastHp || max != lastMax)
            {
                hull.text = $"{hp} / {max}";
                healthBar.sizeDelta = new Vector2(235 * u.Integrity,7);
                lastHp = hp; lastMax = max;
            }
            if (lastDemand != u.Rig.Demand || lastCapacity != u.Rig.Capacity)
            {
                lastDemand = u.Rig.Demand; lastCapacity = u.Rig.Capacity;
                power.text = $"출력 사용   {lastDemand} / {lastCapacity} W";
            }
            if (cachedRule == null) { cachedRule = u.Rig.Sentence(); rule.text = cachedRule; }
            int countdown = field.Hostiles == 0 && !field.CanExtract ? Mathf.CeilToInt(field.UntilWave) : -1;
            if (field.Wave != lastWave || field.Hostiles != lastHostiles || countdown != lastCountdown)
            {
                lastWave = field.Wave; lastHostiles = field.Hostiles; lastCountdown = countdown;
                wave.text = field.CanExtract ? "귀환 가능" : $"습격  {field.Wave:D2} / 03";
                threat.text = field.CanExtract ? "중계소로 복귀" : countdown >= 0 ? $"신호 접근  {countdown}초" : $"적 신호  {field.Hostiles}기";
            }
            Wreck near = field.Nearby();
            prompt.text = near != null ? $"[ E ]   {near.Name} 회수" :
                field.CanExtract ? field.AtBeacon ? "[ E ]   중계소 접속 · 귀환" : $"중계소까지  {V2.Distance(u.Position,Field.Beacon):0} m" : "";
            if (notice.text.Length > 0 && UnityEngine.Time.unscaledTime > messageUntil) notice.text = "";
            int idx = 0;
            Dot(ref idx, Field.Beacon, right, forward, Mint, 9);
            foreach (Wreck w in field.Wrecks) Dot(ref idx,w.Position,right,forward,Gold,4);
            foreach (Unit bot in field.Units)
                if (!bot.Friendly) Dot(ref idx,bot.Position,right,forward,Orange,5);
            Dot(ref idx,u.Position,right,forward,Color.white,6);
            for (int i = idx; i < dots.Count; i++) dots[i].enabled = false;
        }

        private void Dot(ref int idx, V2 p, Vector3 right, Vector3 forward, Color color, float size)
        {
            if (idx >= dots.Count)
            {
                RectTransform pin = New(map,"신호");
                Image img = pin.gameObject.AddComponent<Image>(); img.raycastTarget = false;
                dots.Add(img);
            }
            Image dot = dots[idx++];
            Vector3 world = FieldView.World(p);
            Place(dot.rectTransform, Center, Vector3.Dot(world,right)*1.60f, Vector3.Dot(world,forward)*1.60f,size,size);
            dot.color = color; dot.enabled = true;
        }

        private void Tab(bool showChips)
        {
            chips = showChips;
            selection = chips ? Socket.Who : Socket.Hull;
            benchMessage = ""; Rebuild();
        }

        private void Replace(ItemId id)
        {
            if (field.Inventory.Install(field.Player.Rig,selection,id,out benchMessage))
                cachedRule = null;
            Rebuild();
        }

        private void Rebuild()
        {
            if (benchPage != null) { benchPage.gameObject.SetActive(false); UnityEngine.Object.Destroy(benchPage.gameObject); }
            benchPage = Fill(benchCard,"소켓 목록");
            var rig = field.Player.Rig;
            Button(benchPage,29,77,178,42,"행동 칩  /  6",chips ? Mint : SlotColor,chips ? Ink : Muted,()=>Tab(true));
            Button(benchPage,216,77,155,42,"기체 부품  /  3",chips ? SlotColor : Gold,chips ? Muted : Ink,()=>Tab(false));
            Label(benchPage,404,80,615,34,$"차체 내구  {Mathf.CeilToInt(field.Player.Health)} / {field.Player.MaxHull:0}     ·     출력  {rig.Demand} / {rig.Capacity} W",16,Muted,false,TextAnchor.MiddleRight);
            Strip(benchPage,29,133,992,1,Line);
            int start = chips ? (int)Socket.Who : (int)Socket.Hull, count = chips ? 6 : 3;
            for (int i = 0; i < count; i++)
            {
                Socket slot = (Socket)(start+i);
                int y = 150+i*53;
                RectTransform row = Panel(benchPage,TopLeft,29,-y,340,46,selection == slot ? SlotColor : Plate);
                if (selection == slot) Strip(row,0,0,4,46,chips ? Mint : Gold);
                Label(row,13,0,84,46,Parts.Labels[(int)slot],16,Muted);
                Label(row,99,0,229,46,Parts.Name(rig[slot]),19,TextColor,true);
                Clickable(row,()=>{selection=slot;benchMessage="";Rebuild();});
            }
            RectTransform detail = Panel(benchPage,TopLeft,389,-150,632,353,Plate);
            Item current = rig.At(selection);
            Label(detail,20,12,406,32,$"{Parts.Labels[(int)selection]}  /  {current?.Name ?? "빈 소켓"}",23,TextColor,true);
            Label(detail,21,47,580,38,current?.Specs() ?? "인벤토리에서 회수한 모듈을 장착하세요.",15,Muted);
            if (current != null) Button(detail,522,14,88,30,"분리",SlotColor,Gold,()=>Replace(ItemId.Empty));
            Strip(detail,20,91,592,1,Line);
            Label(detail,20,100,587,26,"호환 모듈",15,chips ? Mint : Gold,true);
            int option = 0;
            foreach (Item item in Parts.All)
            {
                if (item == null || item.Socket != selection) continue;
                int y = 134+option++*68;
                int owned = field.Inventory.Count(item.Id);
                Label(detail,21,y,422,25,item.Name,17,TextColor,true);
                Label(detail,21,y+25,440,24,item.Specs(),13,Muted);
                bool installed = rig[selection] == item.Id;
                if (owned > 0 && !installed)
                    Button(detail,490,y+4,117,35,$"장착  ×{owned}",Mint,Ink,()=>Replace(item.Id));
                else Label(detail,490,y+4,117,35,installed ? "장착 중" : "미회수",14,installed ? Mint : Muted,false,TextAnchor.MiddleCenter);
            }
            Strip(benchPage,29,518,992,1,Line);
            Label(benchPage,29,530,992,31,rig.Sentence(),16,rig.Online ? Mint : Gold,true);
            string hint = chips ? "칩을 분리하면 인벤토리로 돌아갑니다. 여섯 소켓을 채우면 명령이 실행됩니다." :
                "부품의 사거리·연사·피해는 고정입니다. 동력원의 공급 출력 안에서 조합하세요.";
            Label(benchPage,29,568,992,35,benchMessage.Length == 0 ? hint : benchMessage,16,benchMessage.Length == 0 ? Muted : Gold);
        }

        private static Color Hex(string rgb) { ColorUtility.TryParseHtmlString("#"+rgb,out Color c); return c; }

        private static RectTransform New(RectTransform parent, string name)
        {
            var t = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            t.SetParent(parent,false); return t;
        }
        private static RectTransform Fill(RectTransform parent,string name)
        {
            RectTransform t=New(parent,name);
            t.anchorMin=Vector2.zero; t.anchorMax=Vector2.one; t.offsetMin=Vector2.zero; t.offsetMax=Vector2.zero;
            return t;
        }
        private static void Place(RectTransform t,Vector2 anchor,float x,float y,float w,float h)
        {
            t.anchorMin=anchor; t.anchorMax=anchor; t.pivot=anchor;
            t.anchoredPosition=new Vector2(x,y); t.sizeDelta=new Vector2(w,h);
        }
        private static Image Shade(RectTransform t,Color c)
        { Image box=t.gameObject.AddComponent<Image>(); box.color=c; box.raycastTarget=false; return box; }
        private static RectTransform Panel(RectTransform p,Vector2 anchor,float x,float y,float w,float h,Color c)
        { RectTransform t=New(p,"패널"); Place(t,anchor,x,y,w,h); Shade(t,c); return t; }
        private static RectTransform Strip(RectTransform p,float x,float y,float w,float h,Color c)
        { return Panel(p,TopLeft,x,-y,w,h,c); }
        private Text Label(RectTransform p,float x,float y,float w,float h,string content,int size,Color color,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft)
        {
            RectTransform t=New(p,"텍스트");
            Place(t,TopLeft,x,-y,w,h);
            Text text=t.gameObject.AddComponent<Text>();
            text.font=font; text.text=content; text.fontSize=size; text.color=color; text.alignment=align;
            text.fontStyle=bold ? FontStyle.Bold : FontStyle.Normal;
            text.raycastTarget=false; text.supportRichText=false;
            return text;
        }
        private static void OutlineText(Text t)
        {
            var edge=t.gameObject.AddComponent<Outline>(); edge.effectColor=Ink; edge.effectDistance=new Vector2(1,-1);
        }
        private void Button(RectTransform p,float x,float y,float w,float h,string caption,Color bg,Color fg,Action action)
        {
            RectTransform t=Strip(p,x,y,w,h,bg);
            Label(t,3,0,w-6,h,caption,16,fg,true,TextAnchor.MiddleCenter);
            Clickable(t,action);
        }
        private static void Clickable(RectTransform t,Action action)
        {
            Image target=t.GetComponent<Image>(); target.raycastTarget=true;
            var button=t.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic=target;
            var colors=button.colors; colors.highlightedColor=new Color(.82f,.91f,.92f); colors.pressedColor=new Color(.65f,.78f,.77f);
            button.colors=colors; button.navigation=new Navigation{mode=Navigation.Mode.None};
            button.onClick.AddListener(()=>action());
        }
    }
}
