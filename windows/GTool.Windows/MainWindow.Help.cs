using System.Windows;
using System.Windows.Controls;
namespace GTool.Windows;
public enum AppPage{Home,Library,Settings,Help}
public sealed partial class MainWindow
{
    private AppPage currentPage=AppPage.Home;
    private readonly Border settingsHost=new(){Visibility=Visibility.Collapsed};
    private readonly ScrollViewer helpScroll=new(){Visibility=Visibility.Collapsed};
    private readonly StackPanel helpPage=new();
    internal bool SettingsVisible=>currentPage==AppPage.Settings;
    internal bool HelpVisible=>currentPage==AppPage.Help;
    internal bool ToolsButtonVisible=>toolsSetupButton.Visibility==Visibility.Visible;
    internal FrameworkElement SettingsPage=>settingsHost;
    internal FrameworkElement HelpPage=>helpPage;
    private readonly List<Expander> helpHeadings=[];
    internal IEnumerable<string> HelpHeadings=>helpHeadings.Select(h=>h.Header as string??"");
    internal void ShowPage(AppPage page)
    {
        var changed=currentPage!=page;currentPage=page;
        if(!busy)L10n.SetText(status,"Files stay on your computer.");
        selectedLibraryId=selectedLibraryId??library.Entries.FirstOrDefault()?.Id;
        Visibility Shown(AppPage target)=>target==page?Visibility.Visible:Visibility.Collapsed;
        home.Visibility=Shown(AppPage.Home);libraryPage.Visibility=Shown(AppPage.Library);
        // Home and Library scrollers stay visible (only their content is hidden), so focus can move into them at once.
        homeScroll.IsHitTestVisible=page==AppPage.Home;libraryScroll.IsHitTestVisible=page==AppPage.Library;
        // The settings panel is rebuilt on every visit, so it always shows the saved values.
        if(page==AppPage.Settings&&(changed||settingsHost.Child is null))settingsHost.Child=new SettingsView(preferences,ApplyPreferences,()=>CheckUpdatesAsync(true),library);
        settingsHost.Visibility=Shown(AppPage.Settings);helpScroll.Visibility=Shown(AppPage.Help);
        foreach(var (button,target) in new[]{(homeButton,AppPage.Home),(libraryButton,AppPage.Library),(settingsButton,AppPage.Settings),(helpButton,AppPage.Help)})button.SetResourceReference(Control.BackgroundProperty,target==page?"AccentSoft":"Surface");
        appearance.SelectedItem=char.ToUpper(library.Appearance[0])+library.Appearance[1..];
        if(page==AppPage.Library)RenderLibrary();
        // Pages built or first shown after the window loaded miss the window-wide translation pass, so run it for the page itself.
        if(IsLoaded)Dispatcher.BeginInvoke(()=>L10n.Attach(page switch{AppPage.Library=>libraryPage,AppPage.Settings=>settingsHost,AppPage.Help=>helpPage,_=>home}),System.Windows.Threading.DispatcherPriority.Loaded);
        if(changed&&IsLoaded)Theme.Entrance(page switch{AppPage.Library=>libraryPage,AppPage.Settings=>settingsHost,AppPage.Help=>helpPage,_=>home});
    }
    /// The setup button is only needed until both tool groups are installed; Tools on Home stays available.
    private void UpdateToolsButton(){toolsSetupButton.Visibility=tools.Ready&&officeRuntime.Executable is not null?Visibility.Collapsed:Visibility.Visible;L10n.Bind(toolsSetupButton,ContentControl.ContentProperty,"Finish setup");}
    private void BuildHelpPage()
    {
        helpPage.Children.Add(Text("Help",30));helpPage.Children.Add(Text("How G-Tool works, in a few short steps.",14,"Muted"));
        void Section(string title,params string[] lines)
        {
            var body=new StackPanel{Margin=new(6,8,6,4)};foreach(var line in lines){var paragraph=Text(line,13);paragraph.Margin=new(0,4,0,6);paragraph.VerticalAlignment=VerticalAlignment.Top;body.Children.Add(paragraph);}
            var expander=new Expander{Header=title,Content=body,IsExpanded=helpPage.Children.Count==2};L10n.Bind(expander,HeaderedContentControl.HeaderProperty,title);helpHeadings.Add(expander);expander.SetResourceReference(Control.ForegroundProperty,"Ink");var card=Card(expander);card.Margin=new(0,12,0,0);helpPage.Children.Add(card);
        }
        Section("Getting started","Add files by dragging them onto this window, with Choose files (Ctrl+O) or with Paste (Ctrl+V). Paste also accepts text, an image or a link.","On first start G-Tool offers to download its tools: media tools (FFmpeg, yt-dlp, Deno) for video, audio and web downloads, and office tools (LibreOffice) for Office documents. You can install them later from Tools at the bottom of Home.");
        Section("Converting","Select files, choose Open wheel (Ctrl+Space or Enter) and pick a format.","Images are converted straight away. Video, audio and documents show their progress at the bottom of the screen with a Cancel button, and a short notice when they are done.","The original file goes to the Recycle Bin, so you can restore it. On USB and network drives the original stays where it is.","In Settings you can choose Keep the original file when converting, or Show settings before converting to adjust quality first.","While dragging files anywhere in Windows, hold the key chosen in Settings to open the wheel without opening G-Tool first.");
        Section("Editing tools","The wheel also has tools such as crop, annotate, redact, resize, trim and merge. They open an editor, and your result is saved as a new copy.","In video and audio editors: Space plays or pauses, ←/→ move one second, Shift+←/→ move one frame, I and O set the start and the end.");
        Section("Download from the web","Paste a public video, audio or image page link into Download a link, choose the format, then Download. Ctrl+Shift+D uses a link you copied.","Links to your own network, such as a router or a NAS, are not accepted. Downloaded files keep Windows' internet mark, so Windows can warn you before they open.");
        Section("Library and the file buttons","Library lists the files G-Tool made or you pasted. The files stay where they were saved; Library only keeps the list.","◉  Preview: shows the file inside G-Tool.","↖  Use in G-Tool: selects these files again on Home, ready for another tool.","▱  Show in Explorer: opens the folder with the file selected.","A greyed-out button means the file was moved or deleted. In Library, Space previews and Enter reuses the selected file, and Ctrl+F searches.");
        Section("Keyboard shortcuts",ShortcutHelp);
        Section("Updates and privacy","Files you convert are processed on this computer. G-Tool goes online to check for and download updates and tools, and to download the links you choose.","When a new version is out, Windows shows a notice and an update button appears on Home. Install it with one click: the download is checked against G-Tool's signature, and the old version is removed once the new one starts.","Automatic update checks can be turned off in Settings, under Updates.","G-Tool checks for updates when it opens and every hour while it runs. To get notices on time, turn on Launch at login in Settings, under General.");
        Section("If something goes wrong","If Windows says it protected your PC, choose More info, then Run anyway. G-Tool does not have a code signing certificate yet.","Keep G-Tool in a folder you can write to, such as Documents. Program Files or the top level of a drive can stop updates from installing.","If a tool does not download, check your internet connection and choose Set up media tools or Set up office tools again under Tools on Home.");
    }
}
