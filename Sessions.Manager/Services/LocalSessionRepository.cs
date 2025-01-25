
using System.Net.Sockets;
using System.Net;
using StackExchange.Redis;
using Sessions.Manager.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Sessions.Manager.Services
{
    public class LocalSessionRepository : ReadInterface, WriteInterface
    {
        private readonly IMemoryCache _memCache;
        private readonly ILogger<LocalSessionRepository> _logger;
        private readonly RedisProxyService _redis;
        private readonly string _localPath;
        private readonly TimeSpan _sessionTTL;

        public LocalSessionRepository(Configuration configuration,
                RedisProxyService redis,
                ILogger<LocalSessionRepository> logger,
                IMemoryCache memoryCache)
        {
            _memCache = memoryCache;
            _logger = logger;
            _redis = redis;
            _localPath = configuration.LocalPath;
            _sessionTTL = TimeSpan.FromMinutes(configuration.SessionTimeoutMinutes);
            if (!Directory.Exists(_localPath))
            {
                _logger.LogInformation($"Local directory {_localPath} missing. Creating.");
                Directory.CreateDirectory(_localPath);
            }
        }

        public Task<Stream?> GetSession(string sessionId)
        {
            var path = getSessionPath(sessionId);
            try
            {
                var fileStream = File.OpenRead(path);
                return Task.FromResult((Stream?)fileStream);
            }
            catch (FileNotFoundException)
            {
                return Task.FromResult((Stream?)null);
            }
        }

        public async Task UpdateSession(string sessionId, Stream streamReader, TimeSpan? expiry = null)
        {
            using (var fileStream = File.Create(getSessionPath(sessionId)))
            {
                var redisTask = _redis.SetSession(sessionId, expiry);
                await streamReader.CopyToAsync(fileStream);
                await redisTask;
                _memCache.Set<Object>(sessionId, new object(), new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry??_sessionTTL });
            }
        }

        private string getSessionPath(string sessionId)
        {
            return Path.Combine(_localPath, fixSessionName(sessionId));
        }
        private string fixSessionName(string sessionId)
        {
            return sessionId.Replace("/", "_");
        }

        internal IList<string> ListSessions(bool onlyExpired = false)
        {
            var allSessions = Directory.GetFiles(_localPath)
                    .Select(x => x.Split(Path.DirectorySeparatorChar).Last().Replace("_", "/"))
                    .ToList();
            if (onlyExpired)
            {
                var expired = new List<string>();
                for (int i = 0; i < allSessions.Count; i++)
                {
                    if (_memCache.Get<Object>(allSessions[i]) == null)
                    {
                        expired.Add(allSessions[i]);
                    }
                }
                return expired;
            }

            return allSessions;
        }

        internal void Delete(string session)
        {
            var path = getSessionPath(session);
            try
            {
                File.Delete(path);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to delete session file. "+path);
            }
        }
    }
}
