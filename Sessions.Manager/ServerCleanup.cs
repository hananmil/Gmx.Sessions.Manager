
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
        private readonly RemoteSessionProvider _remote;
        private readonly TaskCompletionSource _taskCompletionSource = new TaskCompletionSource();
        public ServerCleanup(
            Configuration configuration,
            ILogger<ServerCleanup> logger,
            RedisProxyService redis,
            LocalSessionRepository repository,
            RemoteSessionProvider remoteSessionProvider,
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
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(5000, cancellationToken);
                    // Cleanup expired sessions. We don't rmove sessions from redis, only from local storage.
                    // Redis will remove them when they expire.
                    foreach (var session in _repository.ListSessions(true))
                    {
                        _repository.Delete(session);
                        _logger.LogInformation("Session {session} expired and was removed.", session);
                    }
                }
            });
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping server. Running cleanup.");
            _taskCompletionSource.SetCanceled();
            var liveServers = await _redis.GetServersUris();
            liveServers = liveServers.Where(s => s != _addressProvider.Uri).ToArray();
            int serverIndex = 0;

            // Distribute sessions to other live servers on shutdown
            var localSessions = _repository.ListSessions();
            Parallel.ForEach(localSessions, async session =>
            {
                serverIndex = (serverIndex + 1) % liveServers.Length;
                var server = liveServers[serverIndex];
                if (await _redis.IsSessionRemote(session))
                {
                    _logger.LogInformation("Session {session} is already remote. Skipping.", session);
                    _repository.Delete(session);

                }
                else
                {
                    using (var ms = await _repository.GetSession(session))
                    {
                        _logger.LogInformation("Migrating session {session} to {server}.", session, server);
                        if (ms != null)
                        {
                            await _remote.SetSession(server, session, ms);
                            _repository.Delete(session);
                        }
                    }
                }
            });

            _logger.LogInformation("Cleanup complete.");

        }

    }
}
