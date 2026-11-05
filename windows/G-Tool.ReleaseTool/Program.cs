using System.IO.Compression;
using System.Text.Json;
using GTool.Core;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

// keygen <private key file>
//   Creates the signing key if it does not exist and prints the public key for PublicUpdates.cs.
// release <published folder> <private key file> <version> <output folder>
//   Writes G-Tool-Windows-x64.zip and the signed windows-update.json for a GitHub release tagged v<version>.
const string ArchiveName="G-Tool-Windows-x64.zip";
if(args.Length==2&&args[0]=="keygen")return KeyGen(args[1]);
if(args.Length==5&&args[0]=="release")return Release(args[1],args[2],args[3],args[4]);
Console.Error.WriteLine("Usage: keygen <key file> | release <published folder> <key file> <version> <output folder>");
return 2;

static int KeyGen(string file)
{
    if(File.Exists(file))
    {
        Console.WriteLine(Convert.ToBase64String(new Ed25519PrivateKeyParameters(Convert.FromBase64String(File.ReadAllText(file).Trim()),0).GeneratePublicKey().GetEncoded()));
        return 0;
    }
    var generator=new Ed25519KeyPairGenerator();generator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));var pair=generator.GenerateKeyPair();
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
    File.WriteAllText(file,Convert.ToBase64String(((Ed25519PrivateKeyParameters)pair.Private).GetEncoded()));
    Console.WriteLine(Convert.ToBase64String(((Ed25519PublicKeyParameters)pair.Public).GetEncoded()));
    return 0;
}

static int Release(string published,string keyFile,string version,string output)
{
    if(!PublicUpdates.Configured){Console.Error.WriteLine("Set PublicUpdates.Repository first.");return 3;}
    if(!Version.TryParse(version,out _)){Console.Error.WriteLine("Use a version such as 1.2.0.");return 4;}
    if(!File.Exists(Path.Combine(published,"G-Tool.exe"))){Console.Error.WriteLine("The published folder has no G-Tool.exe.");return 5;}
    var key=new Ed25519PrivateKeyParameters(Convert.FromBase64String(File.ReadAllText(keyFile).Trim()),0);
    if(Convert.ToBase64String(key.GeneratePublicKey().GetEncoded())!=PublicUpdates.PublicKey){Console.Error.WriteLine("This signing key does not match the public key built into the app.");return 6;}
    Directory.CreateDirectory(output);var archive=Path.Combine(output,ArchiveName);File.Delete(archive);
    ZipFile.CreateFromDirectory(published,archive,CompressionLevel.Optimal,false);
    var signer=new Ed25519Signer();signer.Init(true,key);
    using(var source=File.OpenRead(archive)){var buffer=new byte[81920];int count;while((count=source.Read(buffer))>0)signer.BlockUpdate(buffer,0,count);}
    var update=new WindowsUpdate(version,$"https://github.com/{PublicUpdates.Repository}/releases/download/v{version}/{ArchiveName}",new FileInfo(archive).Length,Convert.ToBase64String(signer.GenerateSignature()));
    PublicUpdates.Validate(update);
    File.WriteAllText(Path.Combine(output,"windows-update.json"),JsonSerializer.Serialize(update,new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine(archive);
    return 0;
}
