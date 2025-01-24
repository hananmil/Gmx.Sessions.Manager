using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Sessions.Manager.Services;

namespace Sessions.Manager.Controllers
{
    [ApiController]
    [Route("diag")]
    public class DiagnosticsController : Controller
    {
        private RedisProxyService _redis;

        public DiagnosticsController(RedisProxyService serversStateManager)
        {
            _redis = serversStateManager;
        }

        [HttpGet("servers")]
        public async Task<IActionResult> Index()
        {
            var servers = await _redis.GetServersDetails();

            return this.Content(JsonSerializer.Serialize(servers, new JsonSerializerOptions { WriteIndented=true }));
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> Sessions()
        {
            var sessions = await _redis.ListSessions();

            return this.Content(JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented=true }));
        }
    }

}
