using System.IO.Compression;
using System.Net;
using GTool.Core;

/// Regression checks for the G-Tool security review: internet-origin marks and public-only links.
internal static class SecurityChecks
{
    internal static void Run(Action<bool,string> check,string root)
    {
        foreach(var blocked in new[]{"http://192.168.1.1/a.png","http://10.0.0.5/","http://172.16.0.1/","http://172.31.255.255/","http://169.254.169.254/latest","http://100.64.0.1/","http://0.0.0.0/","http://127.0.0.1/","http://localhost/","http://router/","http://printer.local/","http://nas.lan/","http://[::1]/","http://[fe80::1]/","http://[fd00::1]/","http://[::ffff:192.168.1.1]/","ftp://example.com/a.png","file:///C:/a.png"})
        {
            var rejected=false;try{WebDownload.Validate(blocked);}catch(ArgumentException){rejected=true;}
            check(rejected,"A local or unsupported link was accepted: "+blocked);
        }
        foreach(var allowed in new[]{"https://example.com/a.png","https://www.youtube.com/watch?v=abcdefghijk","http://8.8.8.8/a.png","https://172.32.0.1/a.png","https://[2606:4700:4700::1111]/a.png"})
        {
            var accepted=true;try{WebDownload.Validate(allowed);}catch(ArgumentException){accepted=false;}
            check(accepted,"A public link was rejected: "+allowed);
        }
        check(!PublicNetwork.IsPublic(IPAddress.Parse("::ffff:10.0.0.1"))&&PublicNetwork.IsPublic(IPAddress.Parse("1.1.1.1")),"Address classification regressed");
        if(!OperatingSystem.IsWindows())return;

        ToolVersionChecks(check,root);
        UpdateLeftoverChecks(check,root);
        var folder=Path.Combine(root,"Provenance");Directory.CreateDirectory(folder);
        var marked=Path.Combine(folder,"Downloaded.zip");
        using(var zip=ZipFile.Open(marked,ZipArchiveMode.Create)){using var entry=new StreamWriter(zip.CreateEntry("Inner/program.txt").Open());entry.Write("content");}
        var plain=Path.Combine(folder,"Local.zip");File.Copy(marked,plain);
        Provenance.MarkDownloaded(new("https://example.com/Downloaded.zip?secret=1"),[marked]);
        var mark=Provenance.Read(marked);
        check(mark?.Contains("ZoneId=3",StringComparison.Ordinal)==true&&mark.Contains("https://example.com/Downloaded.zip",StringComparison.Ordinal)&&!mark.Contains("secret",StringComparison.Ordinal),"A download was not marked correctly");
        var extracted=ArchiveEngine.Extract(marked,Path.Combine(folder,"FromMarked"))[0];
        check(Provenance.Read(Path.Combine(extracted,"Inner","program.txt"))==mark,"Files from a downloaded archive lost the internet mark");
        var converted=ArchiveEngine.Convert(marked,"tar",Path.Combine(folder,"ConvertedMarked"))[0];
        check(Provenance.Read(converted)==mark,"A converted downloaded archive lost the internet mark");
        var local=ArchiveEngine.Extract(plain,Path.Combine(folder,"FromPlain"))[0];
        check(Provenance.Read(Path.Combine(local,"Inner","program.txt")) is null,"A local archive's files were marked as downloaded");
    }
    private static void UpdateLeftoverChecks(Action<bool,string> check,string root)
    {
        const string id="0123456789abcdef0123456789abcdef";const string other="fedcba9876543210fedcba9876543210";
        var parent=Path.Combine(root,"Update leftovers");var target=Path.Combine(parent,"G-Tool");Directory.CreateDirectory(Path.Combine(target,"data"));File.WriteAllText(Path.Combine(target,"G-Tool.exe"),"new");
        string Folder(string name){var path=Path.Combine(parent,name);Directory.CreateDirectory(path);File.WriteAllText(Path.Combine(path,"file.txt"),"sample");return path;}
        string Stage(string name,string requestTarget,bool payload){var path=Folder(name);File.WriteAllText(Path.Combine(path,"request.json"),System.Text.Json.JsonSerializer.Serialize(new{ProcessId=1,Target=requestTarget,Payload=Path.Combine(path,"payload"),Stage=path}));if(payload)Directory.CreateDirectory(Path.Combine(path,"payload"));return path;}
        var backup=Folder("G-Tool.backup-"+id);var finished=Stage(".gtool-update-"+id,target,false);
        var running=Stage(".gtool-update-"+other,target,true);
        var foreign=Stage(".gtool-update-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",Path.Combine(parent,"Another copy"),false);
        var noRequest=Folder(".gtool-update-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        var kept=new[]{Folder("G-Tool.backup-mine"),Folder("G-Tool.backup-"+id+"x"),Folder("Other.backup-"+id),Folder(".gtool-update-notes"),Folder("G-Tool backup"),Folder("Documents"),running,foreign,noRequest};
        var linked=Path.Combine(parent,"G-Tool.backup-cccccccccccccccccccccccccccccccc");var linkTarget=Folder("Linked data");Directory.CreateSymbolicLink(linked,linkTarget);
        var now=DateTime.UtcNow;var selected=UpdateLeftovers.Find(target+Path.DirectorySeparatorChar,now);
        check(selected.Count==2&&selected.Contains(backup)&&selected.Contains(finished),"Update cleanup chose the wrong folders: "+string.Join(", ",selected.Select(Path.GetFileName)));
        check(!selected.Contains(target),"Update cleanup selected the running app");
        foreach(var folder in kept)check(!selected.Contains(folder),"Update cleanup selected an unrelated folder: "+Path.GetFileName(folder));
        check(!selected.Contains(linked),"Update cleanup followed a linked folder");
        var later=UpdateLeftovers.Find(target,now.AddDays(2));
        check(later.Contains(running)&&later.Contains(noRequest)&&!later.Contains(foreign),"Abandoned update folders were handled differently than expected");
        var remaining=UpdateLeftovers.Remove(selected);
        check(remaining.Count==0&&!Directory.Exists(backup)&&!Directory.Exists(finished),"Update leftovers were not removed");
        check(File.Exists(Path.Combine(target,"G-Tool.exe"))&&kept.All(Directory.Exists)&&File.Exists(Path.Combine(linkTarget,"file.txt")),"Update cleanup removed something it must keep");
        check(UpdateLeftovers.Find(Path.GetPathRoot(parent)!,now).Count==0,"Update cleanup ran for a drive root");
        Directory.Delete(linked);
    }
    private static void ToolVersionChecks(Action<bool,string> check,string root)
    {
        var folder=Path.Combine(root,"Tool versions");Directory.CreateDirectory(folder);var tools=new Toolchain(folder);
        check(!tools.Outdated,"An empty tool folder was reported as outdated");
        File.WriteAllBytes(Path.Combine(folder,OperatingSystem.IsWindows()?"ffmpeg.exe":"ffmpeg"),[1]);
        check(tools.Outdated,"Tools without a version record were not offered an update");
        File.WriteAllText(Path.Combine(folder,"packages.txt"),"older");check(tools.Outdated,"Older tools were not offered an update");
        File.WriteAllText(Path.Combine(folder,"packages.txt"),Toolchain.PackagesFingerprint);check(!tools.Outdated,"Current tools were offered an update");
        var key=new Org.BouncyCastle.Crypto.Generators.Ed25519KeyPairGenerator();key.Init(new Org.BouncyCastle.Crypto.Parameters.Ed25519KeyGenerationParameters(new Org.BouncyCastle.Security.SecureRandom()));var pair=key.GenerateKeyPair();
        var archive=Path.Combine(folder,"signed.bin");File.WriteAllBytes(archive,[1,2,3,4]);
        var signer=new Org.BouncyCastle.Crypto.Signers.Ed25519Signer();signer.Init(true,pair.Private);signer.BlockUpdate([1,2,3,4],0,4);var signature=Convert.ToBase64String(signer.GenerateSignature());var publicKey=((Org.BouncyCastle.Crypto.Parameters.Ed25519PublicKeyParameters)pair.Public).GetEncoded();
        check(PublicUpdates.SignatureMatchesAsync(archive,signature,publicKey).GetAwaiter().GetResult(),"A correctly signed update was rejected");
        File.WriteAllBytes(archive,[1,2,3,5]);check(!PublicUpdates.SignatureMatchesAsync(archive,signature,publicKey).GetAwaiter().GetResult(),"A changed update passed its signature check");
    }
}
