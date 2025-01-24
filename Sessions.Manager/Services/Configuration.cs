using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using StackExchange.Redis;

namespace Sessions.Manager.Services
{
    public class Configuration
    {
        //public string Uri { get; }
        public string ClusterPrefix { get; }
        public int SessionTimeoutMinutes { get; }
        public int LivelinessTimeoutSec { get; }
        public string LocalPath { get; }
        public string ServerName { get; }

        public Configuration(IConfiguration configuration,ILogger<Configuration> logger)
        {
            ServerName = Dns.GetHostName();
            LivelinessTimeoutSec = configuration.GetValue<int?>("ServerUpReporter:ReportIntervalSeconds") ?? throw new InvalidOperationException("ServerUpReporter:ReportIntervalSeconds missing from config.");
            ClusterPrefix = configuration.GetValue<string>("ClusterPrefix") ?? throw new InvalidOperationException("Cluster prefix missing from config.");
            SessionTimeoutMinutes = configuration.GetValue<int?>("SessionTimeoutMinutes") ?? throw new InvalidOperationException("SessionTimeout missing from config.");
            LocalPath = configuration.GetValue<string>("LocalSessionRepository:Path") ?? throw new InvalidOperationException("Missing configuration for LocalSessionRepository:Path");

            logger.LogInformation("Configuration loaded.\n"+
                "Server name\t{serverName}\n"+
                "Cluster prefix\t{clusterPrefix}\n"+
                "Session timeout\t{sessionTimeout}\n"+
                "Liveliness timeout\t{livelinessTimeout}\n"+
                "Local path\t{localPath}",
                ServerName,ClusterPrefix,SessionTimeoutMinutes,LivelinessTimeoutSec,LocalPath);
        }

    }
}
