using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace GTool.Windows;

internal static class Theme
{
    // The G-Tool color map, top to bottom.
    internal static readonly Color MapTop=Color.FromRgb(0x00,0xF0,0xFF),MapUpperMiddle=Color.FromRgb(0x02,0xBD,0xDE),AccentColor=Color.FromRgb(0x02,0x97,0xDE),MapLowerMiddle=Color.FromRgb(0x0A,0x5E,0xFF),MapBottom=Color.FromRgb(0x00,0x28,0x75);
    private const string Personalize=@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    internal static bool Dark{get;private set;}
    /// Glass surfaces sit on the blurred wallpaper. With no wallpaper to show, the same design uses opaque colors so text stays readable.
    internal static bool GlassEnabled=>WallpaperGlass.Available;
    internal static Brush Primary=>new LinearGradientBrush(MapUpperMiddle,MapLowerMiddle,90);
    internal static Brush Brush(string name)=>(Brush)Application.Current.FindResource(name);
    private static string appearance="dark";
    // key, glass light, glass dark, opaque light, opaque dark. "a>b" is a top-to-bottom gradient; the bright upper edge reads as a glass rim.
    private static readonly (string Key,string GlassLight,string GlassDark,string SolidLight,string SolidDark)[] Palette=
    [
        ("Paper","#E8F4FC>#F7FAFD","#0B1521","#E8F4FC>#F7FAFD","#0B1521"),
        ("Surface","#A6FFFFFF>#59FFFFFF","#2EFFFFFF>#0FFFFFFF","#FFFFFF","#18242F"),
        ("Inset","#40FFFFFF","#29000000","#EEF4F9","#101922"),
        ("SolidSurface","#F7FBFDFF","#F2141E28","#FFFFFF","#18242F"),
        ("Ink","#0B2233","#F3F8FC","#0B2233","#F3F8FC"),
        ("Muted","#3B5062","#C2D2DE","#43586A","#B4C6D4"),
        ("Line","#F2FFFFFF>#59FFFFFF","#80FFFFFF>#1AFFFFFF","#D5E0E9","#2C3B48"),
        ("AccentSoft","#400297DE","#590297DE","#330297DE","#4D0297DE"),
        ("AccentFaint","#260297DE","#2E0297DE","#1F0297DE","#2E0297DE"),
        ("Accent","#0297DE","#0297DE","#0297DE","#0297DE"),
        ("WheelHub","#99FFFFFF>#4DFFFFFF","#40FFFFFF>#14FFFFFF","#EEF4F9","#101922"),
        ("WheelSector","#66FFFFFF>#1FFFFFFF","#2EFFFFFF>#0AFFFFFF","#EEF4F9","#101922"),
        ("WheelLine","#D9FFFFFF>#59FFFFFF","#8CFFFFFF>#1FFFFFFF","#D5E0E9","#2C3B48"),
    ];
    private static Color ParseColor(string value)=>(Color)ColorConverter.ConvertFromString(value);
    private static Brush Paint(string value)
    {
        var stops=value.Split('>');
        return stops.Length==2?new LinearGradientBrush(ParseColor(stops[0]),ParseColor(stops[1]),90):new SolidColorBrush(ParseColor(value));
    }
    internal static void ReapplyCurrent()=>Apply(appearance);
    internal static void Apply(string appearance)
    {
        Theme.appearance=appearance;
        Dark=appearance=="dark"||(appearance=="system"&&Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize","AppsUseLightTheme",1) is int value&&value==0);
        WallpaperGlass.Refresh(Dark);
        foreach(var (key,glassLight,glassDark,solidLight,solidDark) in Palette)
            Application.Current.Resources[key]=Paint(GlassEnabled?(Dark?glassDark:glassLight):(Dark?solidDark:solidLight));
        foreach(Window window in Application.Current.Windows){ApplyTitleBar(window);ApplyBackdrop(window);}
    }
    internal static void Attach(Window window)
    {
        window.SetResourceReference(Window.BackgroundProperty,"Paper");window.SetResourceReference(Window.ForegroundProperty,"Ink");
        window.Loaded+=(_,_)=>{if(window.WindowStyle!=WindowStyle.None)DesktopGeometry.FitToWorkArea(window);L10n.Bind(window,Window.TitleProperty,window.Title);L10n.Attach(window);};
        window.SourceInitialized+=(_,_)=>{ApplyBackdrop(window);ApplyTitleBar(window);};
    }
    internal static void Entrance(UIElement element)
    {
        if(!WheelPresentation.Preferences.Animations||!SystemParameters.ClientAreaAnimation)return;
        element.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(180)));
        var offset=new TranslateTransform();element.RenderTransform=offset;
        offset.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(8,0,TimeSpan.FromMilliseconds(180)){DecelerationRatio=.8});
    }
    private static void ApplyBackdrop(Window window)
    {
        if(window.AllowsTransparency||window.WindowStyle==WindowStyle.None||!IsThemed(window))return;
        if(!GlassEnabled){if(window.Background is ImageBrush)window.SetResourceReference(Window.BackgroundProperty,"Paper");return;}
        if(window.Background is not ImageBrush&&WallpaperGlass.Follow(window) is {} brush)window.Background=brush;
    }
    // Only windows that went through Attach follow the theme's backdrop.
    private static bool IsThemed(Window window)=>window.Background is ImageBrush||ReferenceEquals(window.Background,Application.Current.TryFindResource("Paper"));
    private static void ApplyTitleBar(Window window)
    {
        var handle=new WindowInteropHelper(window).Handle;if(handle==IntPtr.Zero)return;var dark=Dark?1:0;_ = DwmSetWindowAttribute(handle,DwmUseDarkMode,ref dark,sizeof(int));
        // The title bar takes the wallpaper's tone at the top of the glass, so it does not read as a separate opaque strip.
        var caption=GlassEnabled&&window.Background is ImageBrush?WallpaperGlass.CaptionColor.R|WallpaperGlass.CaptionColor.G<<8|WallpaperGlass.CaptionColor.B<<16:DwmColorDefault;
        _ = DwmSetWindowAttribute(handle,DwmCaptionColor,ref caption,sizeof(int));
    }
    private const int DwmUseDarkMode=20,DwmCaptionColor=35,DwmColorDefault=unchecked((int)0xFFFFFFFF);
    [DllImport("dwmapi.dll")]private static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
}
