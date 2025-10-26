using System.Net;
using System.Text.Json;
using VideoConferencingApp.Domain.Exceptions;

namespace VideoConferencingApp.Api.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {

            var statusCode = HttpStatusCode.InternalServerError;
            object errorResponse = new { error = "An unexpected internal server error has occurred.", statusCode = (int)statusCode };
            _logger.LogError(exception, "An unhandled exception has occurred: {ErrorMessage}", exception.Message);

            switch (exception)
            {

                case ValidationException validationException:
                    statusCode = HttpStatusCode.BadRequest;
                    errorResponse = new
                    {
                        title = "One or more validation errors occurred.",
                        statusCode = (int)statusCode,
                        errors = validationException.Errors
                    };
                    break;
                case UnauthorizedAccessException unauthorizedAccessException:
                    statusCode = HttpStatusCode.Unauthorized;
                    errorResponse = new { error = unauthorizedAccessException.Message, statusCode = (int)statusCode };
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            var result = JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(result);
        }
    }
}
