using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
namespace GTool.Windows;

/// Glass for round windows. Windows' own blur cannot be cut to a circle on a transparent window (it stays
/// square), so this takes one picture of the screen behind the wheel, blurs it and paints it inside the circle.
/// The picture only lives in memory while the wheel is open and is never saved or sent anywhere.
internal static class ScreenGlass
{
    private const double BlurRadius=26;
    internal static ImageBrush? Behind(Window window)
    {
        try
        {
            if(PresentationSource.FromVisual(window)?.CompositionTarget is not {} target)return null;
            var corner=window.PointToScreen(new Point(0,0));var width=(int)Math.Round(window.ActualWidth*target.TransformToDevice.M11);var height=(int)Math.Round(window.ActualHeight*target.TransformToDevice.M22);
            if(width<1||height<1)return null;
            var picture=Capture((int)Math.Round(corner.X),(int)Math.Round(corner.Y),width,height);
            if(picture is null)return null;
            var visual=new DrawingVisual{Effect=new BlurEffect{Radius=BlurRadius,KernelType=KernelType.Gaussian,RenderingBias=RenderingBias.Quality}};
            using(var drawing=visual.RenderOpen())drawing.DrawImage(picture,new Rect(-BlurRadius*2,-BlurRadius*2,width+BlurRadius*4,height+BlurRadius*4));
            var blurred=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);blurred.Render(visual);blurred.Freeze();
            var tinted=new DrawingVisual();
            using(var drawing=tinted.RenderOpen())
            {
                drawing.DrawImage(blurred,new Rect(0,0,width,height));
                // Enough tint to keep text readable over anything behind the wheel.
                var tint=new SolidColorBrush(Theme.Dark?Color.FromArgb(120,11,21,33):Color.FromArgb(120,244,249,253));tint.Freeze();
                drawing.DrawRectangle(tint,null,new Rect(0,0,width,height));
            }
            var result=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);result.Render(tinted);result.Freeze();
            var brush=new ImageBrush(result){Stretch=Stretch.Fill};brush.Freeze();return brush;
        }
        catch(Exception error)when(error is ExternalException or InvalidOperationException or ArgumentException or OutOfMemoryException)
        {
            return null;
        }
    }
    private static BitmapSource? Capture(int x,int y,int width,int height)
    {
        var screen=GetDC(IntPtr.Zero);if(screen==IntPtr.Zero)return null;var memory=CreateCompatibleDC(screen);var bitmap=CreateCompatibleBitmap(screen,width,height);
        try
        {
            if(memory==IntPtr.Zero||bitmap==IntPtr.Zero)return null;
            var previous=SelectObject(memory,bitmap);var copied=BitBlt(memory,0,0,width,height,screen,x,y,0x00CC0020);SelectObject(memory,previous);
            if(!copied)return null;
            var source=Imaging.CreateBitmapSourceFromHBitmap(bitmap,IntPtr.Zero,Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions());source.Freeze();return source;
        }
        finally
        {
            if(bitmap!=IntPtr.Zero)DeleteObject(bitmap);
            if(memory!=IntPtr.Zero)DeleteDC(memory);
            _ = ReleaseDC(IntPtr.Zero,screen);
        }
    }
    [DllImport("user32.dll")]private static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll")]private static extern int ReleaseDC(IntPtr window,IntPtr context);
    [DllImport("gdi32.dll")]private static extern IntPtr CreateCompatibleDC(IntPtr context);
    [DllImport("gdi32.dll")]private static extern IntPtr CreateCompatibleBitmap(IntPtr context,int width,int height);
    [DllImport("gdi32.dll")]private static extern IntPtr SelectObject(IntPtr context,IntPtr item);
    [DllImport("gdi32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool BitBlt(IntPtr target,int x,int y,int width,int height,IntPtr source,int sourceX,int sourceY,uint operation);
    [DllImport("gdi32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool DeleteObject(IntPtr item);
    [DllImport("gdi32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool DeleteDC(IntPtr context);
}
