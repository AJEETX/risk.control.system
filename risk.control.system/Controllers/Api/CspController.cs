using Microsoft.AspNetCore.Mvc;

using risk.control.system.Models.ViewModel;

namespace risk.control.system.Controllers.Api
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    public class CspController : ControllerBase
    {
        private readonly ILogger<CspController> _logger;

        public CspController(ILogger<CspController> logger)
        {
            _logger = logger;
        }

        [HttpPost("csp-violations")]
        public IActionResult CSPReport([FromBody] CspViolation cspViolation)
        {
            _logger.LogWarning($"URI: {cspViolation.CspReport.DocumentUri}, Blocked: {cspViolation.CspReport.BlockedUri}");

            return Ok();
        }
    }
}