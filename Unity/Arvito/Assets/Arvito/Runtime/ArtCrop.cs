using UnityEngine;

namespace Arvito.Runtime
{
    public sealed class ArtCrop : MonoBehaviour
    {
        UnityEngine.UI.RawImage raw;
        RectTransform rect;
        void Awake() { raw = GetComponent<UnityEngine.UI.RawImage>(); rect = (RectTransform)transform; }
        void LateUpdate()
        {
            if (raw.texture == null) return;
            var r = rect.rect; if (r.height <= 0 || r.width <= 0) return;
            float imageRatio = (float)raw.texture.width / raw.texture.height, viewRatio = r.width / r.height;
            raw.uvRect = viewRatio > imageRatio ? new Rect(0, (1 - imageRatio / viewRatio) / 2, 1, imageRatio / viewRatio) : new Rect((1 - viewRatio / imageRatio) / 2, 0, viewRatio / imageRatio, 1);
        }
    }
}
