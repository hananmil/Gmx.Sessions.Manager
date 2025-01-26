using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Sessions.Manager.Controllers.DTO;
using Sessions.Manager.Services;

namespace Sessions.Manager.Controllers
{
    [ApiController]
    [Route("diag")]
    [Produces(MediaTypeNames.Application.Json)]
    public class DiagnosticsController : Controller
    {
        private RedisProxyService _redis;

        public DiagnosticsController(RedisProxyService serversStateManager)
        {
            _redis = serversStateManager;
        }

        [HttpGet("servers")]
        public async Task<ServerStatus[]> Servers()
        {
            var servers = await _redis.GetServersDetails();

            return servers;
        }

        [HttpGet("sessions")]
        public async Task<Session[]> Sessions()
        {
            var sessions = await _redis.ListSessions();

            return sessions.ToArray();
        }
    }

}
