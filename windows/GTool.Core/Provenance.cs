namespace GTool.Core;

/// Keeps Windows' internet-origin mark (the Zone.Identifier stream). SmartScreen uses it to warn before an
/// unknown program runs, so files unpacked from a downloaded archive or downloaded here must carry it too.
public static class Provenance
{
    private const string Stream=":Zone.Identifier";
    private const int MaximumLength=8192;
    public static string? Read(string file)
    {
        if(!OperatingSystem.IsWindows())return null;
        try
        {
            using var source=new FileStream(file+Stream,FileMode.Open,FileAccess.Read,FileShare.Read);if(source.Length is 0 or >MaximumLength)return null;
            using var reader=new StreamReader(source);return reader.ReadToEnd();
        }
        catch(Exception error)when(IsExpected(error)){return null;}
    }
    /// Passes the mark of `source`, if it has one, to every output file. Folders are marked file by file.
    public static void Inherit(string source,IEnumerable<string> outputs)
    {
        if(Read(source) is {} mark)Apply(mark,outputs);
    }
    public static void MarkDownloaded(Uri origin,IEnumerable<string> outputs)=>Apply("[ZoneTransfer]\r\nZoneId=3\r\nHostUrl="+origin.GetLeftPart(UriPartial.Path)+"\r\n",outputs);
    private static void Apply(string mark,IEnumerable<string> outputs)
    {
        if(!OperatingSystem.IsWindows())return;
        foreach(var output in outputs)
            foreach(var file in Directory.Exists(output)?Directory.EnumerateFiles(output,"*",SearchOption.AllDirectories):[output])
            {
                // Drives without alternate streams (FAT, some network shares) cannot hold the mark; the file itself is still fine.
                try{File.WriteAllText(file+Stream,mark);}catch(Exception error)when(IsExpected(error)){}
            }
    }
    private static bool IsExpected(Exception error)=>error is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException;
}
