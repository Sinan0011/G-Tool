using System.Reflection;
namespace GTool.Windows;
internal static class AppVersion
{
    public static Version Current=>Assembly.GetExecutingAssembly().GetName().Version??new(0,7,0);
}
