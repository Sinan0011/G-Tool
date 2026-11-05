using System.Windows;
using System.Windows.Controls;
namespace GTool.Windows;
public sealed partial class MainWindow
{
    private Border? launchBanner;
    // Checks replace these so they never change the real Windows startup list.
    internal Func<bool> LaunchAtLoginEnabled=LaunchAtLogin.IsEnabled;internal Action<bool> SetLaunchAtLogin=LaunchAtLogin.Set;
    internal bool LaunchBannerVisible=>launchBanner?.Visibility==Visibility.Visible;
    internal bool LaunchQuestionAnswered=>preferences.Value.LaunchAtLoginAsked;
    private void BuildLaunchBanner()
    {
        var row=new DockPanel();var buttons=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};DockPanel.SetDock(buttons,Dock.Right);
        buttons.Children.Add(Button("Not now",()=>AnswerLaunchAtLogin(false)));buttons.Children.Add(Primary("Yes, start it",()=>AnswerLaunchAtLogin(true)));row.Children.Add(buttons);
        var caption=new StackPanel();caption.Children.Add(Text("Start G-Tool when you sign in?",16));caption.Children.Add(Text("It waits in the tray and tells you about updates on time. You can change this in Settings.",12,"Muted"));row.Children.Add(caption);
        launchBanner=Card(row);launchBanner.Visibility=Visibility.Collapsed;home.Children.Add(launchBanner);
    }
    /// Asked once. Someone who already turned it on, or started G-Tool from sign-in, is not asked.
    internal void OfferLaunchAtLogin()
    {
        if(preferences.Value.LaunchAtLoginAsked||launchBanner is null)return;
        bool enabled;try{enabled=LaunchAtLoginEnabled();}catch(Exception error)when(error is IOException or UnauthorizedAccessException or System.Security.SecurityException){enabled=false;}
        if(enabled||Environment.GetCommandLineArgs().Contains("--background")){RememberLaunchAnswer();return;}
        launchBanner.Visibility=Visibility.Visible;
    }
    internal void AnswerLaunchAtLogin(bool start)
    {
        if(start)
        {
            try{SetLaunchAtLogin(true);L10n.SetText(status,"G-Tool will start when you sign in.");}
            catch(Exception error)when(error is IOException or UnauthorizedAccessException or System.Security.SecurityException){L10n.SetText(status,"G-Tool could not be added to startup. You can try again in Settings.");}
        }
        RememberLaunchAnswer();if(launchBanner is not null)launchBanner.Visibility=Visibility.Collapsed;
    }
    internal void ResetLaunchQuestionForCheck()=>preferences.Save(preferences.Value with{LaunchAtLoginAsked=false});
    private void RememberLaunchAnswer()
    {
        try{preferences.Save(preferences.Value with{LaunchAtLoginAsked=true});}
        catch(Exception error)when(error is IOException or UnauthorizedAccessException or ArgumentException){/* Asked again next time. */}
    }
}
