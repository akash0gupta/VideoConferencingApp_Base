namespace VideoConferencingApp.Api.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Remove server header
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            // Add security headers
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Add("Permissions-Policy", "camera=(self), microphone=(self), geolocation=()");

            // Content Security Policy
            context.Response.Headers.Add("Content-Security-Policy",
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; " +
                "media-src 'self'; " +
                "connect-src 'self' wss: ws:; " +
                "frame-ancestors 'none';"
            );

            // HSTS (HTTP Strict Transport Security)
            if (context.Request.IsHttps)
            {
                context.Response.Headers.Add("Strict-Transport-Security",
                    "max-age=31536000; includeSubDomains; preload");
            }

            await _next(context);
        }
    }
}
