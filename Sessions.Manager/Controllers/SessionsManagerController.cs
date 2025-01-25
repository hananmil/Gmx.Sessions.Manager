using Microsoft.AspNetCore.Mvc;
using Sessions.Manager.Services;

namespace Sessions.Manager.Controllers
{
    [ApiController]
    [Route("session")]
    public class SessionsManagerController : ControllerBase
    {

        private readonly ILogger<SessionsManagerController> _logger;
        private readonly RedisProxyService _redis;
        private readonly LocalSessionRepository _localRepository;
        private readonly RemoteSessionProxy _removeProvider;

        public SessionsManagerController(
            RedisProxyService redis,
            ILogger<SessionsManagerController> logger, LocalSessionRepository localRepository, RemoteSessionProxy remoteSessionProvider)
        {
            _logger = logger;
            _redis = redis;
            _localRepository = localRepository;
            _removeProvider = remoteSessionProvider;
        }

        [HttpGet("{sessionId}")]
        public async Task<IActionResult> GetSession(string sessionId)
        {
            Stream? ms = null;
            if (await _redis.IsSessionRemote(sessionId))
            {
                _logger.LogDebug("Reading remote session {sessionId}.", sessionId);
                ms = await _removeProvider.GetSession(sessionId);
            }
            else
            {
                _logger.LogDebug("Reading local session {sessionId}.", sessionId);
                ms = await _localRepository.GetSession(sessionId);
            }

            if (ms == null)
            {
                return NotFound();
            }
            _logger.LogDebug("Returning session {sessionId} size {size}.", sessionId,ms.Length);
            return File(ms, "application/octet-stream");

        }

        [HttpPut("{sessionId}")]
        public async Task<IActionResult> UpdateSession(string sessionId)
        {
            if (!Request.Body.CanRead)
            {
                return BadRequest();
            }

            await _localRepository.UpdateSession(sessionId, Request.Body);

            return Ok();
        }
    }
}
