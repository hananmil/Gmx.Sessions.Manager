using System.Net;
using System.Net.Sockets;
using Sessions.Manager.Services;
using StackExchange.Redis;

namespace Sessions.Manager
{

    public class ServerUpReporter : IHostedService
    {
        private readonly ILogger<ServerUpReporter> _logger;
        private readonly RedisProxyService _redis;
        private readonly Services.Configuration _config;
        private PeriodicTimer _timer;
        private Task? _updateTask;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public ServerUpReporter(RedisProxyService redis,
            Configuration configuration,
            ILogger<ServerUpReporter> logger
            )
        {
            _logger = logger;
            _redis = redis;
            _config = configuration;
            _timer = new PeriodicTimer(TimeSpan.FromSeconds(_config.LivelinessTimeoutSec * 0.8));
        }



        public Task StartAsync(CancellationToken cancellationToken)
        {
            _updateTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await _timer.WaitForNextTickAsync();
                    await _redis.MarkServerAlive();
                }
            },cancellationToken);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
            if (_updateTask != null) await _updateTask;
            await _redis.MarkServerDead();
            _logger.LogInformation("Server is stopped. Marked as dead.");
            _timer.Dispose();
        }
    }
}
