using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Infrastructure.Services.AuthServices
{
        public class HttpContextService : IHttpContextService
        {
            private readonly IHttpContextAccessor _httpContextAccessor;

            public HttpContextService(IHttpContextAccessor httpContextAccessor)
            {
                _httpContextAccessor = httpContextAccessor;
            }

            private HttpContext? Context => _httpContextAccessor.HttpContext;

            public string TraceId => Context?.TraceIdentifier ?? Guid.NewGuid().ToString();

            public string CorrelationId =>
                GetRequestHeader("X-Correlation-ID")
                ?? GetRequestHeader("X-Request-ID")
                ?? TraceId;

            public string IpAddressV4
            {
                get
                {
                    if (Context == null)
                        return "Unknown";

                    // Check for X-Forwarded-For header (behind proxy/load balancer)
                    var forwardedFor = GetRequestHeader("X-Forwarded-For");
                    if (!string.IsNullOrEmpty(forwardedFor))
                    {
                        var ips = forwardedFor.Split(',');
                        return ips[0].Trim();
                    }

                    // Check for X-Real-IP header
                    var realIp = GetRequestHeader("X-Real-IP");
                    if (!string.IsNullOrEmpty(realIp))
                        return realIp;

                    // Fallback to remote IP address
                    return Context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Unknown";
                }
            }

        public string IpAddressV6
        {
            get
            {
                if (Context == null)
                    return "Unknown";

                // Check for X-Forwarded-For header (behind proxy/load balancer)
                var forwardedFor = GetRequestHeader("X-Forwarded-For");
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    var ips = forwardedFor.Split(',');
                    return ips[0].Trim();
                }

                // Check for X-Real-IP header
                var realIp = GetRequestHeader("X-Real-IP");
                if (!string.IsNullOrEmpty(realIp))
                    return realIp;

                // Fallback to remote IP address
                return Context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "Unknown";
            }
        }

        public string UserAgent => GetRequestHeader("User-Agent") ?? "Unknown";

            public string RequestPath => Context?.Request.Path.Value ?? string.Empty;

            public string RequestMethod => Context?.Request.Method ?? string.Empty;

            public void AddResponseHeader(string key, string value)
            {
                if (Context == null)
                    throw new InvalidOperationException("HttpContext is not available");

                if (Context.Response.HasStarted)
                {
                    throw new InvalidOperationException(
                        "Cannot add headers after response has started");
                }

                if (Context.Response.Headers.ContainsKey(key))
                {
                    Context.Response.Headers[key] = value;
                }
                else
                {
                    Context.Response.Headers.Add(key, value);
                }
            }

            public void RemoveResponseHeader(string key)
            {
                if (Context == null || Context.Response.HasStarted)
                    return;

                Context.Response.Headers.Remove(key);
            }

            public string? GetRequestHeader(string key)
            {
                if (Context == null)
                    return null;

                return Context.Request.Headers.TryGetValue(key, out var value)
                    ? value.FirstOrDefault()
                    : null;
            }

            public Dictionary<string, string> GetAllRequestHeaders()
            {
                if (Context == null)
                    return new Dictionary<string, string>();

                return Context.Request.Headers
                    .ToDictionary(h => h.Key, h => h.Value.ToString());
            }

            public void SetCookie(string key, string value, CookieOptions? options = null)
            {
                if (Context == null)
                    throw new InvalidOperationException("HttpContext is not available");

                options ??= new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                };

                Context.Response.Cookies.Append(key, value, options);
            }

            public string? GetCookie(string key)
            {
                if (Context == null)
                    return null;

                return Context.Request.Cookies.TryGetValue(key, out var value) ? value : null;
            }

            public void DeleteCookie(string key)
            {
                Context?.Response.Cookies.Delete(key);
            }
        }
}
