using System.Text.Json;
using System.Text.RegularExpressions;
namespace GTool.Core;

/// An update moves the old app to "G-Tool.backup-id" and leaves its work folder ".gtool-update-id" beside the
/// app, because the installer runs from there and cannot delete itself. The new version removes them once it has
/// started. Only folders with exactly these generated names, next to this app, are ever selected.
public static partial class UpdateLeftovers
{
    private const string StagePrefix=".gtool-update-";
    private const string BackupMarker=".backup-";
    private static readonly TimeSpan AbandonedAfter=TimeSpan.FromDays(1);
    [GeneratedRegex("^[0-9a-f]{32}$")] private static partial Regex GeneratedId();

    public static IReadOnlyList<string> Find(string target,DateTime utcNow)
    {
        target=Path.TrimEndingDirectorySeparator(Path.GetFullPath(target));
        var parent=Path.GetDirectoryName(target);var name=Path.GetFileName(target);
        if(string.IsNullOrEmpty(parent)||string.IsNullOrEmpty(name))return [];
        var found=new List<string>();
        foreach(var folder in Directory.EnumerateDirectories(parent))
        {
            if((File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0)continue;
            var folderName=Path.GetFileName(folder);
            if(HasGeneratedId(folderName,name+BackupMarker)||(HasGeneratedId(folderName,StagePrefix)&&StageIsFinished(folder,target,utcNow)))found.Add(folder);
        }
        return found;
    }

    /// Removes what it can and returns the folders that remain, such as an installer that is still closing.
    public static IReadOnlyList<string> Remove(IEnumerable<string> folders)
    {
        var remaining=new List<string>();
        foreach(var folder in folders)
        {
            try{if(Directory.Exists(folder))Directory.Delete(folder,true);}
            catch(Exception error)when(error is IOException or UnauthorizedAccessException){remaining.Add(folder);}
        }
        return remaining;
    }

    private static bool HasGeneratedId(string folderName,string prefix)=>folderName.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)&&GeneratedId().IsMatch(folderName[prefix.Length..]);

    private static bool StageIsFinished(string stage,string target,DateTime utcNow)
    {
        var abandoned=utcNow-Directory.GetLastWriteTimeUtc(stage)>AbandonedAfter;
        var request=Path.Combine(stage,"request.json");
        if(!File.Exists(request))return abandoned;
        try
        {
            using var json=JsonDocument.Parse(File.ReadAllBytes(request));
            var requested=json.RootElement.GetProperty("Target").GetString();
            if(requested is null||!string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(requested)),target,StringComparison.OrdinalIgnoreCase))return false;
        }
        catch(Exception error)when(error is JsonException or KeyNotFoundException or InvalidOperationException or ArgumentException or NotSupportedException){return abandoned;}
        // The installer moves the payload into place, so a payload still here means it has not finished.
        return !Directory.Exists(Path.Combine(stage,"payload"))||abandoned;
    }
}
