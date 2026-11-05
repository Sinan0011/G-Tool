using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
namespace GTool.Windows;

/// Glass backdrop: the desktop wallpaper, blurred and aligned to where each window sits on screen,
/// so it looks like the window is a pane of frosted glass over the desktop. Drawing it ourselves
/// keeps it working when Windows' "Transparency effects" switch is off, which stops system blur.
internal static class WallpaperGlass
{
    private const int DecodeWidth=960;
    private const double BlurRadius=48;
    private static BitmapSource? blurred,tinted;
    private static string? loadedPath;
    private static bool listening;
    private static readonly List<WeakReference<ImageBrush>> brushes=[];
    internal static bool Available=>tinted is not null;
    internal static Color CaptionColor{get;private set;}

    /// Rebuilds the backdrop for the current wallpaper and theme. Without a wallpaper (solid color desktop) Available is false.
    internal static void Refresh(bool dark)
    {
        if(!listening)
        {
            listening=true;
            SystemEvents.UserPreferenceChanged+=(_,e)=>{if(e.Category==UserPreferenceCategory.Desktop)Application.Current?.Dispatcher.BeginInvoke(()=>Theme.ReapplyCurrent());};
        }
        try
        {
            var path=WallpaperPath();
            if(path is null){blurred=null;loadedPath=null;}
            else if(path!=loadedPath||blurred is null){blurred=Blur(path);loadedPath=path;}
            tinted=blurred is null?null:Tint(blurred,dark);
        }
        catch(Exception error)when(error is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or InvalidOperationException or COMException)
        {
            blurred=null;tinted=null;loadedPath=null;
        }
        if(tinted is not null)CaptionColor=TopEdgeColor(tinted);
        brushes.RemoveAll(reference=>!reference.TryGetTarget(out _));
        foreach(var reference in brushes)if(reference.TryGetTarget(out var brush))brush.ImageSource=tinted;
    }
    /// Creates a brush that keeps the blurred wallpaper aligned with `window` as it moves or resizes. Null when there is no wallpaper.
    internal static ImageBrush? Follow(Window window)
    {
        if(tinted is null)return null;
        var brush=new ImageBrush(tinted){ViewboxUnits=BrushMappingMode.Absolute,Stretch=Stretch.Fill};brushes.Add(new(brush));
        void Align()=>AlignTo(window,brush);
        window.SourceInitialized+=(_,_)=>Align();window.Loaded+=(_,_)=>Align();window.LocationChanged+=(_,_)=>Align();window.SizeChanged+=(_,_)=>Align();window.ContentRendered+=(_,_)=>Align();
        Align();return brush;
    }
    private static void AlignTo(Window window,ImageBrush brush)
    {
        if(brush.ImageSource is not BitmapSource image||PresentationSource.FromVisual(window)?.CompositionTarget is not {} target)return;
        var handle=new WindowInteropHelper(window).Handle;if(handle==IntPtr.Zero)return;
        var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};if(!GetMonitorInfo(MonitorFromWindow(handle,MonitorDefaultToNearest),ref info))return;
        // Same mapping as the desktop's default "Fill": cover the monitor, centered, keeping the aspect ratio.
        var monitorWidth=(double)(info.Right-info.Left);var monitorHeight=(double)(info.Bottom-info.Top);
        var scale=Math.Max(monitorWidth/image.PixelWidth,monitorHeight/image.PixelHeight);if(scale<=0)return;
        var originX=info.Left+(monitorWidth-image.PixelWidth*scale)/2;var originY=info.Top+(monitorHeight-image.PixelHeight*scale)/2;
        var corner=window.PointToScreen(new Point(0,0));var device=target.TransformToDevice;
        brush.Viewbox=new Rect((corner.X-originX)/scale,(corner.Y-originY)/scale,window.ActualWidth*device.M11/scale,window.ActualHeight*device.M22/scale);
    }
    private static string? WallpaperPath()
    {
        var buffer=new char[1024];if(!SystemParametersInfo(GetDesktopWallpaper,(uint)buffer.Length,buffer,0))return null;
        var path=new string(buffer).TrimEnd('\0');var end=path.IndexOf('\0');if(end>=0)path=path[..end];
        if(path.Length==0)return null;
        if(File.Exists(path))return path;
        // Slideshows and synced wallpapers can point elsewhere; Windows keeps its current copy here.
        var current=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Microsoft","Windows","Themes","TranscodedWallpaper");
        return File.Exists(current)?current:null;
    }
    private static BitmapSource Blur(string path)
    {
        var source=new BitmapImage();source.BeginInit();source.CacheOption=BitmapCacheOption.OnLoad;source.CreateOptions=BitmapCreateOptions.IgnoreColorProfile;source.DecodePixelWidth=DecodeWidth;source.UriSource=new Uri(path);source.EndInit();source.Freeze();
        var width=source.PixelWidth;var height=source.PixelHeight;
        var visual=new DrawingVisual{Effect=new BlurEffect{Radius=BlurRadius,KernelType=KernelType.Gaussian,RenderingBias=RenderingBias.Quality}};
        // Drawn slightly oversized so the blur does not fade to transparent at the edges.
        using(var drawing=visual.RenderOpen())drawing.DrawImage(source,new Rect(-BlurRadius*2,-BlurRadius*2,width+BlurRadius*4,height+BlurRadius*4));
        return Render(visual,width,height);
    }
    private static BitmapSource Tint(BitmapSource image,bool dark)
    {
        var visual=new DrawingVisual();var rect=new Rect(0,0,image.PixelWidth,image.PixelHeight);
        using(var drawing=visual.RenderOpen())
        {
            drawing.DrawImage(image,rect);
            // Enough tint to keep text readable over any wallpaper, little enough that the wallpaper's colors stay visible.
            var tint=new SolidColorBrush(dark?Color.FromArgb(120,11,21,33):Color.FromArgb(120,244,249,253));tint.Freeze();
            drawing.DrawRectangle(tint,null,rect);
        }
        return Render(visual,image.PixelWidth,image.PixelHeight);
    }
    private static BitmapSource Render(Visual visual,int width,int height)
    {
        var result=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);result.Render(visual);result.Freeze();return result;
    }
    private static Color TopEdgeColor(BitmapSource image)
    {
        var width=image.PixelWidth;var pixels=new byte[width*4];image.CopyPixels(new Int32Rect(0,0,width,1),pixels,width*4,0);
        long blue=0,green=0,red=0;for(var i=0;i<pixels.Length;i+=4){blue+=pixels[i];green+=pixels[i+1];red+=pixels[i+2];}
        return Color.FromRgb((byte)(red/width),(byte)(green/width),(byte)(blue/width));
    }
    private const uint GetDesktopWallpaper=0x0073,MonitorDefaultToNearest=2;
    [StructLayout(LayoutKind.Sequential)]private struct MonitorInfo{public int Size,Left,Top,Right,Bottom,WorkLeft,WorkTop,WorkRight,WorkBottom,Flags;}
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool SystemParametersInfo(uint action,uint size,[Out]char[] buffer,uint flags);
    [DllImport("user32.dll")]private static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
}
