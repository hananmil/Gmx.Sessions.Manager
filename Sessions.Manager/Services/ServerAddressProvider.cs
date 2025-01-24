using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting.Server.Features;
using StackExchange.Redis;

namespace Sessions.Manager.Services
{
    public class ServerAddressProvider
    {
        private string? _uri;
        private readonly Microsoft.AspNetCore.Hosting.Server.IServer _server;

        public RedisValue Uri => _uri??(_uri = GetUri());

        public ServerAddressProvider(Microsoft.AspNetCore.Hosting.Server.IServer server)
        {
            _server = server;            
        }
        private string GetUri()
        {
            var addresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses;
            var port = addresses?.Select(a => new Uri(a)).FirstOrDefault()?.Port;
            if (port == null) throw new InvalidOperationException("Server address not found.");

            var _lanIp = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork).ToString();
            var uri = $"{_lanIp}:{port}";
            return uri;
        }
    }
}
