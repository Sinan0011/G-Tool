using System.Windows;
using GTool.Core;
namespace GTool.Windows;
/// Hosts the settings panel in its own window. The app shows the panel inside the main window; checks use this host.
public sealed class SettingsWindow:Window
{
    public SettingsWindow(Window owner,PreferencesStore store,Action changed,Func<Task> update,LibraryStore? appearanceStore=null)
    {
        Owner=owner;Theme.Attach(this);Title="Make it yours · G-Tool";Width=850;Height=680;MinWidth=740;MinHeight=500;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        Content=new SettingsView(store,changed,update,appearanceStore);
        PreviewKeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Escape){Close();e.Handled=true;}};
    }
}
