using System.Runtime.InteropServices;
using System.Text;
namespace GTool.Windows;

/// Puts a converted file's desktop icon where the original's icon was. Windows otherwise drops a new file into
/// the first free slot, which looks as if it vanished. This is a nicety: any failure simply leaves the icon there.
internal static class DesktopIcons
{
    [StructLayout(LayoutKind.Sequential)]internal struct Spot{public int X,Y;}
    [ComImport,Guid("6D5140C1-7436-11CE-8034-00AA006009FA"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IServiceProvider{void QueryService(ref Guid service,ref Guid iid,[MarshalAs(UnmanagedType.IUnknown)]out object result);}
    [ComImport,Guid("000214E2-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellBrowser
    {
        void GetWindow();void ContextSensitiveHelp();void InsertMenusSB();void SetMenuSB();void RemoveMenusSB();void SetStatusTextSB();void EnableModelessSB();void TranslateAcceleratorSB();void BrowseObject();void GetViewStateStream();void GetControlWindow();void SendControlMsg();
        void QueryActiveShellView([MarshalAs(UnmanagedType.IUnknown)]out object view);
    }
    [ComImport,Guid("CDE725B0-CCC9-4519-917E-325D72FAB4CE"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFolderView
    {
        void GetCurrentViewMode();void SetCurrentViewMode();void GetFolder();
        void Item(int index,out IntPtr item);
        void ItemCount(uint flags,out int count);
        void Items();void GetSelectionMarkedItem();void GetFocusedItem();
        void GetItemPosition(IntPtr item,out Spot spot);
        void GetSpacing();void GetDefaultSpacing();
        [PreserveSig]int GetAutoArrange();
        void SelectItem();
        void SelectAndPositionItems(uint count,[In,MarshalAs(UnmanagedType.LPArray,SizeParamIndex=0)]IntPtr[] items,[In,MarshalAs(UnmanagedType.LPArray,SizeParamIndex=0)]Spot[] spots,uint flags);
    }
    private const uint AllViewItems=2,PositionItem=0x80;
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool SHGetPathFromIDListW(IntPtr list,StringBuilder path);
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)]private static extern void SHChangeNotify(int eventId,uint flags,string item,IntPtr unused);
    /// Tells Explorer about a new file so the desktop shows it right away.
    internal static void Announce(string path){if(File.Exists(path))SHChangeNotify(0x2,0x1005,Path.GetFullPath(path),IntPtr.Zero);}
    internal static void AnnounceGone(string path)=>SHChangeNotify(0x4,0x1005,Path.GetFullPath(path),IntPtr.Zero);
    private static IFolderView? View()
    {
        var type=Type.GetTypeFromCLSID(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39"));if(type is null)return null;
        dynamic windows=Activator.CreateInstance(type)!;object location=0;object root=Type.Missing;
        var desktop=(object)windows.FindWindowSW(ref location,ref root,8,out int _,1);
        var service=new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837");var browserId=typeof(IShellBrowser).GUID;
        ((IServiceProvider)desktop).QueryService(ref service,ref browserId,out var browser);
        ((IShellBrowser)browser).QueryActiveShellView(out var view);
        return (IFolderView)view;
    }
    private static IntPtr Find(IFolderView view,string path)
    {
        view.ItemCount(AllViewItems,out var count);var name=new StringBuilder(32768);
        for(var n=0;n<count;n++)
        {
            view.Item(n,out var item);name.Clear();
            if(SHGetPathFromIDListW(item,name)&&string.Equals(name.ToString(),path,StringComparison.OrdinalIgnoreCase))return item;
            Marshal.FreeCoTaskMem(item);
        }
        return IntPtr.Zero;
    }
    private static bool IsOnDesktop(string path)=>string.Equals(Path.GetDirectoryName(Path.GetFullPath(path)),Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),StringComparison.OrdinalIgnoreCase);
    /// Where each file's icon sits now, or null for files that are not on the desktop. Auto-arrange leaves nothing to keep.
    internal static Spot?[] Positions(IReadOnlyList<string> files)
    {
        var spots=new Spot?[files.Count];
        try
        {
            if(!files.Any(IsOnDesktop)||View() is not {} view||view.GetAutoArrange()==0)return spots;
            for(var n=0;n<files.Count;n++)
            {
                if(!IsOnDesktop(files[n]))continue;var item=Find(view,Path.GetFullPath(files[n]));if(item==IntPtr.Zero)continue;
                try{view.GetItemPosition(item,out var spot);spots[n]=spot;}finally{Marshal.FreeCoTaskMem(item);}
            }
        }
        catch(Exception error)when(IsExpected(error)){}
        return spots;
    }
    /// Moves each output's icon to its saved spot. The desktop lists a new file a moment after it is created, so this waits briefly.
    internal static async Task PlaceAsync(Spot?[] spots,IReadOnlyList<string> outputs)
    {
        if(spots.Length!=outputs.Count||spots.All(s=>s is null))return;
        try
        {
            if(View() is not {} view)return;
            for(var n=0;n<outputs.Count;n++)
            {
                if(spots[n] is not {} spot||!IsOnDesktop(outputs[n]))continue;
                for(var attempt=0;attempt<100;attempt++)
                {
                    var item=Find(view,Path.GetFullPath(outputs[n]));
                    if(item!=IntPtr.Zero){try{view.SelectAndPositionItems(1,[item],[spot],PositionItem);}finally{Marshal.FreeCoTaskMem(item);}break;}
                    await Task.Delay(100);
                }
            }
        }
        catch(Exception error)when(IsExpected(error)){}
    }
    private static bool IsExpected(Exception error)=>error is COMException or InvalidCastException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException or ArgumentException or NullReferenceException;
}
