using System.Net;
using Sessions.Manager.Controllers.DTO;
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

        public RedisProxyService( IConnectionMultiplexer connectionMultiplexer, Configuration configuration, ILogger<RedisProxyService> logger, ServerAddressProvider addressProvider )
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

        public async Task<ServerStatus[]> GetServersDetails()
        {
            var serversHash = await database.HashGetAllAsync(GetServersKey());
            var servers = serversHash.Select(e => new ServerStatus { Ip = e.Name.ToString(), LastUpdate = DateTime.UtcNow -  new DateTime(long.Parse(e.Value.ToString())) }).ToArray();
            foreach ( var server in servers )
            {
                server.Alive = server.LastUpdate <= _keepAliveExpiry;
            }
            return servers;
        }

        public async Task<string[]> GetServersUris()
        {
            var servers = await GetServersDetails();
            return servers.Where(s => s.Alive).Select(x => x.Ip).ToArray();
        }

        public async Task SetSession( string sessionId, TimeSpan? expiry = null )
        {
            _logger.LogDebug("Setting session {sessionId} on server {server} expiry {expiry}.", sessionId, _serverUri.Uri, expiry??_sessionExpiry);
            await database.StringSetAsync(GetSessionKey(sessionId), _serverUri.Uri, expiry??_sessionExpiry);
        }

        public async Task<string?> GetSessionServer( string sessionId )
        {
            var result = await database.StringGetAsync(GetSessionKey(sessionId));
            if ( result.IsNull )
            {
                _logger.LogDebug("Session {sessionId} not found in redis.", sessionId);
            }
            else
            {
                _logger.LogDebug("Session {sessionId} is on server {server}.", sessionId, result);
            }
            return result;
        }

        public async Task<bool?> IsSessionRemote( string sessionId )
        {
            var serverName = await GetSessionServer(sessionId);
            if ( serverName == null )
                return null;
            return serverName != _serverUri.Uri;
        }


        private string GetSessionKey( string sessionId ) => $"{_clusterName}:Sessions:{sessionId}";
        private string GetServersKey() => $"{_clusterName}:Servers";

        internal async Task<IList<Session>> ListSessions()
        {
            var result = new List<Session>();
            var sessionKeys = this.server.KeysAsync(pattern: $"{_clusterName}:Sessions:*");

            await foreach ( var key in sessionKeys )
            {
                var server = await database.StringGetAsync(key);
                var expiry = await database.KeyTimeToLiveAsync(key);
                result.Add(new Session
                {
                    Id = key.ToString().Replace($"{_clusterName}:Sessions:",""),
                    Server = server.ToString(),
                    Expiry = expiry,
                });
            }
            return result;
        }
    }
}
