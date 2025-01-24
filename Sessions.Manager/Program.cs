using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Sessions.Manager;
using Sessions.Manager.Controllers;
using Sessions.Manager.Services;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<ServerAddressProvider>();
builder.Services.AddSingleton(typeof(Configuration));
builder.Services.AddSingleton(typeof(RedisProxyService));
builder.Services.AddSingleton(typeof(LocalSessionRepository));
builder.Services.AddSingleton(typeof(RemoteSessionProvider));

builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var logger = provider.GetRequiredService<ILogger<Object>>();
    var connectionString = configuration.GetValue<string>("Redis:ConnectionString")??
        throw new InvalidOperationException("Missing redis configuration");

    var redis = ConnectionMultiplexer.Connect(connectionString, co =>
    {
        co.ReconnectRetryPolicy = new ExponentialRetry(1000, 5000);
        co.ConnectTimeout = 1000;
        co.SyncTimeout = 1000;
        co.AbortOnConnectFail = false;
    });
    return redis;
});

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddHostedService<ServerUpReporter>();
builder.Services.AddHostedService<ServerCleanup>();


builder.WebHost.ConfigureKestrel(options =>
{
    // Handle requests up to 256 MB
    options.Limits.MaxRequestBodySize = 256 * 1024 * 1024;
    // Max concurrent connections
    options.Limits.MaxConcurrentConnections = 100;
});


var app = builder.Build();
app.MapControllers();
// Configure the HTTP request pipeline.
app.Run();


