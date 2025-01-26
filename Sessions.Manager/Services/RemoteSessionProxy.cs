using System.Net;
using System.Net.Sockets;
using Sessions.Manager.Interfaces;
using StackExchange.Redis;

namespace Sessions.Manager.Services
{
    public class RemoteSessionProxy : ReadInterface
    {
        private readonly Configuration _config;
        private readonly ILogger<RemoteSessionProxy> _logger;
        private readonly RedisProxyService _redis;
        private readonly HttpClient _httpClient;

        public RemoteSessionProxy(ILogger<RemoteSessionProxy> logger, HttpClient httpClient,
            RedisProxyService redis,
            Configuration configuration
            )
        {
            _config = configuration;
            _logger = logger;
            _redis = redis;
            _httpClient = httpClient;
        }

        public async Task<Stream?> GetSession(string sessionId)
        {
            sessionId = WebUtility.UrlEncode(sessionId);
            var remoteServer = await _redis.GetSessionServer(sessionId);
            if (string.IsNullOrWhiteSpace(remoteServer))
            {
                _logger.LogWarning("Session {sessionId} not found in redis.", sessionId);
                return null;
            }

            using (var response = await _httpClient.GetAsync($"http://{remoteServer}/session/{sessionId}"))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogWarning("Failed to get session {sessionId} from {remoteServer}. Status code: {statusCode}.", sessionId, remoteServer, response.StatusCode);
                    return null;
                }
                var memoryStream = new MemoryStream();
                await response.Content.CopyToAsync(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return memoryStream;
            }
        }

        internal async Task SetSession(string server, string session, Stream ms)
        {
            session = WebUtility.UrlEncode(session);
            using (var content = new StreamContent(ms))
            {
                using (var response = await _httpClient.PutAsync($"http://{server}/session/{session}", content))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        _logger.LogWarning("Failed to set session {session} on {server}. Status code: {statusCode}.", session, server, response.StatusCode);
                    }
                }
            }
        }
    }
}
