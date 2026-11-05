using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using GTool.Core;
using Ellipse=System.Windows.Shapes.Ellipse;
namespace GTool.Windows;
internal static class WheelPresentation
{
    internal const double Diameter=320;
    internal static double StartAngle(int count)=>count==3?-180:-90;
    internal static Preferences Preferences{get;set;}=new();
    internal const double Center=270,InnerRadius=108,OuterRadius=250,GapDegrees=1.4,CornerRadius=13;
    internal static bool Glass=>Preferences.WheelFinish=="glass"&&Theme.GlassEnabled;
    internal static Brush Backdrop=>Theme.Brush("SolidSurface");
    internal static Brush SectorFill=>Theme.Brush(Glass?"WheelSector":"Inset");
    internal static Brush SectorLine=>Theme.Brush(Glass?"WheelLine":"Line");
    private static bool Animated=>Preferences.Animations&&SystemParameters.ClientAreaAnimation;
    private static BitmapImage? hubMark;
    internal static BitmapImage HubMark
    {
        get
        {
            if(hubMark is not null)return hubMark;
            var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.DecodePixelWidth=128;image.UriSource=new Uri("pack://application:,,,/Assets/GToolMark.png");image.EndInit();image.Freeze();
            return hubMark=image;
        }
    }
    private static Brush Frozen(Brush brush){brush.Freeze();return brush;}
    private static Color White(byte alpha)=>Color.FromArgb(alpha,255,255,255);
    // The color map, innermost to outermost. Its stops span the sector, from the inner edge to the outer edge.
    private static readonly Brush MapBrush=Frozen(new RadialGradientBrush
    {
        MappingMode=BrushMappingMode.Absolute,Center=new Point(Center,Center),GradientOrigin=new Point(Center,Center),RadiusX=OuterRadius,RadiusY=OuterRadius,
        GradientStops=
        [
            new(Theme.MapTop,InnerRadius/OuterRadius),
            new(Theme.MapUpperMiddle,InnerRadius/OuterRadius+(1-InnerRadius/OuterRadius)*.25),
            new(Theme.AccentColor,InnerRadius/OuterRadius+(1-InnerRadius/OuterRadius)*.5),
            new(Theme.MapLowerMiddle,InnerRadius/OuterRadius+(1-InnerRadius/OuterRadius)*.75),
            new(Theme.MapBottom,1),
        ],
    });
    /// Light catches a glass edge unevenly: bright upper left, dim middle, a second glint lower right.
    internal static Brush Rim()=>Frozen(new LinearGradientBrush
    {
        StartPoint=new Point(.15,0),EndPoint=new Point(.85,1),
        GradientStops=[new(White(245),0),new(White(70),.42),new(White(35),.6),new(White(165),1)],
    });
    /// Sector outline with rounded corners. The arcs keep the wheel's circle; only the four corners are eased.
    internal static Geometry Sector(double a,double b)
    {
        Point P(double r,double v)=>new(Center+r*Math.Cos(v),Center+r*Math.Sin(v));
        var outer=CornerRadius/OuterRadius;var inner=CornerRadius/InnerRadius;
        var geometry=new StreamGeometry();
        using(var g=geometry.Open())
        {
            g.BeginFigure(P(InnerRadius+CornerRadius,a),true,true);
            g.LineTo(P(OuterRadius-CornerRadius,a),true,true);
            g.QuadraticBezierTo(P(OuterRadius,a),P(OuterRadius,a+outer),true,true);
            g.ArcTo(P(OuterRadius,b-outer),new Size(OuterRadius,OuterRadius),0,b-a-2*outer>Math.PI,SweepDirection.Clockwise,true,true);
            g.QuadraticBezierTo(P(OuterRadius,b),P(OuterRadius-CornerRadius,b),true,true);
            g.LineTo(P(InnerRadius+CornerRadius,b),true,true);
            g.QuadraticBezierTo(P(InnerRadius,b),P(InnerRadius,b-inner),true,true);
            g.ArcTo(P(InnerRadius,a+inner),new Size(InnerRadius,InnerRadius),0,b-a-2*inner>Math.PI,SweepDirection.Counterclockwise,true,true);
            g.QuadraticBezierTo(P(InnerRadius,a),P(InnerRadius+CornerRadius,a),true,true);
        }
        geometry.Freeze();return geometry;
    }
    /// The lit state of a sector: color map, a soft sheen and a bright edge. Hidden until the pointer arrives.
    internal static Canvas Glow(Geometry sector)
    {
        var glow=new Canvas{Width=540,Height=540,Opacity=0,IsHitTestVisible=false};
        glow.Children.Add(new System.Windows.Shapes.Path{Data=sector,Fill=MapBrush});
        glow.Children.Add(new System.Windows.Shapes.Path{Data=sector,Fill=Frozen(new LinearGradientBrush(White(70),White(0),90)),Stroke=Rim(),StrokeThickness=1.5});
        return glow;
    }
    /// Lights or dims one sector. The glow is removed once dark, so idle sectors cost nothing to draw.
    internal static void SetActive(Canvas glow,ScaleTransform? zoom,bool active)
    {
        if(active)glow.Effect=new DropShadowEffect{Color=Theme.MapTop,BlurRadius=30,ShadowDepth=0,Opacity=.7};
        var scale=active?1.028:1d;
        if(!Animated)
        {
            glow.BeginAnimation(UIElement.OpacityProperty,null);glow.Opacity=active?1:0;if(!active)glow.Effect=null;
            if(zoom is not null){zoom.BeginAnimation(ScaleTransform.ScaleXProperty,null);zoom.BeginAnimation(ScaleTransform.ScaleYProperty,null);zoom.ScaleX=scale;zoom.ScaleY=scale;}
            return;
        }
        var ease=new CubicEase{EasingMode=EasingMode.EaseOut};var time=TimeSpan.FromMilliseconds(active?110:190);
        var fade=new DoubleAnimation(active?1:0,time){EasingFunction=ease};
        if(!active)fade.Completed+=(_,_)=>{if(glow.Opacity<=.01)glow.Effect=null;};
        glow.BeginAnimation(UIElement.OpacityProperty,fade);
        if(zoom is null)return;
        var grow=new DoubleAnimation(scale,time){EasingFunction=ease};zoom.BeginAnimation(ScaleTransform.ScaleXProperty,grow);zoom.BeginAnimation(ScaleTransform.ScaleYProperty,grow);
    }
    // White reads best on the lit segment; every other segment keeps the theme's text color.
    internal static void Recolor(Panel label,Brush color)
    {
        foreach(UIElement child in label.Children)
        {
            if(child is TextBlock text)text.Foreground=color;
            else if(child is Canvas glyph)foreach(UIElement part in glyph.Children)if(part is System.Windows.Shapes.Shape shape){if(shape.Stroke is not null)shape.Stroke=color;if(shape.Fill is not null)shape.Fill=color;}
        }
    }
    // Keeps white labels legible where the color map is at its brightest.
    internal static DropShadowEffect LabelShadow()=>new(){Color=Theme.MapBottom,BlurRadius=9,ShadowDepth=0,Opacity=.95};
    /// Lens layer over the disc: edges slightly brighter than the middle.
    internal static Canvas GlassDecor()
    {
        var decor=new Canvas{Width=540,Height=540,IsHitTestVisible=false};
        if(!Glass)return decor;
        var lens=new RadialGradientBrush{GradientStops=[new(White(0),.62),new(White(14),.86),new(White(52),1)]};lens.Freeze();
        var edge=new Ellipse{Width=526,Height=526,Fill=lens};Canvas.SetLeft(edge,7);Canvas.SetTop(edge,7);decor.Children.Add(edge);
        return decor;
    }
    /// The raised glass button in the middle of the wheel.
    internal static Ellipse Hub()
    {
        const double diameter=186;
        var hub=new Ellipse{Width=diameter,Height=diameter,IsHitTestVisible=false,Fill=Glass?Theme.Brush("WheelHub"):Theme.Brush("Inset"),Stroke=Glass?Rim():SectorLine,StrokeThickness=Glass?1.6:1};
        if(Glass)hub.Effect=new DropShadowEffect{Color=Colors.Black,BlurRadius=20,ShadowDepth=4,Direction=270,Opacity=.35};
        Canvas.SetLeft(hub,Center-diameter/2);Canvas.SetTop(hub,Center-diameter/2);return hub;
    }
    /// Opens with a small settle, like a lens coming into focus.
    internal static void Open(FrameworkElement frame)
    {
        if(!Animated)return;
        var zoom=new ScaleTransform(.93,.93);frame.RenderTransformOrigin=new Point(.5,.5);frame.RenderTransform=zoom;
        var settle=new DoubleAnimation(.93,1,TimeSpan.FromMilliseconds(210)){EasingFunction=new BackEase{Amplitude=.28,EasingMode=EasingMode.EaseOut}};
        zoom.BeginAnimation(ScaleTransform.ScaleXProperty,settle);zoom.BeginAnimation(ScaleTransform.ScaleYProperty,settle);
    }
    internal static double LabelRadius(int count,double half)=>count<=2?180:4*Math.Sin(half)*(Math.Pow(250,3)-Math.Pow(108,3))/(6*half*(250*250-108*108));
    internal static (StackPanel Label,Point Center) Caption(ToolChoice choice,Geometry petal,double angle,double half,int count)
    {
        var preferred=LabelRadius(count,half);
        for(var font=16d;font>=10;font-=.5)for(var width=142d;width>=72;width-=7)
        {
            // Wrap between words only: a width that would split a word ("ANNOTAT-E") is not a valid layout.
            var longest=new System.Windows.Controls.TextBlock{Text=L10n.Uppercase(choice.Title).Split(' ').MaxBy(word=>word.Length)??"",FontSize=font,FontWeight=FontWeights.SemiBold};longest.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));if(longest.DesiredSize.Width>width)continue;
            var label=new StackPanel{Width=width,IsHitTestVisible=false};
            if(choice.Placement is {} placement){var glyph=new System.Windows.Controls.Canvas{Width=36,Height=25,HorizontalAlignment=HorizontalAlignment.Center,Margin=new(0,0,0,7)};glyph.Children.Add(new System.Windows.Shapes.Rectangle{Width=35,Height=24,RadiusX=3,RadiusY=3,Stroke=Theme.Brush("Ink"),StrokeThickness=1.7});var (start,end)=placement.Range();var fill=new System.Windows.Shapes.Rectangle{Width=29*(end-start),Height=18,Fill=Theme.Brush("Ink"),Opacity=.65};System.Windows.Controls.Canvas.SetLeft(fill,3+29*start);System.Windows.Controls.Canvas.SetTop(fill,3);glyph.Children.Add(fill);label.Children.Add(glyph);}
            else label.Children.Add(new System.Windows.Controls.TextBlock{Text=choice.Symbol,FontSize=32,TextAlignment=TextAlignment.Center,Foreground=Theme.Brush("Ink"),Margin=new(0,0,0,6)});
            label.Children.Add(new System.Windows.Controls.TextBlock{Text=L10n.Uppercase(choice.Title),FontSize=font,FontWeight=FontWeights.SemiBold,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap,Foreground=Theme.Brush("Ink")});
            label.Measure(new Size(width,double.PositiveInfinity));var h=label.DesiredSize.Height;
            for(var step=0;step<=20;step++)
            {
                var radial=preferred+(step==0?0:(step%2==1?1:-1)*((step+1)/2)*5);var center=new Point(270+radial*Math.Cos(angle),270+radial*Math.Sin(angle));var r=new Rect(center.X-width/2-3,center.Y-h/2-3,width+6,h+6);
                if(new[]{r.TopLeft,r.TopRight,r.BottomLeft,r.BottomRight,new Point(r.Left,r.Top+r.Height/2),new Point(r.Right,r.Top+r.Height/2)}.All(petal.FillContains))return(label,center);
            }
        }
        var fallback=new StackPanel{Width=72};fallback.Children.Add(new System.Windows.Controls.TextBlock{Text=L10n.Uppercase(choice.Title),FontSize=10,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap,Foreground=Theme.Brush("Ink")});fallback.Measure(new Size(72,double.PositiveInfinity));return(fallback,new Point(270+preferred*Math.Cos(angle),270+preferred*Math.Sin(angle)));
    }
    internal static void Animate(Window window,double from=0){if(Preferences.Animations&&SystemParameters.ClientAreaAnimation)window.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(from,1,TimeSpan.FromMilliseconds(140)));}
}
