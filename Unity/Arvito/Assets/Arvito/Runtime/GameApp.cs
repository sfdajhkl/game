using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arvito.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Arvito.Runtime
{
    public sealed partial class GameApp : MonoBehaviour
    {
        Game game;
        Catalog data;
        SaveStore saves;
        RectTransform canvas, page, main, aside, dialog, toast;
        TMP_InputField nameInput;
        string tab = "journey", selectedSkill, selectedTarget, lastActor, notice = "";
        bool title = true, busy;
        float toastUntil;
        int screenWidth, screenHeight;
        bool Compact => Screen.width < 1000 || (float)Screen.width / Mathf.Max(1, Screen.height) < 1.25f;
        readonly List<Action> shortcuts = new List<Action>();
        readonly Dictionary<string, Texture2D> art = new Dictionary<string, Texture2D>();
        Action enterAction;
        InputSystemUIInputModule uiInput;
        InputActionReference uiSubmit;
        float UiScale => PlayerPrefs.GetFloat("textScale", 1);

        void Awake()
        {
            Application.targetFrameRate = 60;
            data = JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("Arvito/catalog").text);
            data.Normalize();
            Ui.Font = Resources.Load<TMP_FontAsset>("Arvito/Fonts/Arvito-Korean");
            if (Ui.Font == null)
            {
                Ui.Font = TMP_FontAsset.CreateFontAsset(Resources.Load<Font>("Arvito/Fonts/NotoSerifKR-Regular"));
                Ui.Font.isMultiAtlasTexturesEnabled = true;
            }
            Ui.NarrativeFont = Resources.Load<TMP_FontAsset>("Arvito/Fonts/Arvito-YiSunShin");
            if (Ui.NarrativeFont == null)
            {
                var source = Resources.Load<Font>("Arvito/Fonts/YiSunShin-Regular");
                if (source != null)
                {
                    Ui.NarrativeFont = TMP_FontAsset.CreateFontAsset(source, 48, 5,
                        UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                    Ui.NarrativeFont.fallbackFontAssetTable = new List<TMP_FontAsset> { Ui.Font };
                }
                else Debug.LogWarning("YiSunShin font is missing. Run Arvito > Prepare Project.");
            }
            Ui.TextScale = UiScale;
            foreach (string id in new[] { "village", "forest", "ruins", "camp", "battle" }) art[id] = Resources.Load<Texture2D>("Arvito/Art/" + id);
            saves = new SaveStore(Path.Combine(Application.persistentDataPath, "arvito-save.json"), s => JsonUtility.ToJson(s, true), json => JsonUtility.FromJson<GameState>(json), s => Game.Valid(s, data));
            var state = saves.Load(out notice); if (state != null) game = new Game(data, state);
            canvas = Ui.Node("Arvito Canvas", transform); var c = canvas.gameObject.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1440, 900); scaler.matchWidthOrHeight = .5f;
            canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Ui.Paint(canvas, Ui.Ink);
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem)); var input = events.AddComponent<InputSystemUIInputModule>(); input.AssignDefaultActions();

            }
            uiInput = FindFirstObjectByType<InputSystemUIInputModule>();
            if (uiInput != null) uiSubmit = uiInput.submit;
            Render();
        }
        void Update()
        {
            if (toast != null && Time.unscaledTime > toastUntil) { Destroy(toast.gameObject); toast = null; }
            if (dialog == null && (screenWidth != Screen.width || screenHeight != Screen.height))
            {
                screenWidth = Screen.width; screenHeight = Screen.height;
                Render();
            }
            var k = Keyboard.current; if (k == null || busy) return;
            if (k.f11Key.wasPressedThisFrame) Screen.fullScreenMode = Screen.fullScreen ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
            if (dialog != null) { if (k.escapeKey.wasPressedThisFrame) CloseDialog(); return; }
            if (nameInput != null && nameInput.isFocused) return;
            if (k.escapeKey.wasPressedThisFrame) { Settings(); return; }
            if (title) return;
            if (k.eKey.wasPressedThisFrame) Navigate("journey");
            if (k.iKey.wasPressedThisFrame) Navigate("inventory");
            if (k.jKey.wasPressedThisFrame) Navigate("journal");
            if (k.cKey.wasPressedThisFrame) Navigate("party");
            if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) { enterAction?.Invoke(); return; }
            var digits = new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
            for (int i = 0; i < digits.Length && i < shortcuts.Count; i++) if (k[digits[i]].wasPressedThisFrame) { shortcuts[i]?.Invoke(); break; }
        }
        void OnApplicationPause(bool paused) { if (paused && !title) Save(); }
        void OnApplicationQuit() { if (!title) Save(); PlayerPrefs.Save(); }
        void Save()
        {
            if (game == null) return;
            try { saves.Save(game.State); notice = "자동 저장됨"; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { notice = "저장 실패 · 저장 공간을 확인해 주세요"; Debug.LogException(e); Notify(notice); }
        }
        void Change(Action action)
        {
            if (busy) return; busy = true;
            try { selectedChoice = -1; action(); Save(); if (game.State.mode == "ended") tab = "journey"; Render(); }
            finally { busy = false; }
        }
        void Navigate(string destination) { tab = destination; Render(); }
        void ClearPage()
        {
            if (page != null) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            page = Ui.Card(canvas, "Page", Ui.Paper); Ui.Fill(page, 20, 20, 20, 20); Ui.Border(page);
            Canvas.ForceUpdateCanvases(); shortcuts.Clear(); enterAction = null; nameInput = null;
            EventSystem.current?.SetSelectedGameObject(null);
        }
        void Render()
        {
            ClearPage();
            if (title) { TitleScreen(); FinishBook(); return; }
            Shell();
            Canvas.ForceUpdateCanvases();
            if (tab == "inventory") InventoryScreen();
            else if (tab == "journal") JournalScreen();
            else if (tab == "party") PartyScreen();
            else if (game.State.mode == "combat") CombatScreen();
            else if (game.State.mode == "ended") EndingScreen();
            else if (game.State.mode == "outcome") OutcomeScreen();
            else StoryScreen();
            if (aside != null) Sidebar();
            FinishBook();
        }
        void SyncSubmit()
        {
            // Preserve native keyboard submit in menus/dialogs; story/combat Enter has one owner.
            if (uiInput != null) uiInput.submit = dialog != null || enterAction == null ? uiSubmit : null;
        }
        void FinishBook()
        {
            SyncSubmit();
            var edges = Ui.Node("Worn parchment edges", page); Ui.Fill(edges);
            edges.gameObject.AddComponent<ParchmentWear>().raycastTarget = false;
        }
        void Shell()
        {
            aside = null;
            var header = Ui.Card(page, "Book header"); header.anchorMin = new Vector2(0, 1); header.anchorMax = Vector2.one; header.pivot = new Vector2(.5f, 1); header.sizeDelta = new Vector2(0, 86);
            var brand = Ui.Narrative(header, "아 르 비 토", 30, null, TextAlignmentOptions.MidlineLeft);
            Ui.Box(brand.rectTransform, 28, 12, 210, 40);
            Ui.FixedText(header, "A  R  V  I  T  O", 32, 53, 210, 20, 12, Ui.Gold);
            if (!Compact) Ui.FixedText(header, game.AgeLabel + "   ·   " + Season() + "   /   " + game.RegionName, 280, 22, 590, 40, 19, Ui.Muted);
            var settings = Ui.Button(header, "설정", Settings, 42);
            var sr = (RectTransform)settings.transform; sr.anchorMin = sr.anchorMax = Vector2.one; sr.pivot = Vector2.one; sr.anchoredPosition = new Vector2(-24, -22); sr.sizeDelta = new Vector2(82, 42);
            var rule = Ui.Card(header, "Header rule", Ui.Line); rule.anchorMin = Vector2.zero; rule.anchorMax = new Vector2(1, 0); rule.sizeDelta = new Vector2(0, 1);
            if (game.State.mode == "combat" && tab == "journey")
            {
                main = Ui.Node("Battle main", page); Ui.Fill(main, 24, 24, 104, 32); return;
            }
            float left = Compact ? 20 : 198;
            if (Compact)
            {
                var nav = Ui.Node("Book tabs", page); Ui.Box(nav, 20, 96, page.rect.width - 40, 46);
                string[] names = { "여정", "소지품", "동행", "기록" }, tabs = { "journey", "inventory", "party", "journal" };
                for (int i = 0; i < tabs.Length; i++)
                {
                    string destination = tabs[i]; var button = Ui.Button(nav, names[i], () => Navigate(destination), 44, true, tab == destination);
                    var rt = (RectTransform)button.transform; rt.anchorMin = new Vector2(i / 4f, 0); rt.anchorMax = new Vector2((i + 1) / 4f, 1); rt.offsetMin = new Vector2(2, 0); rt.offsetMax = new Vector2(-2, 0);
                }
            }
            else
            {
                var rail = Ui.Card(page, "Book navigation", Ui.Rail); Ui.Fill(rail, 0, 0, 87, 0); rail.anchorMax = new Vector2(0, 1); rail.offsetMax = new Vector2(166, -87);
                Ui.FixedText(rail, "여 행 수 첩", 24, 28, 130, 30, 14, Ui.Muted);
                var nav = Ui.Stack(rail, "Navigation items", 0, 12); Ui.Fill(nav, 18, 18, 76, 300);
                Nav(nav, "여정", "journey", "E"); Nav(nav, "소지품", "inventory", "I"); Nav(nav, "동행", "party", "C"); Nav(nav, "기록", "journal", "J");
                var place = Ui.Text(rail, "현재 장\n\n" + game.RegionName + "\n" + game.StageName, 18, Ui.Muted);
                Ui.Box(place.rectTransform, 24, 358, 120, 160);
                var travel = Ui.Button(rail, "다른 지역으로", TravelDialog, 46, game.State.mode == "story");
                var tr = (RectTransform)travel.transform; tr.anchorMin = Vector2.zero; tr.anchorMax = new Vector2(1, 0); tr.offsetMin = new Vector2(18, 100); tr.offsetMax = new Vector2(-18, 146);
                var camp = Ui.Button(rail, "불가에서 쉬기", CampDialog, 46, game.State.mode == "story");
                var cr = (RectTransform)camp.transform; cr.anchorMin = Vector2.zero; cr.anchorMax = new Vector2(1, 0); cr.offsetMin = new Vector2(18, 42); cr.offsetMax = new Vector2(-18, 88);
            }
            float top = Compact ? 158 : 104;
            var resources = Ui.Node("Travel resources", page); Ui.Box(resources, left, top, page.rect.width - left - 28, 56);
            var hero = game.State.hero;
            string[] values = { "체력  " + hero.hp + " / " + hero.maxHp, "긴장  " + hero.stress + " / 100", "식량  " + game.State.rations, "소지금  " + game.State.gold + " G" };
            for (int i = 0; i < values.Length; i++)
            {
                var label = Ui.Text(resources, values[i], Compact ? 16 : 19, Ui.Muted, TextAlignmentOptions.MidlineLeft);
                label.rectTransform.anchorMin = new Vector2(i / 4f, 0); label.rectTransform.anchorMax = new Vector2((i + 1) / 4f, 1); label.rectTransform.offsetMin = Vector2.zero; label.rectTransform.offsetMax = Vector2.zero;
            }
            var divider = Ui.Card(resources, "Resource rule", Ui.Line); divider.anchorMin = Vector2.zero; divider.anchorMax = new Vector2(1, 0); divider.sizeDelta = new Vector2(0, 1);
            main = Ui.Node("Book content", page); Ui.Fill(main, left, 28, top + 78, 52);
            var footer = Ui.Text(page, Compact ? game.AgeLabel + " · " + Season() + "     /     " + notice : "E 여정   I 소지품   C 동행   J 기록   1–9 선택   Enter 확정     /     " + notice, 12, Ui.Muted);
            footer.rectTransform.anchorMin = Vector2.zero; footer.rectTransform.anchorMax = new Vector2(1, 0); footer.rectTransform.offsetMin = new Vector2(left, 14); footer.rectTransform.offsetMax = new Vector2(-24, 34);
        }
        void Nav(Transform parent, string label, string destination, string key) => Ui.Button(parent, label + "  <size=11>" + key + "</size>", () => Navigate(destination), 52, true, tab == destination);
        string Season() => new[] { "봄", "여름", "가을", "겨울" }[(game.State.weeks % 52) / 13];
        void Sidebar()
        {
            var stack = Ui.Scroll(aside, "Party scroll"); stack.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().padding = new RectOffset(17, 17, 18, 18);
            Ui.Text(stack, "함께 걷는 사람들", 18, Ui.Gold); Ui.Text(stack, "전열  →  후열    /    " + game.Party.Count + "명", 12, Ui.Muted);
            foreach (var a in game.Party)
            {
                var card = Ui.Card(stack, a.id, Ui.Raised); Ui.Height(card, 164); Ui.Border(card, game.State.combat?.active == a.id ? Ui.Gold : Ui.Line);
                Ui.FixedText(card, game.Rank(a.id).ToString("00") + "   " + Ui.Safe(a.name), 12, 10, 210, 25, 18, a.id == "hero" ? Ui.Gold : Ui.Ink);
                var portrait=Ui.Node("Party portrait",card);Ui.Box(portrait,8,36,88,112);BattlerArt.Portrait(portrait,Appearance.Sprite(a));
                Ui.FixedText(card, data.Weapon(a.weapon).name, 104, 40, 128, 35, 12, Ui.Muted);
                Ui.FixedText(card, "체력 " + a.hp + " / " + a.maxHp, 104, 80, 128, 20, 12); Ui.Bar(card, 104, 104, 120, (float)a.hp / a.maxHp, Ui.Green);
                Ui.FixedText(card, "긴장 " + a.stress + "\n기력 " + a.energy + " / 5" + (a.status.bleed > 0 ? "   출혈" : ""), 104, 116, 128, 36, 11, a.stress >= 70 ? Ui.Red : Ui.Muted);
            }
            if (game.Party.Count == 1) Ui.Text(stack, "어떤 만남은\n선택 하나에서 시작됩니다.", 13, Ui.Muted);
            Ui.Text(stack, "평판  " + game.State.rep + "     유대  " + game.State.bond, 13, Ui.Gold);
            Ui.Text(stack, "힘 " + game.State.hero.stats.strength + "  ·  민첩 " + game.State.hero.stats.agility + "\n지혜 " + game.State.hero.stats.wit + "  ·  의지 " + game.State.hero.stats.will, 13, Ui.Muted);
        }
        void Notify(string message)
        {
            if (toast != null) Destroy(toast.gameObject);
            toast = Ui.Card(canvas, "Notice", Ui.Raised); toast.anchorMin = toast.anchorMax = new Vector2(.5f, 0); toast.pivot = new Vector2(.5f, 0); toast.anchoredPosition = new Vector2(0, 48); toast.sizeDelta = new Vector2(600, 60); Ui.Border(toast, Ui.Gold);
            var label = Ui.Text(toast, message, 16, Ui.Ink, TextAlignmentOptions.Center); Ui.Fill(label.rectTransform, 16, 16, 8, 8); toastUntil = Time.unscaledTime + 4;
        }
        RectTransform Dialog(string heading, string body)
        {
            CloseDialog(); dialog = Ui.Card(canvas, "Modal backdrop", new Color(0, 0, 0, .82f)); Ui.Fill(dialog);
            var box = Ui.Card(dialog, "Dialog"); box.anchorMin = box.anchorMax = new Vector2(.5f, .5f); box.pivot = new Vector2(.5f, .5f); box.sizeDelta = new Vector2(Mathf.Min(650, canvas.rect.width - 48), Mathf.Min(680, Mathf.Max(500, canvas.rect.height - 80))); Ui.Border(box, Ui.Gold);
            var stack = Ui.Scroll(box, "Dialog content"); var layout = stack.GetComponent<UnityEngine.UI.VerticalLayoutGroup>(); layout.padding = new RectOffset(32, 24, 32, 32); layout.spacing = 20;
            Ui.Narrative(stack, heading, 29, Ui.Gold); Ui.Text(stack, body, 18); SyncSubmit(); return stack;
        }
        void CloseDialog() { if (dialog != null) { dialog.gameObject.SetActive(false); Destroy(dialog.gameObject); dialog = null; } SyncSubmit(); }
        void CampDialog()
        {
            var stack = Dialog("불가에서 쉬기", "1주가 흐릅니다. 식량 1개를 사용하고, 식량이 없으면 8골드를 냅니다.\n\n일행의 체력이 회복되고 긴장이 20 줄어듭니다.");
            Ui.Art(stack, art["camp"], 150);
            Ui.Button(stack, "1주 동안 야영한다", () => { CloseDialog(); string message = ""; Change(() => message = game.Camp()); Notify(message); }, 52, true, true);
            Ui.Button(stack, "길을 계속 걷는다", CloseDialog);
        }
        void AssetStatus()
        {
            int missing = 0;
            foreach (string id in art.Keys) if (art[id] == null) missing++;
            bool characters = Resources.Load<Texture2D>("Arvito/Art/characters") != null;
            bool eventArt = game == null || EventArt.Load(game.Event.id) != null;
            bool font = Resources.Load<TMP_FontAsset>("Arvito/Fonts/Arvito-Korean") != null;
            bool narrativeFont = Resources.Load<TMP_FontAsset>("Arvito/Fonts/Arvito-YiSunShin") != null;
            var stack = Dialog("게임 자료 확인", "양피지 UI 0.3.1\n\n배경: " + (missing == 0 ? "정상" : missing + "개 누락") +
                "\n캐릭터: " + (characters ? "정상" : "누락") + "\n현재 삽화: " + (game == null ? "시작 전" : eventArt ? "정상" : "누락") + "\n정보 글꼴: " + (font ? "정상" : "준비 필요") + "\n이순신체: " + (narrativeFont ? "정상" : "준비 필요"));
            Ui.Text(stack, "글꼴 준비가 필요하면 재생을 멈추고 Arvito → Prepare Project를 실행해 주세요.\n그림 자료가 누락되면 Unity 재생을 멈추고 상단 Arvito → Repair Art Imports를 실행해 주세요.\n\n프로젝트 위치\n" + Application.dataPath, 16, Ui.Muted);
            Ui.Button(stack, "닫기", CloseDialog);
        }
        void Settings()
        {
            var stack = Dialog("잠시 책갈피를 끼우다", "행동을 마칠 때마다 자동 저장됩니다.\nEsc 닫기  ·  F11 전체 화면");
            Ui.Button(stack, Screen.fullScreen ? "창 모드로 전환" : "전체 화면으로 전환", () => { Screen.fullScreenMode = Screen.fullScreen ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow; CloseDialog(); });
            Ui.Button(stack, Ui.TextScale > 1 ? "글자 크기: 크게 → 기본" : "글자 크기: 기본 → 크게", () => { Ui.TextScale = Ui.TextScale > 1 ? 1 : 1.12f; PlayerPrefs.SetFloat("textScale", Ui.TextScale); PlayerPrefs.Save(); CloseDialog(); Render(); });
            if (!title) Ui.Button(stack, "저장하고 시작 화면으로", () => { Save(); CloseDialog(); title = true; Render(); });
            Ui.Button(stack, "게임 종료", () => { Save(); Application.Quit(); });
            Ui.Button(stack, "그림·글꼴 자료 확인", AssetStatus, 48);
            Ui.Text(stack, "아르비토 0.3.1 · 양피지 수첩\n제목·본문: 이순신체 · 아산시\n정보·숫자: Noto Serif KR · SIL OFL 1.1", 14, Ui.Muted);
            Ui.Button(stack, "계속하기", CloseDialog, 50, true, true);
        }
    }
}
