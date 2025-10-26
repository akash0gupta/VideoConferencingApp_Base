using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VideoConferencingApp.API.Controllers.Base
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{v:apiVersion}/[controller]/[action]")]
    [Authorize]
    public abstract class BaseController : ControllerBase
    {
        protected readonly ILogger _logger;

        protected BaseController(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Standardized response wrapper
        /// </summary>
        protected IActionResult Success(object result, string? message = null)
        {
            return Ok(new
            {
                success = true,
                message,
                data = result
            });
        }

        protected IActionResult Failure(string errorMessage, int statusCode = 400)
        {
            _logger.LogWarning("API error: {Error}", errorMessage);

            return StatusCode(statusCode, new
            {
                success = false,
                message = errorMessage
            });
        }

        protected string TraceId => HttpContext?.TraceIdentifier ?? string.Empty;
    }
}