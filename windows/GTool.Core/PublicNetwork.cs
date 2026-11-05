using System.Net;
using System.Net.Http;
using System.Net.Sockets;
namespace GTool.Core;

/// Links pasted into the app, and the redirects they lead to, may only reach the public internet.
/// Otherwise a crafted link could make this computer send requests to its router or other local devices.
internal static class PublicNetwork
{
    private static readonly string[] LocalSuffixes=[".local",".localhost",".internal",".lan",".home.arpa"];
    internal static bool IsPublicHost(Uri url)
    {
        if(url.IsLoopback)return false;
        var host=url.IdnHost.TrimEnd('.');
        if(IPAddress.TryParse(url.HostNameType==UriHostNameType.IPv6?url.DnsSafeHost:host,out var address))return IsPublic(address);
        return host.Contains('.')&&!LocalSuffixes.Any(s=>host.EndsWith(s,StringComparison.OrdinalIgnoreCase));
    }
    internal static bool IsPublic(IPAddress address)
    {
        if(address.IsIPv4MappedToIPv6)address=address.MapToIPv4();
        if(IPAddress.IsLoopback(address))return false;
        if(address.AddressFamily==AddressFamily.InterNetworkV6)
        {
            var bytes=address.GetAddressBytes();
            return !(address.IsIPv6LinkLocal||address.IsIPv6SiteLocal||address.IsIPv6Multicast||address.Equals(IPAddress.IPv6None)||(bytes[0]&0xFE)==0xFC);
        }
        var v4=address.GetAddressBytes();
        return !(v4[0] is 0 or 10 or 127||v4[0]>=224||v4[0]==100&&v4[1] is >=64 and <=127||v4[0]==169&&v4[1]==254||v4[0]==172&&v4[1] is >=16 and <=31||v4[0]==192&&v4[1]==168||v4[0]==192&&v4[1]==0&&v4[2]==0||v4[0]==198&&v4[1] is 18 or 19);
    }
    /// Checks where a name currently points before a tool such as yt-dlp is given the link.
    /// A name that cannot be resolved is left for the tool to report, so being offline gives the normal error.
    internal static async Task EnsurePublicAsync(Uri url,CancellationToken token)
    {
        if(IPAddress.TryParse(url.DnsSafeHost,out _))return;
        IPAddress[] addresses;
        try{addresses=await Dns.GetHostAddressesAsync(url.DnsSafeHost,token);}catch(SocketException){return;}
        if(addresses.Any(a=>!IsPublic(a)))throw new ArgumentException("Paste a public http(s) media or page link.");
    }
    /// Same check at connection time, including redirects. A configured proxy is the user's own choice and is not checked.
    internal static SocketsHttpHandler Handler()=>new()
    {
        AllowAutoRedirect=false,
        ConnectCallback=async(context,token)=>
        {
            var host=context.DnsEndPoint.Host;var addresses=await Dns.GetHostAddressesAsync(host,token);
            var proxied=!string.Equals(context.InitialRequestMessage.RequestUri?.IdnHost,host,StringComparison.OrdinalIgnoreCase)&&!string.Equals(context.InitialRequestMessage.RequestUri?.DnsSafeHost,host,StringComparison.OrdinalIgnoreCase);
            if(!proxied&&addresses.Any(a=>!IsPublic(a)))throw new HttpRequestException("Paste a public http(s) media or page link.");
            var socket=new Socket(SocketType.Stream,ProtocolType.Tcp){NoDelay=true};
            try{await socket.ConnectAsync(addresses,context.DnsEndPoint.Port,token);return new NetworkStream(socket,true);}
            catch{socket.Dispose();throw;}
        },
    };
}
