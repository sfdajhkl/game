using System;
using Arvito.Runtime;

static class Program
{
    static int passed;
    static void Assert(bool value) { if(!value) throw new Exception("Pixel layout assertion failed"); }
    static void Check(string name,Action action) { action();passed++;Console.WriteLine("PASS "+name); }
    static void Main()
    {
        Check("native 64px portrait stays 64 screen pixels",()=>{
            var f=PixelArtGeometry.Fit(0,0,76,76,64,64);
            Assert(f.x==6 && f.y==6 && f.width==64 && f.height==64 && f.scale==1);
        });
        Check("large battle portrait uses integer enlargement",()=>{
            var f=PixelArtGeometry.Fit(100,50,350,260,64,64);
            Assert(f.x==129 && f.y==59 && f.width==192 && f.height==192 && f.scale==3);
        });
        Check("fractional canvas coordinates snap without stretching",()=>{
            var f=PixelArtGeometry.Fit(10.3f,20.7f,224.8f,238.9f,64,64);
            Assert(f.x==21 && f.y==33 && f.width==192 && f.height==192);
        });
        Check("small windows retain every source pixel",()=>{
            var f=PixelArtGeometry.Fit(0,0,59,59,64,64);
            Assert(f.scale==1 && f.width==64 && f.height==64);
        });
        Check("empty layout/invalid source cannot emit a quad",()=>{
            Assert(PixelArtGeometry.Fit(0,0,0,100,64,64).scale==0);
            Assert(PixelArtGeometry.Fit(0,0,100,100,0,64).scale==0);
        });
        Check("fallback textures keep their native square pixel size",()=>{
            var f=PixelArtGeometry.Fit(0,0,100,150,32,32);
            Assert(f.width==96 && f.height==96 && f.scale==3);
        });
        Check("portrait and battle sizes remain square at eight desktop resolutions",()=>{
            var sizes=new[]{(1024,768),(1280,720),(1366,768),(1440,900),(1920,1080),(2560,1440),(3440,1440),(3840,2160)};
            foreach(var size in sizes)
            {
                // Same ScaleWithScreenSize + matchWidthOrHeight=.5 as GameApp.
                float canvasScale=(float)Math.Sqrt(size.Item1/1440.0*size.Item2/900.0);
                foreach(int referenceSize in new[]{76,88,170,240,320})
                {
                    float side=referenceSize*canvasScale;
                    var f=PixelArtGeometry.Fit(6.25f,9.75f,6.25f+side,9.75f+side,64,64);
                    Assert(f.scale>=1 && f.width==64*f.scale && f.height==f.width);
                    Assert(Math.Abs(f.x+f.width/2.0-(6.25+side/2))<=1.01);
                    Assert(Math.Abs(f.y+f.height/2.0-(9.75+side/2))<=1.01);
                }
            }
        });
        Check("each raster pixel occupies a uniform integer square after resizing",()=>{
            foreach(int side in new[]{64,76,128,170,192,250,256,384,512})
            {
                var f=PixelArtGeometry.Fit(0,0,side,side,64,64);
                var coverage=new int[64];
                for(int screenX=0;screenX<f.width;screenX++)
                {
                    int texel=(int)((screenX+.5)/f.width*64);
                    Assert(texel>=0 && texel<64);coverage[texel]++;
                }
                foreach(int count in coverage) Assert(count==f.scale);
            }
        });
        Check("event illustrations keep the full 3:2 raster at desktop canvas scales",()=>{
            foreach(var size in new[]{(1024,768),(1280,720),(1366,768),(1440,900),(1920,1080),(2560,1440),(3440,1440),(3840,2160)})
            {
                float canvasScale=(float)Math.Sqrt(size.Item1/1440.0*size.Item2/900.0);
                float width=700*canvasScale, height=396*canvasScale;
                var f=PixelArtGeometry.Fit(10.25f,12.75f,10.25f+width,12.75f+height,192,128);
                Assert(f.scale>=1 && f.width==192*f.scale && f.height==128*f.scale);
                Assert(f.x>=10.25f && f.y>=12.75f && f.x+f.width<=10.25f+width && f.y+f.height<=12.75f+height);
            }
        });
        Console.WriteLine(passed+" pixel layout checks passed (geometry only; native Unity rendering requires Play Mode).");
    }
}
