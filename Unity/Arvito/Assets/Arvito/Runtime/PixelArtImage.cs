using UnityEngine;

namespace Arvito.Runtime
{
    /// <summary>Overlay UI image with one texel per integer square of physical screen pixels.</summary>
    [AddComponentMenu("Arvito/Pixel Art Image")]
    public sealed class PixelArtImage : UnityEngine.UI.RawImage
    {
        Rect previousScreenRect;
        Camera UiCamera => canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        Rect ScreenRect()
        {
            var rect=rectTransform.rect;
            var min=RectTransformUtility.WorldToScreenPoint(UiCamera,rectTransform.TransformPoint(rect.min));
            var max=RectTransformUtility.WorldToScreenPoint(UiCamera,rectTransform.TransformPoint(rect.max));
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }

        void LateUpdate()
        {
            // A window resize can change canvas scale without changing this rect's local size.
            if (ScreenRect()!=previousScreenRect) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vertices)
        {
            vertices.Clear();
            if (texture == null) return;
            var screen=ScreenRect(); previousScreenRect=screen;
            int width=Mathf.RoundToInt(texture.width*Mathf.Abs(uvRect.width));
            int height=Mathf.RoundToInt(texture.height*Mathf.Abs(uvRect.height));
            var frame=PixelArtGeometry.Fit(screen.xMin,screen.yMin,screen.xMax,screen.yMax,width,height);
            if (frame.scale==0) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,new Vector2(frame.x,frame.y),UiCamera,out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,new Vector2(frame.x+frame.width,frame.y+frame.height),UiCamera,out var max);
            Color32 tint=color;
            vertices.AddVert(new Vector3(min.x,min.y),tint,new Vector2(uvRect.xMin,uvRect.yMin));
            vertices.AddVert(new Vector3(min.x,max.y),tint,new Vector2(uvRect.xMin,uvRect.yMax));
            vertices.AddVert(new Vector3(max.x,max.y),tint,new Vector2(uvRect.xMax,uvRect.yMax));
            vertices.AddVert(new Vector3(max.x,min.y),tint,new Vector2(uvRect.xMax,uvRect.yMin));
            vertices.AddTriangle(0,1,2); vertices.AddTriangle(2,3,0);
        }
    }
}
