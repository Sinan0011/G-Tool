using Microsoft.Win32;
namespace GTool.Windows;
/// Starting at sign-in uses the current user's Run key, so no administrator rights are needed.
internal static class LaunchAtLogin
{
    private const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";private const string Name="G-Tool";
    internal static bool IsEnabled(){using var key=Registry.CurrentUser.OpenSubKey(RunKey);return key?.GetValue(Name) is string;}
    internal static void Set(bool enabled){using var run=Registry.CurrentUser.CreateSubKey(RunKey);if(enabled)run.SetValue(Name,"\""+Environment.ProcessPath+"\" --background");else run.DeleteValue(Name,false);}
}
