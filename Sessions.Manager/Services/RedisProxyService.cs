using System.Net;
using StackExchange.Redis;

namespace Sessions.Manager.Services
{
    public class RedisProxyService
    {
        private readonly ILogger<RedisProxyService> _logger;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly EndPoint[] _endpoints;
        private readonly string _clusterName;
        private readonly ServerAddressProvider _serverUri;
        private readonly TimeSpan _sessionExpiry;
        private readonly TimeSpan _keepAliveExpiry;

        private IServer server => _connectionMultiplexer.GetServer(_endpoints[0]);
        private IDatabaseAsync database => _connectionMultiplexer.GetDatabase();

        public RedisProxyService(IConnectionMultiplexer connectionMultiplexer, Configuration configuration,ILogger<RedisProxyService> logger, ServerAddressProvider addressProvider)
        {
            _logger = logger;
            _connectionMultiplexer = connectionMultiplexer;
            _endpoints = _connectionMultiplexer.GetEndPoints();
            _clusterName = configuration.ClusterPrefix;
            _serverUri = addressProvider;
            _sessionExpiry = TimeSpan.FromMinutes(configuration.SessionTimeoutMinutes);
            _keepAliveExpiry = TimeSpan.FromSeconds(configuration.LivelinessTimeoutSec);
        }

        public async Task MarkServerAlive()
        {
            await database.HashSetAsync(GetServersKey(), new[] { new HashEntry(_serverUri.Uri, DateTime.UtcNow.Ticks) });
        }

        public async Task MarkServerDead()
        {
            await database.HashDeleteAsync(GetServersKey(), _serverUri.Uri);
        }

        public async Task<dynamic[]> GetServersDetails()
        {
            var serversHash = await database.HashGetAllAsync(GetServersKey());
            return serversHash.Select(e => new { Ip = e.Name.ToString(), Expiry = DateTime.UtcNow -  new DateTime(long.Parse(e.Value.ToString())) }).ToArray();
        }

        public async Task<string[]> GetServersUris()
        {
            var servers = await GetServersDetails();
            return servers.Where(s => (TimeSpan)s.Expiry <= _keepAliveExpiry)
                          .Select(x => (string)x.Ip).ToArray();
        }

        public async Task SetSession(string sessionId)
        {
            await database.StringSetAsync(GetSessionKey(sessionId), _serverUri.Uri, _sessionExpiry);
        }

        public async Task<string?> GetSessionServer(string sessionId)
        {
            return await database.StringGetAsync(GetSessionKey(sessionId));
        }

        public async Task<bool> IsSessionRemote(string sessionId)
        {
            var serverName = await GetSessionServer(sessionId);
            return serverName != null && serverName != _serverUri.Uri;
        }

        public async Task RemoveSession(string sessiondId)
        {
            await database.KeyDeleteAsync(GetSessionKey(sessiondId));
        }

        private string GetSessionKey(string sessionId) => $"{_clusterName}:Sessions:{sessionId}";
        private string GetServersKey() => $"{_clusterName}:Servers";

        internal async Task<IList<dynamic>> ListSessions()
        {
            var result = new List<dynamic>();
            var sessionKeys = this.server.KeysAsync(pattern: $"{_clusterName}:Sessions:*");

            await foreach (var key in sessionKeys)
            {
                var server = await database.StringGetAsync(key);
                result.Add(new { SessionId = key.ToString(), Server = server.ToString() });
            }
            return result;

        }
    }
}
