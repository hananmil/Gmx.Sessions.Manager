using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Sessions.Manager.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Sessions.Manager.Controllers
{
    [ApiController]
    [Route("session")]
    [Tags("Sessions")]
    public class SessionsManagerController : ControllerBase
    {

        private readonly ILogger<SessionsManagerController> _logger;
        private readonly RedisProxyService _redis;
        private readonly LocalSessionRepository _localRepository;
        private readonly RemoteSessionProxy _removeProvider;

        public SessionsManagerController(
            RedisProxyService redis,
            ILogger<SessionsManagerController> logger, LocalSessionRepository localRepository, RemoteSessionProxy remoteSessionProvider )
        {
            _logger = logger;
            _redis = redis;
            _localRepository = localRepository;
            _removeProvider = remoteSessionProvider;
        }

        [HttpGet("{sessionId}")]
        [SwaggerResponse(StatusCodes.Status200OK, contentTypes: new[] { "application/octet-stream" })]
        [SwaggerResponse(StatusCodes.Status404NotFound)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError)]
        [SwaggerOperation("Get session data.")]
        public async Task<IActionResult> GetSession(
            [SwaggerParameter(Required = true, Description = "Session unique id.")]
            string sessionId )
        {
            Stream? ms = null;
            var sessionRemote = await _redis.IsSessionRemote(sessionId);
            if ( !sessionRemote.HasValue )
            {
                _logger.LogDebug("Session {sessionId} not found.", sessionId);
                return NotFound($"Session id [{sessionId}] not found.");
            }

            if ( sessionRemote.Value )
            {
                _logger.LogDebug("Reading remote session {sessionId}.", sessionId);
                ms = await _removeProvider.GetSession(sessionId);
            }
            else
            {
                _logger.LogDebug("Reading local session {sessionId}.", sessionId);
                ms = await _localRepository.GetSession(sessionId);
            }

            if ( ms == null )
            {
                _logger.LogError("Session {sessionId} not found.", sessionId);
                return base.Problem($"Failed to read [{sessionId}] not found.");
            }

            _logger.LogDebug("Returning session {sessionId} size {size}.", sessionId, ms.Length);
            return File(ms, "application/octet-stream");

        }

        [HttpPut("{sessionId}/{expirySeconds?}")]
        [SwaggerResponse(StatusCodes.Status200OK)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError)]
        [SwaggerOperation("Create or update session data.")]
        public async Task<IActionResult> UpdateSession(
            [SwaggerParameter(Required = true, Description = "Session unique id.")]
            string sessionId,
            [SwaggerParameter(Required = false,Description = "Override default expiry time.")]
            int? expirySeconds )
        {
            if ( !Request.Body.CanRead )
            {
                return BadRequest();
            }
            if ( expirySeconds.HasValue )
            {
                await _localRepository.UpdateSession(sessionId, Request.Body, TimeSpan.FromSeconds(expirySeconds.Value));
            }
            else
            {
                await _localRepository.UpdateSession(sessionId, Request.Body);
            }
            return Ok();
        }
    }
}
