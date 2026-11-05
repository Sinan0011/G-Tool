using System.Runtime.InteropServices;
namespace GTool.Windows;

/// Moves a file to the Recycle Bin so it can still be restored. Drives without a Recycle Bin (USB sticks,
/// network shares) would delete the file for good, so on those the file is left where it is.
internal static class RecycleBin
{
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    private struct FileOperation
    {
        public IntPtr Window;public uint Function;public string From;public string? To;public ushort Flags;
        [MarshalAs(UnmanagedType.Bool)]public bool Aborted;public IntPtr Mappings;public string? Title;
    }
    private const uint Delete=3;
    private const ushort AllowUndo=0x40,NoConfirmation=0x10,Silent=0x4,NoErrorUi=0x400;
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)]private static extern int SHFileOperation(ref FileOperation operation);
    internal static bool Available(string path)
    {
        try{return new DriveInfo(Path.GetFullPath(path)).DriveType==DriveType.Fixed;}
        catch(Exception error)when(error is ArgumentException or IOException or UnauthorizedAccessException){return false;}
    }
    internal static bool TryMove(string path)
    {
        if(!File.Exists(path)||!Available(path))return false;
        var operation=new FileOperation{Function=Delete,From=Path.GetFullPath(path)+"\0\0",Flags=(ushort)(AllowUndo|NoConfirmation|Silent|NoErrorUi)};
        return SHFileOperation(ref operation)==0&&!operation.Aborted&&!File.Exists(path);
    }
}
