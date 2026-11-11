using UnityEngine;

namespace Arvito.Runtime
{
    /// <summary>Quiet, continuous deckled edges. Stable coordinates never consume gameplay RNG.</summary>
    public sealed class ParchmentWear : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            var r = rectTransform.rect;
            if (r.width <= 0 || r.height <= 0) return;
            Vector2 Point(int side, float t, float d)
            {
                switch (side)
                {
                    case 0: return new Vector2(r.xMin + t, r.yMax - d);
                    case 1: return new Vector2(r.xMax - d, r.yMax - t);
                    case 2: return new Vector2(r.xMax - t, r.yMin + d);
                    default: return new Vector2(r.xMin + d, r.yMin + t);
                }
            }
            void Band(int side, float a, float b, float outerA, float outerB, float innerA, float innerB, Color outer, Color inner)
            {
                int n = mesh.currentVertCount;
                mesh.AddVert(Point(side, a, outerA), outer, Vector2.zero);
                mesh.AddVert(Point(side, b, outerB), outer, Vector2.zero);
                mesh.AddVert(Point(side, b, innerB), inner, Vector2.zero);
                mesh.AddVert(Point(side, a, innerA), inner, Vector2.zero);
                mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
            }
            Color shade = Ui.Gold; shade.a = .22f;
            Color clear = shade; clear.a = 0;
            Color fiber = Ui.Raised; fiber.a = .38f;
            for (int side = 0; side < 4; side++)
            {
                float length = side % 2 == 0 ? r.width : r.height;
                int steps = Mathf.Clamp(Mathf.CeilToInt(length / 3), 1, 512);
                for (int i = 0; i < steps; i++)
                {
                    float a = length * i / steps, b = length * (i + 1) / steps;
                    float da = Depth(a, length, side), db = Depth(b, length, side);
                    // Oxidation fades inward; a narrow pale lip suggests exposed paper fibers.
                    Band(side, a, b, da, db, da + 30, db + 30, shade, clear);
                    Band(side, a, b, da, db, da + .65f, db + .65f, fiber, clear);
                    Band(side, a, b, 0, 0, da, db, Ui.Ink, Ui.Ink);
                }
            }
        }
        static float Depth(float t, float length, int side)
        {
            float phase = side * 2.31f;
            float edge = 2.8f + 1.1f * Mathf.Sin(t * .019f + phase)
                + .65f * Mathf.Sin(t * .083f + phase * 1.7f)
                + .3f * Mathf.Sin(t * .37f + phase);
            float corner = Mathf.Max(0, 1 - Mathf.Min(t, length - t) / 17);
            float dent = (t - length * (.19f + side * .137f)) / (7 + side * 2);
            float worn = (t - length * (.78f - side * .103f)) / 19;
            return edge + 5 * corner * corner + 3.4f * Mathf.Exp(-dent * dent) + 1.8f * Mathf.Exp(-worn * worn);
        }
    }
}
