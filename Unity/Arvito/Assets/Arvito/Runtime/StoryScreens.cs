using System;
using System.IO;
using System.Linq;
using Arvito.Core;
using TMPro;
using UnityEngine;

namespace Arvito.Runtime
{
    public sealed partial class GameApp
    {
        bool newLifeFemale;
        int selectedChoice = -1;
        string choiceEvent;
        void TitleScreen()
        {
            var picture = Ui.Node("Landscape", page); picture.anchorMin = new Vector2(.39f, 0); picture.anchorMax = Vector2.one; picture.offsetMin = picture.offsetMax = Vector2.zero;
            var raw = picture.gameObject.AddComponent<UnityEngine.UI.RawImage>(); raw.texture = EventArt.Load("q080_offer") ?? art["village"]; raw.enabled = raw.texture != null; raw.raycastTarget = false; picture.gameObject.AddComponent<ArtCrop>();
            var shade = Ui.Card(picture, "Shade", new Color(.23f, .16f, .12f, .12f)); Ui.Fill(shade);
            var cover = Ui.Card(page, "Book cover", Ui.Panel); cover.anchorMin = Vector2.zero; cover.anchorMax = new Vector2(Compact ? 1 : .40f, 1); cover.offsetMin = cover.offsetMax = Vector2.zero;
            var titleArea = Ui.Node("Title area", cover); Ui.Fill(titleArea, 0, 0, 48, 32);
            var stack = Ui.Scroll(titleArea, "Title content");
            var layout = stack.GetComponent<UnityEngine.UI.VerticalLayoutGroup>(); layout.padding = new RectOffset(48, 40, 20, 20); layout.spacing = 20;
            Ui.Text(stack, "A  R  V  I  T  O", 18, Ui.Gold);
            Ui.Text(stack, "아르비토", 60, Ui.Ink);
            Ui.Text(stack, "당신이 걸은 길이,\n당신의 이름이 된다.", 25, Ui.Gold);
            Ui.Text(stack, "열여섯의 봄부터 마지막 계절까지.\n선택과 만남으로 써 내려가는\n한 모험가의 일대기.", 18, Ui.Muted);
            Ui.Height(Ui.Node("Space", stack), 18);
            if (game != null) Ui.Button(stack, game.State.mode == "ended" ? "지난 삶의 마지막 장" : "이어서 읽기  ·  " + game.AgeLabel, () => { title = false; tab = "journey"; Render(); }, 58, true, true);
            Ui.Button(stack, "새로운 삶을 시작하다", NewLifeDialog, 58, true, game == null);
            Ui.Button(stack, "설정", Settings, 48);
            Ui.Text(stack, notice + "\n0.3.0 · 양피지 수첩", 12, Ui.Muted);
            if (Compact) return;
            var caption = Ui.Text(page, "아직 쓰이지 않은\n당신의 이야기", 19, Ui.Raised, TextAlignmentOptions.BottomRight); Ui.Fill(caption.rectTransform, 700, 44, 44, 48);
        }
        void NewLifeDialog()
        {
            newLifeFemale=false;
            var stack = Dialog("새로운 이름, 새로운 길", game == null ? "당신은 열여섯 살입니다.\n어떤 이름으로 첫 페이지를 열까요?" : "현재 삶은 별도 기록 파일로 보관합니다.\n새 인물은 열여섯 살부터 시작합니다.");
            var field = Ui.Card(stack, "Name input", Ui.Raised); Ui.Height(field, 64); Ui.Border(field);
            nameInput = field.gameObject.AddComponent<TMP_InputField>(); nameInput.targetGraphic = field.GetComponent<UnityEngine.UI.Image>(); nameInput.characterLimit = 12;
            var area = Ui.Node("Text area", field); Ui.Fill(area, 15, 15, 8, 8); area.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var text = Ui.Text(area, "", 24, Ui.Ink, TextAlignmentOptions.MidlineLeft); Ui.Fill(text.rectTransform);
            var placeholder = Ui.Text(area, "모험가의 이름", 24, Ui.Muted, TextAlignmentOptions.MidlineLeft); Ui.Fill(placeholder.rectTransform);
            nameInput.textViewport = area; nameInput.textComponent = text; nameInput.placeholder = placeholder; nameInput.contentType = TMP_InputField.ContentType.Standard; nameInput.lineType = TMP_InputField.LineType.SingleLine;
            nameInput.text = "여행자"; nameInput.onSubmit.AddListener(_ => StartLife());
            var portrait=Ui.Node("Hero appearance preview",stack);Ui.Height(portrait,128);
            var portraitImage=portrait.gameObject.AddComponent<PixelArtImage>();BattlerArt.Apply(portraitImage,"hero");
            UnityEngine.UI.Button appearanceButton=null;
            appearanceButton=Ui.Button(stack,"외형: 남성형  →  여성형으로 변경",()=>{
                newLifeFemale=!newLifeFemale;
                BattlerArt.Apply(portraitImage,Appearance.Sprite("hero",newLifeFemale));
                appearanceButton.GetComponentInChildren<TextMeshProUGUI>().text=newLifeFemale ? "외형: 여성형  →  남성형으로 변경" : "외형: 남성형  →  여성형으로 변경";
            },46);
            Ui.Text(stack, "직업은 정해져 있지 않습니다.\n손에 든 무기와 배운 기술이 당신의 길을 만듭니다.", 16, Ui.Muted);
            Ui.Button(stack, "첫 페이지를 펼친다", StartLife, 58, true, true); Ui.Button(stack, "돌아가기", CloseDialog);
            nameInput.ActivateInputField();
        }
        void StartLife()
        {
            string name = nameInput == null ? "여행자" : nameInput.text;
            // Archive before replacing the active run; abort on IO failure to avoid losing the old life.
            try
            {
                if (game != null)
                {
                    string folder = Path.Combine(Application.persistentDataPath, "Lives"); Directory.CreateDirectory(folder);
                    File.WriteAllText(Path.Combine(folder, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".json"), JsonUtility.ToJson(game.State, true));
                }
            }
            catch (IOException) { Notify("이전 기록을 보관하지 못했습니다. 저장 공간을 확인해 주세요."); return; }
            catch (UnauthorizedAccessException) { Notify("기록 폴더에 쓸 수 없습니다. 접근 권한을 확인해 주세요."); return; }
            game = Game.New(data, name, BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(), 0),newLifeFemale); title = false; tab = "journey"; selectedSkill = selectedTarget = lastActor = null;
            selectedChoice = -1; choiceEvent = null;
            CloseDialog(); Save(); Render();
        }
        RectTransform Chapter(string eyebrow, string heading, string illustration = null, string eventId = null)
        {
            var stack = Ui.Scroll(main, "Chapter");
            var eventImage = EventArt.Load(eventId);
            if (eventImage != null) Ui.EventIllustration(stack, eventImage);
            else if (illustration != null) Ui.Art(stack, art[illustration], 245);
            Ui.Text(stack, eyebrow, 13, Ui.Gold);
            Ui.Text(stack, heading, 34);
            return stack;
        }
        void StoryScreen()
        {
            var e = game.Event;
            if (choiceEvent != e.id) { choiceEvent = e.id; selectedChoice = -1; }
            RectTransform words;
            if (Compact)
            {
                words = Ui.Scroll(main, "Event page");
                Ui.Text(words, game.RegionName + "   /   " + game.StageName, 15, Ui.Muted);
                Ui.Text(words, e.title, 32);
                StoryIllustration(words, e.id, e.art, main.rect.width - 16);
                Ui.Text(words, e.place, 16, Ui.Muted);
            }
            else
            {
                var eyebrow = Ui.Text(main, game.RegionName + "   /   " + game.StageName + "   /   " + e.place, 15, Ui.Muted);
                Ui.Box(eyebrow.rectTransform, 0, 0, main.rect.width - 8, 30);
                var heading = Ui.Text(main, e.title, 34);
                float headingHeight = Mathf.Max(54, heading.GetPreferredValues(e.title, main.rect.width - 8, Mathf.Infinity).y);
                Ui.Box(heading.rectTransform, 0, 38, main.rect.width - 8, headingHeight);
                var pictureHost = Ui.Node("Illustrated page", main); Ui.Fill(pictureHost, 0, 0, headingHeight + 60, 0);
                pictureHost.anchorMax = new Vector2(.53f, 1); pictureHost.offsetMax = new Vector2(-24, -headingHeight - 60);
                var pictures = Ui.Scroll(pictureHost, "Illustration and note");
                StoryIllustration(pictures, e.id, e.art, main.rect.width * .53f - 40);
                Ui.Text(pictures, e.place, 16, Ui.Muted, TextAlignmentOptions.Center);
                Ui.Rule(pictures);
                Ui.Text(pictures, "여행자의 메모", 17, Ui.Gold);
                Ui.Text(pictures, "선택에 따라 시간이 흐릅니다.\n어느 길을 택할지는 당신에게 달려 있습니다.", 18, Ui.Muted);
                var storyHost = Ui.Node("Written page", main); Ui.Fill(storyHost, 0, 0, headingHeight + 60, 0);
                storyHost.anchorMin = new Vector2(.53f, 0); storyHost.offsetMin = new Vector2(8, 0);
                words = Ui.Scroll(storyHost, "Story and choices");
            }
            foreach (var paragraph in e.text) Ui.Text(words, paragraph, 24);
            Ui.Text(words, "어떻게 할까?", 17, Ui.Muted); Ui.Rule(words);
            var buttons = new UnityEngine.UI.Button[e.choices.Length];
            UnityEngine.UI.Button confirm = null;
            Action refresh = () =>
            {
                for (int n = 0; n < buttons.Length; n++)
                    buttons[n].targetGraphic.color = selectedChoice == n ? Ui.Rail : Ui.Raised;
                confirm.interactable = selectedChoice >= 0 && game.ChoiceLock(e.choices[selectedChoice]) == "";
                confirm.GetComponentInChildren<TextMeshProUGUI>().text = selectedChoice < 0 ? "먼저 길을 골라 주세요" : "이 길을 선택한다     →";
            };
            for (int i = 0; i < e.choices.Length; i++)
            {
                int index = i; var c = e.choices[i]; string locked = game.ChoiceLock(c);
                string detail = Game.Duration(c.weeks) + " 소요";
                if (c.check != null && !string.IsNullOrEmpty(c.check.stat)) detail += "   ·   " + Stats.Label(c.check.stat) + " 판정 " + game.CheckChance(c.check) + "%";
                if (!string.IsNullOrEmpty(c.hint)) detail += "   ·   " + c.hint;
                if (locked != "") detail = locked;
                Action select = () => { selectedChoice = index; refresh(); };
                shortcuts.Add(locked == "" ? select : null);
                string label = (i + 1).ToString("00") + "   " + c.label + "\n<size=15><color=#6B4D34>      " + detail + "</color></size>";
                var button = Ui.Button(words, label, select, 84 * Ui.TextScale, locked == ""); buttons[i] = button;
                var text = button.GetComponentInChildren<TextMeshProUGUI>();
                float width = Compact ? main.rect.width - 52 : main.rect.width * .47f - 52;
                Ui.Height((RectTransform)button.transform, Mathf.Max(84 * Ui.TextScale, text.GetPreferredValues(label, width, Mathf.Infinity).y + 24));
            }
            var currentPage = page;
            enterAction = () =>
            {
                // A focused button, repeated key, or old page callback cannot apply the same choice twice.
                if (dialog != null || page != currentPage || game.State.mode != "story" || game.Event.id != e.id ||
                    selectedChoice < 0 || selectedChoice >= e.choices.Length || game.ChoiceLock(e.choices[selectedChoice]) != "") return;
                int index = selectedChoice; selectedChoice = -1;
                Change(() => game.Choose(index));
            };
            confirm = Ui.Button(words, "먼저 길을 골라 주세요", enterAction, 56, true, true); refresh();
            Ui.Text(words, "1–9 길 고르기   ·   Enter 선택 확정", 14, Ui.Muted);
            if (Compact)
            {
                Ui.Rule(words);
                Ui.Button(words, "다른 지역으로", TravelDialog, 48, game.State.mode == "story");
                Ui.Button(words, "불가에서 쉬기", CampDialog, 48, game.State.mode == "story");
            }
        }
        void StoryIllustration(Transform parent, string id, string fallback, float width)
        {
            float canvasScale = canvas.GetComponent<Canvas>().scaleFactor;
            int scale = Mathf.Clamp(Mathf.FloorToInt((width - 8) * canvasScale / 192), 1, 3);
            float height = 128 * scale / canvasScale + 8;
            var texture = EventArt.Load(id);
            if (texture != null) Ui.EventIllustration(parent, texture, height);
            else Ui.Art(parent, fallback != null && art.TryGetValue(fallback, out var background) ? background : null, height);
        }
        void TravelDialog()
        {
            var stack = Dialog("이 땅을 떠나 다른 길로", "현재 위치: " + game.RegionName + "\n" + game.StageName + "\n\n이동에는 2주와 식량 1개가 듭니다. 식량이 없으면 6골드를 냅니다.");
            foreach (var region in game.Routes())
            {
                string id = region.id;
                Ui.Text(stack, region.description, 16, Ui.Muted);
                Ui.Button(stack, region.name + "으로", () => { CloseDialog(); string message = ""; Change(() => message = game.Travel(id)); Notify(message); }, 50, game.State.rations > 0 || game.State.gold >= 6);
            }
            Ui.Button(stack, "지금의 길에 머문다", CloseDialog);
        }
        void OutcomeScreen()
        {
            var o = game.State.outcome; var stack = Chapter(o.combatWin ? "전투의 끝   /   살아남은 사람들" : "기록된 선택   /   " + game.AgeLabel, o.title, game.Event.art, game.Event.id);
            if (o.weeks > 0) Ui.Text(stack, Game.Duration(o.weeks) + "의 시간이 흘렀다.", 16, Ui.Gold);
            if (o.hasCheck) Ui.Text(stack, o.checkStat + " 판정 " + (o.success ? "성공" : "실패") + "  ·  성공 확률 " + o.chance + "%", 16, o.success ? Ui.Green : Ui.Red);
            Ui.Text(stack, o.body, 23);
            if (o.changes.Count > 0) Ui.Text(stack, string.Join("   /   ", o.changes), 16, Ui.Gold);
            enterAction = () => Change(() => game.Continue()); shortcuts.Add(enterAction);
            Ui.Button(stack, "다음 페이지로                 Enter", enterAction, 60, true, true);
        }
        void InventoryScreen()
        {
            var stack = Chapter("BELONGINGS   /   손에 익은 것들", "소지품");
            Ui.Text(stack, "식량 " + game.State.rations + "   ·   회복약 " + game.State.potions + "   ·   소지금 " + game.State.gold + " G", 19, Ui.Gold);
            Ui.Text(stack, "장비가 바뀌면 사용할 수 있는 기술도 바뀝니다.\n모험에서 직접 배운 기술은 계속 남습니다.", 18, Ui.Muted);
            foreach (string id in game.State.inventory)
            {
                var w = data.Weapon(id); bool equipped = game.State.hero.weapon == id;
                Ui.Text(stack, w.name + (equipped ? "  <size=15><color=#79573A>착용 중</color></size>" : ""), 24, Ui.Gold);
                Ui.Text(stack, w.desc + "\n위력 " + w.power + "  ·  속도 " + w.speed + "  ·  숙련 " + Game.Count(game.State.hero.mastery, id), 16, Ui.Muted);
                Ui.Text(stack, string.Join("   /   ", w.skills.Select(s => data.Skill(s).name)), 17);
                Ui.Button(stack, equipped ? "현재 손에 든 무기" : "이 무기를 든다", () => Change(() => game.Equip(id)), 48, !equipped && game.State.mode != "combat" && game.State.mode != "ended");
            }
            Ui.Text(stack, "배운 기술", 24, Ui.Gold);
            Ui.Text(stack, game.State.hero.skills.Count == 0 ? "아직은 빈 페이지입니다. 사람을 만나고 새로운 길을 걸어 보세요." : string.Join("\n\n", game.State.hero.skills.Select(id => data.Skill(id).name + "\n" + data.Skill(id).desc)), 18);
            Ui.Text(stack, "전투 중에는 무기를 바꿀 수 없습니다. 위치 교대 기술로 전열과 후열을 조정하세요.", 13, Ui.Muted);
        }
        void PartyScreen()
        {
            var stack = Chapter("COMPANIONS   /   우연에서 인연으로", "함께 걷는 길");
            Ui.Text(stack, "앞에 있는 사람이 전열입니다. 주인공만 직접 조작하고 동료는 자동으로 행동합니다.\n최대 네 명이 함께하며, 쓰러진 동료는 돌아오지 못합니다.", 18, Ui.Muted);
            foreach (var a in game.Party)
            {
                string id = a.id;
                var portrait=Ui.Node("Party illustration",stack);Ui.Height(portrait,170);BattlerArt.Portrait(portrait,Appearance.Sprite(a));
                Ui.Text(stack, game.Rank(id).ToString("00") + "   " + Ui.Safe(a.name), 26, Ui.Gold);
                Ui.Text(stack, a.id == "hero" ? "한 가지 직업으로 정해지지 않은 모험가. 당신의 선택이 앞으로의 이야기를 만듭니다." : a.story, 19);
                Ui.Text(stack, data.Weapon(a.weapon).name + "  ·  체력 " + a.hp + "/" + a.maxHp + "  ·  긴장 " + a.stress, 16, Ui.Muted);
                if (Appearance.HasFemaleVariant(id))
                    Ui.Button(stack,a.femaleAppearance ? "외형: 여성형  →  남성형으로 변경" : "외형: 남성형  →  여성형으로 변경",()=>Change(()=>game.SetFemaleAppearance(id,!a.femaleAppearance)),44);
                var row = Ui.Node("Formation buttons", stack); Ui.Height(row, 48);
                var left = Ui.Button(row, "전열로 이동", () => Change(() => game.Reorder(id, -1)), 48, game.Rank(id) > 1 && game.State.mode != "combat" && game.State.mode != "ended");
                var l = (RectTransform)left.transform; l.anchorMin = Vector2.zero; l.anchorMax = new Vector2(.49f, 1); l.offsetMin = l.offsetMax = Vector2.zero;
                var right = Ui.Button(row, "후열로 이동", () => Change(() => game.Reorder(id, 1)), 48, game.Rank(id) < game.Party.Count && game.State.mode != "combat" && game.State.mode != "ended");
                var r = (RectTransform)right.transform; r.anchorMin = new Vector2(.51f, 0); r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            }
            if (game.State.fallen.Count > 0) Ui.Text(stack, "길 위에 남겨진 이름\n" + string.Join(" · ", game.State.fallen.Select(id => data.companions.First(a => a.id == id).name)), 18, Ui.Muted);
        }
        void JournalScreen()
        {
            var stack = Chapter("CHRONICLE   /   선택은 사라지지 않는다", "연대기");
            Ui.Text(stack, game.State.events + "개의 선택  ·  " + game.State.victories + "번의 승리  ·  " + game.AgeLabel, 17, Ui.Gold);
            foreach (var j in game.State.journal)
            {
                Ui.Text(stack, (16 + j.week / 52) + "세 " + j.week % 52 + "주   /   " + j.title, 20, Ui.Gold);
                Ui.Text(stack, j.body, 18);
            }
            Ui.Text(stack, "최근 350개의 기록이 보관됩니다.", 12, Ui.Muted);
        }
        void EndingScreen()
        {
            var e = game.State.ending; var stack = Chapter("THE LAST PAGE   /   " + game.AgeLabel, e.title, "camp");
            Ui.Text(stack, Ui.Safe(game.State.hero.name) + "\n" + e.legacy, 28, Ui.Gold); Ui.Text(stack, e.body, 22);
            Ui.Text(stack, game.State.events + "개의 선택   ·   " + game.State.victories + "번의 승리   ·   평판 " + game.State.rep, 17, Ui.Muted);
            Ui.Button(stack, "살아온 날들을 돌아본다", () => Navigate("journal"), 54);
            Ui.Button(stack, "다른 이름으로 다시 시작한다", NewLifeDialog, 58, true, true);
        }
    }
}
