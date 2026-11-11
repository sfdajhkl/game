using System;
using TMPro;
using UnityEngine;

namespace Arvito.Runtime
{
    /// <summary>Small native uGUI component factory. All dimensions are CanvasScaler reference pixels.</summary>
    public static class Ui
    {
        public static TMP_FontAsset Font;
        public static TMP_FontAsset NarrativeFont;
        static Sprite paperSurface;
        public static float TextScale = 1;
        public static readonly Color Ink = Hex("3B2A20"), Paper = Hex("D5BE96"), Panel = Paper,
            Raised = Hex("E1CDAB"), Line = Hex("997951"), Gold = Hex("79573A"), Muted = Hex("6B4D34"),
            Red = Hex("8A3E30"), Green = Hex("53613F"), Rail = Hex("C5AA80");
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        public static RectTransform Node(string name, Transform parent)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(parent, false); return rt;
        }
        public static void Fill(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
        { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t); }
        public static void Box(RectTransform rt, float x, float y, float w, float h)
        { rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h); }
        public static UnityEngine.UI.Image Paint(RectTransform rt, Color color)
        {
            var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color;
            if (color == Paper || color == Raised || color == Rail)
            {
                if (paperSurface == null)
                {
                    var texture = Resources.Load<Texture2D>("Arvito/UI/paper-grain");
                    if (texture != null) paperSurface = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
                }
                image.sprite = paperSurface;
                image.type = UnityEngine.UI.Image.Type.Tiled;
            }
            return image;
        }
        public static RectTransform Card(Transform parent, string name, Color? color = null)
        { var rt = Node(name, parent); Paint(rt, color ?? Panel); return rt; }
        public static void Border(RectTransform rt, Color? color = null)
        { var o = rt.gameObject.AddComponent<UnityEngine.UI.Outline>(); o.effectColor = color ?? Line; o.effectDistance = new Vector2(1, -1); o.useGraphicAlpha = false; }
        public static void Height(RectTransform rt, float h, float flex = 0)
        { var le = rt.gameObject.GetComponent<UnityEngine.UI.LayoutElement>() ?? rt.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); le.minHeight = le.preferredHeight = h; le.flexibleHeight = flex; }
        public static RectTransform Stack(Transform parent, string name, int padding = 0, float spacing = 12)
        {
            var rt = Node(name, parent); var group = rt.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            group.padding = new RectOffset(padding, padding, padding, padding); group.spacing = spacing; group.childControlHeight = true; group.childControlWidth = true; group.childForceExpandHeight = false; group.childForceExpandWidth = true;
            return rt;
        }
        public static TextMeshProUGUI Text(Transform parent, string value, float size = 20, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var rt = Node("Text", parent); var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font; t.text = value; t.fontSize = size * TextScale; t.color = color ?? Ink; t.alignment = align; t.richText = true; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow; t.lineSpacing = 9;
            return t;
        }
        public static TextMeshProUGUI Narrative(Transform parent, string value, float size = 20, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var text = Text(parent, value, size, color, align);
            text.font = NarrativeFont != null ? NarrativeFont : Font;
            return text;
        }
        public static TextMeshProUGUI FixedText(Transform parent, string value, float x, float y, float w, float h, float size = 20, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        { var t = Text(parent, value, size, color, align); Box(t.rectTransform, x, y, w, h); return t; }
        public static RectTransform Scroll(Transform parent, string name)
        {
            var root = Node(name, parent); Fill(root);
            var scroll = root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>(); scroll.horizontal = false; scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 36;
            var viewport = Card(root, "Viewport", new Color(0, 0, 0, .002f)); Fill(viewport, 0, 8); viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            var content = Stack(viewport, "Content", 2, 18); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
            var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>(); fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            return content;
        }
        public static UnityEngine.UI.Button Button(Transform parent, string label, Action click, float height = 50, bool enabled = true, bool primary = false)
        {
            var rt = Card(parent, "Button " + label, primary ? Ink : Raised); Height(rt, height); Border(rt, primary ? Ink : Line);
            var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = rt.GetComponent<UnityEngine.UI.Image>(); b.interactable = enabled;
            var colors = b.colors; colors.normalColor = Color.white; colors.highlightedColor = Hex("F5E9D4"); colors.pressedColor = Hex("CBB38D"); colors.selectedColor = Color.white; colors.disabledColor = Hex("B6A78D"); colors.fadeDuration = .1f; b.colors = colors;
            var text = Text(rt, label, 18, primary ? Paper : Ink, TextAlignmentOptions.MidlineLeft); Fill(text.rectTransform, 16, 16, 5, 5);
            b.onClick.AddListener(() => { if (enabled) click?.Invoke(); }); return b;
        }
        public static void Art(Transform parent, Texture texture, float height)
        {
            var rt = Node("Pixel illustration", parent); Height(rt, height); var raw = rt.gameObject.AddComponent<UnityEngine.UI.RawImage>(); raw.texture = texture; raw.raycastTarget = false; raw.enabled = texture != null;
            rt.gameObject.AddComponent<ArtCrop>(); Border(rt);
            if (texture == null) MissingArt(rt);
        }
        public static void EventIllustration(Transform parent, Texture texture, float height = 400)
        {
            var frame = Card(parent, "Event illustration frame", Paper); Height(frame, height); Border(frame);
            var picture = Node("Event illustration", frame); Fill(picture, 2, 2, 2, 2);
            var raw = picture.gameObject.AddComponent<PixelArtImage>();
            raw.texture = texture; raw.raycastTarget = false;
        }
        public static void Bar(Transform parent, float x, float y, float width, float ratio, Color color)
        {
            var track = Card(parent, "Meter", Line); Box(track, x, y, width, 5);
            var fill = Card(track, "Value", color); fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1); fill.offsetMin = fill.offsetMax = Vector2.zero;
        }
        public static void MissingArt(Transform parent)
        {
            var label = Text(parent, "삽화를 불러오지 못했습니다.\n설정의 자료 확인에서 상태를 확인해 주세요.", 16, Muted, TextAlignmentOptions.Center);
            Fill(label.rectTransform, 16, 16, 16, 16);
        }
        public static void Rule(Transform parent)
        {
            var line = Card(parent, "Book rule", Line); Height(line, 1);
            line.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        }
        public static string Safe(string text) => (text ?? "").Replace("<", "‹").Replace(">", "›");
    }
}
