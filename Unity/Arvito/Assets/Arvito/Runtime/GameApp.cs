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
        readonly List<Action> shortcuts = new List<Action>();
        readonly Dictionary<string, Texture2D> art = new Dictionary<string, Texture2D>();
        Action enterAction;
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
            Ui.TextScale = UiScale;
            foreach (string id in new[] { "village", "forest", "ruins", "camp", "battle" }) art[id] = Resources.Load<Texture2D>("Arvito/Art/" + id);
            saves = new SaveStore(Path.Combine(Application.persistentDataPath, "arvito-save.json"), s => JsonUtility.ToJson(s, true), json => JsonUtility.FromJson<GameState>(json), s => Game.Valid(s, data));
            var state = saves.Load(out notice); if (state != null) game = new Game(data, state);
            canvas = Ui.Node("Arvito Canvas", transform); var c = canvas.gameObject.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1440, 900); scaler.matchWidthOrHeight = .5f;
            canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem)); var input = events.AddComponent<InputSystemUIInputModule>(); input.AssignDefaultActions();
            }
            Render();
        }
        void Update()
        {
            if (toast != null && Time.unscaledTime > toastUntil) { Destroy(toast.gameObject); toast = null; }
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
            try { action(); Save(); if (game.State.mode == "ended") tab = "journey"; Render(); }
            finally { busy = false; }
        }
        void Navigate(string destination) { tab = destination; Render(); }
        void ClearPage()
        {
            if (page != null) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            page = Ui.Card(canvas, "Page", Ui.Ink); Ui.Fill(page); shortcuts.Clear(); enterAction = null; nameInput = null;
            EventSystem.current?.SetSelectedGameObject(null);
        }
        void Render()
        {
            ClearPage();
            if (title) { TitleScreen(); return; }
            Shell();
            if (tab == "inventory") InventoryScreen();
            else if (tab == "journal") JournalScreen();
            else if (tab == "party") PartyScreen();
            else if (game.State.mode == "combat") CombatScreen();
            else if (game.State.mode == "ended") EndingScreen();
            else if (game.State.mode == "outcome") OutcomeScreen();
            else StoryScreen();
            if (aside != null) Sidebar();
        }
        void Shell()
        {
            var header = Ui.Card(page, "Header"); header.anchorMin = new Vector2(0, 1); header.anchorMax = Vector2.one; header.pivot = new Vector2(.5f, 1); header.sizeDelta = new Vector2(0, 78);
            Ui.FixedText(header, "A R V I T O", 28, 13, 205, 30, 26, Ui.Gold);
            Ui.FixedText(header, "아르비토  ·  한 사람의 일대기", 30, 43, 220, 22, 11, Ui.Muted);
            Ui.FixedText(header, Ui.Safe(game.State.hero.name) + "의 여정", 252, 14, 285, 26, 21);
            Ui.FixedText(header, game.AgeLabel + "   /   " + game.RegionName + "   /   성장 " + game.State.hero.level, 252, 44, 490, 20, 13, Ui.Muted);
            var controls = Ui.Node("Header controls", header); controls.anchorMin = controls.anchorMax = new Vector2(1, 1); controls.pivot = new Vector2(1, 1); controls.anchoredPosition = new Vector2(-24, -20); controls.sizeDelta = new Vector2(340, 40);
            Ui.FixedText(controls, "G  " + game.State.gold + "     식량 " + game.State.rations + "     약 " + game.State.potions, 0, 0, 255, 40, 15, Ui.Gold);
            var settings = Ui.Button(controls, "설정", Settings, 40); Ui.Box((RectTransform)settings.transform, 264, 0, 76, 40);
            if (game.State.mode == "combat" && tab == "journey")
            {
                main = Ui.Node("Battle main", page); Ui.Fill(main, 24, 24, 96, 30); aside = null; return;
            }
            var rail = Ui.Card(page, "Navigation"); Ui.Fill(rail, 0, 0, 79, 40); rail.anchorMax = new Vector2(0, 1); rail.offsetMax = new Vector2(204, -79);
            Ui.FixedText(rail, "TRAVELER'S BOOK", 24, 26, 170, 26, 11, Ui.Gold);
            var nav = Ui.Stack(rail, "Navigation items", 0, 12); Ui.Fill(nav, 20, 20, 75, 180);
            Nav(nav, "01   여정", "journey", "E"); Nav(nav, "02   일행", "party", "C"); Nav(nav, "03   소지품", "inventory", "I"); Nav(nav, "04   연대기", "journal", "J");
            var travel = Ui.Button(rail, "다른 지역으로", TravelDialog, 46, game.State.mode == "story");
            var tr = (RectTransform)travel.transform; tr.anchorMin = new Vector2(0, 0); tr.anchorMax = new Vector2(1, 0); tr.offsetMin = new Vector2(20, 138); tr.offsetMax = new Vector2(-20, 184);
            var camp = Ui.Button(rail, "불가에서 쉬기", CampDialog, 50, game.State.mode == "story"); var campRt = (RectTransform)camp.transform; campRt.anchorMin = new Vector2(0, 0); campRt.anchorMax = new Vector2(1, 0); campRt.offsetMin = new Vector2(20, 76); campRt.offsetMax = new Vector2(-20, 126);
            var note = Ui.Text(rail, "한 주의 쉼도\n당신의 삶에 남습니다.", 12, Ui.Muted); Ui.Fill(note.rectTransform, 25, 15); note.rectTransform.anchorMax = new Vector2(1, 0); note.rectTransform.offsetMin = new Vector2(25, 15); note.rectTransform.offsetMax = new Vector2(-15, 62);
            main = Ui.Node("Main", page); Ui.Fill(main, 228, 316, 100, 60);
            aside = Ui.Card(page, "Party sidebar"); Ui.Fill(aside, 0, 24, 100, 60); aside.anchorMin = new Vector2(1, 0); aside.offsetMin = new Vector2(-292, 60);
            var footer = Ui.Text(page, "E 여정     C 일행     I 소지품     J 연대기     1–9 선택     F11 전체 화면", 11, Ui.Muted); footer.rectTransform.anchorMin = Vector2.zero; footer.rectTransform.anchorMax = new Vector2(1, 0); footer.rectTransform.offsetMin = new Vector2(28, 9); footer.rectTransform.offsetMax = new Vector2(-28, 29);
            var saved = Ui.Text(page, notice, 11, Ui.Muted, TextAlignmentOptions.TopRight); saved.rectTransform.anchorMin = Vector2.zero; saved.rectTransform.anchorMax = new Vector2(1, 0); saved.rectTransform.offsetMin = new Vector2(850, 9); saved.rectTransform.offsetMax = new Vector2(-28, 29);
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
                Ui.FixedText(card, game.Rank(a.id).ToString("00") + "   " + Ui.Safe(a.name), 12, 10, 210, 25, 18, a.id == "hero" ? Ui.Gold : Ui.Paper);
                var portrait=Ui.Node("Party portrait",card);Ui.Box(portrait,8,36,88,112);BattlerArt.Portrait(portrait,a.id);
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
            var label = Ui.Text(toast, message, 16, Ui.Paper, TextAlignmentOptions.Center); Ui.Fill(label.rectTransform, 16, 16, 8, 8); toastUntil = Time.unscaledTime + 4;
        }
        RectTransform Dialog(string heading, string body)
        {
            CloseDialog(); dialog = Ui.Card(canvas, "Modal backdrop", new Color(0, 0, 0, .82f)); Ui.Fill(dialog);
            var box = Ui.Card(dialog, "Dialog"); box.anchorMin = box.anchorMax = new Vector2(.5f, .5f); box.pivot = new Vector2(.5f, .5f); box.sizeDelta = new Vector2(650, Mathf.Min(680, Mathf.Max(500, canvas.rect.height - 80))); Ui.Border(box, Ui.Gold);
            var stack = Ui.Scroll(box, "Dialog content"); var layout = stack.GetComponent<UnityEngine.UI.VerticalLayoutGroup>(); layout.padding = new RectOffset(32, 24, 32, 32); layout.spacing = 20;
            Ui.Text(stack, heading, 29, Ui.Gold); Ui.Text(stack, body, 18); return stack;
        }
        void CloseDialog() { if (dialog != null) { dialog.gameObject.SetActive(false); Destroy(dialog.gameObject); dialog = null; } }
        void CampDialog()
        {
            var stack = Dialog("불가에서 쉬기", "1주가 흐릅니다. 식량 1개를 사용하고, 식량이 없으면 8골드를 냅니다.\n\n일행의 체력이 회복되고 긴장이 20 줄어듭니다.");
            Ui.Art(stack, art["camp"], 150);
            Ui.Button(stack, "1주 동안 야영한다", () => { CloseDialog(); string message = ""; Change(() => message = game.Camp()); Notify(message); }, 52, true, true);
            Ui.Button(stack, "길을 계속 걷는다", CloseDialog);
        }
        void Settings()
        {
            var stack = Dialog("잠시 책갈피를 끼우다", "행동을 마칠 때마다 자동 저장됩니다.\nEsc 닫기  ·  F11 전체 화면");
            Ui.Button(stack, Screen.fullScreen ? "창 모드로 전환" : "전체 화면으로 전환", () => { Screen.fullScreenMode = Screen.fullScreen ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow; CloseDialog(); });
            Ui.Button(stack, Ui.TextScale > 1 ? "글자 크기: 크게 → 기본" : "글자 크기: 기본 → 크게", () => { Ui.TextScale = Ui.TextScale > 1 ? 1 : 1.12f; PlayerPrefs.SetFloat("textScale", Ui.TextScale); PlayerPrefs.Save(); CloseDialog(); Render(); });
            if (!title) Ui.Button(stack, "저장하고 시작 화면으로", () => { Save(); CloseDialog(); title = true; Render(); });
            Ui.Button(stack, "게임 종료", () => { Save(); Application.Quit(); });
            Ui.Button(stack, "계속하기", CloseDialog, 50, true, true);
        }
    }
}
