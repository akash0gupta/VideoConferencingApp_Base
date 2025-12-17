using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.DTOs.Authentication;

namespace VideoConferencingApp.Infrastructure.Services.AuthServices
{
    public interface IResponseHeaderService
    {
        void SetRetryAfter(int seconds);
        void SetRetryAfter(TimeSpan timeSpan);
        void SetRetryAfter(DateTime retryAfterDateTime);

        void SetRateLimit(int limit, int remaining, DateTime resetTime);
        void SetRateLimit(RateLimitInfo rateLimitInfo);

        void SetPaginationHeaders(PaginationMetadata pagination);
        void SetCustomHeader(string key, string value);

        void SetCacheControl(string cacheControl);
        void SetNoCache();
        void SetETag(string etag);
    }

}
