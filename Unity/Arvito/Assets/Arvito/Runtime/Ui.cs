using System;
using TMPro;
using UnityEngine;

namespace Arvito.Runtime
{
    /// <summary>Small native uGUI component factory. All dimensions are CanvasScaler reference pixels.</summary>
    public static class Ui
    {
        public static TMP_FontAsset Font;
        public static float TextScale = 1;
        public static readonly Color Ink = Hex("111917"), Panel = Hex("1B2521"), Raised = Hex("25312B"), Line = Hex("3D493C"), Gold = Hex("D0AF71"), Paper = Hex("EBE2CB"), Muted = Hex("A4AE99"), Red = Hex("CA7969"), Green = Hex("91B08B");
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
        { var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; return image; }
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
            t.font = Font; t.text = value; t.fontSize = size * TextScale; t.color = color ?? Paper; t.alignment = align; t.richText = true; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow; t.lineSpacing = 9;
            return t;
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
            var rt = Card(parent, "Button " + label, primary ? Gold : Raised); Height(rt, height); Border(rt, primary ? Gold : Line);
            var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = rt.GetComponent<UnityEngine.UI.Image>(); b.interactable = enabled;
            var colors = b.colors; colors.normalColor = Color.white; colors.highlightedColor = Hex("DDE8C9"); colors.pressedColor = Hex("ABBC97"); colors.selectedColor = Color.white; colors.disabledColor = Hex("687268"); colors.fadeDuration = .1f; b.colors = colors;
            var text = Text(rt, label, 18, primary ? Ink : Paper, TextAlignmentOptions.MidlineLeft); Fill(text.rectTransform, 16, 16, 5, 5);
            b.onClick.AddListener(() => { if (enabled) click?.Invoke(); }); return b;
        }
        public static void Art(Transform parent, Texture texture, float height)
        {
            var rt = Node("Pixel illustration", parent); Height(rt, height); var raw = rt.gameObject.AddComponent<UnityEngine.UI.RawImage>(); raw.texture = texture; raw.raycastTarget = false;
            rt.gameObject.AddComponent<ArtCrop>(); Border(rt);
        }
        public static void EventIllustration(Transform parent, Texture texture)
        {
            var frame = Card(parent, "Event illustration frame", Ink); Height(frame, 400); Border(frame);
            var picture = Node("Event illustration", frame); Fill(picture, 2, 2, 2, 2);
            var raw = picture.gameObject.AddComponent<PixelArtImage>();
            raw.texture = texture; raw.raycastTarget = false;
        }
        public static void Bar(Transform parent, float x, float y, float width, float ratio, Color color)
        {
            var track = Card(parent, "Meter", Ink); Box(track, x, y, width, 5);
            var fill = Card(track, "Value", color); fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1); fill.offsetMin = fill.offsetMax = Vector2.zero;
        }
        public static string Safe(string text) => (text ?? "").Replace("<", "‹").Replace(">", "›");
    }
}
