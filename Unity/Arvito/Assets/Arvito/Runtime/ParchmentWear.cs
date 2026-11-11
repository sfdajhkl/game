using UnityEngine;

namespace Arvito.Runtime
{
    /// <summary>Small deterministic tears and ink wear at the book edge, outside the reading area.</summary>
    public sealed class ParchmentWear : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            var r = rectTransform.rect;
            float w = r.width, h = r.height;
            if (w <= 0 || h <= 0) return;
            Vector2 Point(float x, float y) => new Vector2(r.xMin + x, r.yMax - y);
            void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
            {
                int n = mesh.currentVertCount;
                mesh.AddVert(a, color, Vector2.zero); mesh.AddVert(b, color, Vector2.zero); mesh.AddVert(c, color, Vector2.zero);
                mesh.AddTriangle(n, n + 1, n + 2);
            }
            void Line(float x, float y, float length, float thickness, Color color)
            {
                var a = Point(x, y); var b = Point(x + length, y); var c = Point(x + length, y + thickness); var d = Point(x, y + thickness);
                Triangle(a, b, c, color); Triangle(a, c, d, color);
            }
            Color faded = Ui.Gold; faded.a = .18f;
            Color faint = Ui.Gold; faint.a = .09f;
            // Fixed positions: UI redraws never consume gameplay RNG or make the paper flicker.
            float[] positions = { .08f, .27f, .61f, .86f };
            for (int i = 0; i < positions.Length; i++)
            {
                float x = w * positions[i], size = i % 2 == 0 ? 8 : 5;
                Triangle(Point(x - size, 0), Point(x + 1, size), Point(x + size, 0), Ui.Ink);
                Triangle(Point(w - x - 5, h), Point(w - x + 2, h - size * .65f), Point(w - x + 9, h), Ui.Ink);
                Line(x + 12, 5 + i % 2 * 2, 22 + i * 7, 1, faded);
                Line(w - x - 28, h - 7, 24 + i * 3, 1, faint);
            }
            Triangle(Point(0, 0), Point(0, 12), Point(9, 3), Ui.Ink);
            Triangle(Point(w, h), Point(w - 15, h), Point(w - 3, h - 10), Ui.Ink);
            Triangle(Point(0, h * .68f - 6), Point(5, h * .68f), Point(0, h * .68f + 10), Ui.Ink);
            Triangle(Point(w, h * .31f - 7), Point(w - 4, h * .31f), Point(w, h * .31f + 6), Ui.Ink);
            // Uneven water marks stay within a narrow corner band.
            for (int i = 0; i < 7; i++)
            {
                Line(12 + i * 3, h - 11 - i * 2, 54 - i * 6, 1, faint);
                Line(w - 82 + i * 4, 9 + i * 2, 45 - i * 5, 1, faint);
            }
        }
    }
}
