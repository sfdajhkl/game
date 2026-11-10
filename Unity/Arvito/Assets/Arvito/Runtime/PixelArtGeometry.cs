using System;

namespace Arvito.Runtime
{
    /// <summary>Fit a native raster to physical screen pixels, independently of CanvasScaler.</summary>
    public static class PixelArtGeometry
    {
        public readonly struct Frame
        {
            public readonly int x, y, width, height, scale;
            public Frame(int x, int y, int width, int height, int scale)
            { this.x=x; this.y=y; this.width=width; this.height=height; this.scale=scale; }
        }

        public static Frame Fit(float left, float bottom, float right, float top, int sourceWidth, int sourceHeight)
        {
            if (sourceWidth<=0 || sourceHeight<=0 || !(right>left) || !(top>bottom)) return default;
            int x=(int)Math.Ceiling(left), y=(int)Math.Ceiling(bottom);
            int width=(int)Math.Floor(right)-x, height=(int)Math.Floor(top)-y;
            // Keep native pixels even in a very small portrait frame; never decimate the source.
            int scale=Math.Max(1,Math.Min(width/sourceWidth,height/sourceHeight));
            int imageWidth=sourceWidth*scale, imageHeight=sourceHeight*scale;
            return new Frame(x+(width-imageWidth)/2,y+(height-imageHeight)/2,imageWidth,imageHeight,scale);
        }
    }
}
