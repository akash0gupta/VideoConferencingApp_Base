using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Infrastructure.Services.AuthServices
{
    public interface IHttpContextService
    {
        string TraceId { get; }
        string CorrelationId { get; }
        string IpAddressV4 { get; }
        string IpAddressV6 { get; }
        string UserAgent { get; }
        string RequestPath { get; }
        string RequestMethod { get; }

        void AddResponseHeader(string key, string value);
        void RemoveResponseHeader(string key);
        string? GetRequestHeader(string key);

        void SetCookie(string key, string value, CookieOptions? options = null);
        string? GetCookie(string key);
        void DeleteCookie(string key);

        Dictionary<string, string> GetAllRequestHeaders();
    }
}
