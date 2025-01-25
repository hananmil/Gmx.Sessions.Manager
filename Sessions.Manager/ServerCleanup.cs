
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Sessions.Manager.Services;
using StackExchange.Redis;

namespace Sessions.Manager
{
    public class ServerCleanup : IHostedService
    {
        private readonly Configuration _config;
        private readonly ServerAddressProvider _addressProvider;
        private readonly ILogger<ServerCleanup> _logger;
        private RedisProxyService _redis;
        private LocalSessionRepository _repository;
        private readonly RemoteSessionProxy _remote;
        private readonly CancellationTokenSource _completionSource = new CancellationTokenSource();
        public ServerCleanup(
            Configuration configuration,
            ILogger<ServerCleanup> logger,
            RedisProxyService redis,
            LocalSessionRepository repository,
            RemoteSessionProxy remoteSessionProvider,
            ServerAddressProvider addressProvider)
        {
            _config = configuration;
            _addressProvider = addressProvider;
            _logger = logger;
            _redis = redis;
            _repository = repository;
            _remote = remoteSessionProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                while (!_completionSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(5000, cancellationToken);
                    // Cleanup expired sessions. We don't rmove sessions from redis, only from local storage.
                    // Redis will remove them when they expire.
                    foreach (var session in _repository.ListSessions(true))
                    {
                        _repository.Delete(session);
                        _logger.LogDebug("Session {session} expired and was removed.", session);
                    }
                }
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Migration of sessions to other servers on shutdown.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping server. Running cleanup.");
            await _completionSource.CancelAsync();

            var liveServers = await _redis.GetServersUris();
            liveServers = liveServers.Where(s => s != _addressProvider.Uri).ToArray();
            if (!liveServers.Any())
            {
                _logger.LogError("Shutdown migration failed. No live servers found.");
                return;
            }
            int serverIndex = 0;

            // Distribute sessions to other live servers on shutdown
            var localSessions = _repository.ListSessions();
            Parallel.ForEach(localSessions, async session =>
            {
                serverIndex = (serverIndex + 1) % liveServers.Length;
                var server = liveServers[serverIndex];
                var isRemote = await _redis.IsSessionRemote(session);
                if (!isRemote.HasValue || isRemote.Value)
                {
                    _logger.LogDebug("Session {session} is remote or expired. Skipping.", session);
                    _repository.Delete(session);
                }
                else
                {
                    using (var stream = await _repository.GetSession(session))
                    {
                        _logger.LogDebug("Migrating session {session} to {server}.", session, server);
                        if (stream != null)
                        {
                            await _remote.SetSession(server, session, stream);
                            _repository.Delete(session);
                        }
                    }
                }
            });

            _logger.LogInformation("Cleanup complete.");

        }

    }
}
