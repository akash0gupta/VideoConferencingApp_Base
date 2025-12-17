using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VideoConferencingApp.Application.DTOs.Authentication;

namespace VideoConferencingApp.Infrastructure.Services.AuthServices
{

        public class ResponseHeaderService : IResponseHeaderService
        {
            private readonly IHttpContextService _httpContextService;

            public ResponseHeaderService(IHttpContextService httpContextService)
            {
                _httpContextService = httpContextService;
            }

            public void SetRetryAfter(int seconds)
            {
                _httpContextService.AddResponseHeader("Retry-After", seconds.ToString());
            }

            public void SetRetryAfter(TimeSpan timeSpan)
            {
                SetRetryAfter((int)timeSpan.TotalSeconds);
            }

            public void SetRetryAfter(DateTime retryAfterDateTime)
            {
                var seconds = (int)(retryAfterDateTime - DateTime.UtcNow).TotalSeconds;
                SetRetryAfter(seconds > 0 ? seconds : 0);
            }

            public void SetRateLimit(int limit, int remaining, DateTime resetTime)
            {
                _httpContextService.AddResponseHeader("X-RateLimit-Limit", limit.ToString());
                _httpContextService.AddResponseHeader("X-RateLimit-Remaining", remaining.ToString());
                _httpContextService.AddResponseHeader("X-RateLimit-Reset",
                    new DateTimeOffset(resetTime).ToUnixTimeSeconds().ToString());
            }

            public void SetRateLimit(RateLimitInfo rateLimitInfo)
            {
                SetRateLimit(rateLimitInfo.Limit, rateLimitInfo.Remaining, rateLimitInfo.ResetTime);
            }

            public void SetPaginationHeaders(PaginationMetadata pagination)
            {
                var paginationJson = JsonSerializer.Serialize(new
                {
                    currentPage = pagination.CurrentPage,
                    pageSize = pagination.PageSize,
                    totalPages = pagination.TotalPages,
                    totalCount = pagination.TotalCount,
                    hasNext = pagination.HasNext,
                    hasPrevious = pagination.HasPrevious
                });

                _httpContextService.AddResponseHeader("X-Pagination", paginationJson);
            }

            public void SetCustomHeader(string key, string value)
            {
                _httpContextService.AddResponseHeader(key, value);
            }

            public void SetCacheControl(string cacheControl)
            {
                _httpContextService.AddResponseHeader("Cache-Control", cacheControl);
            }

            public void SetNoCache()
            {
                SetCacheControl("no-cache, no-store, must-revalidate");
                _httpContextService.AddResponseHeader("Pragma", "no-cache");
                _httpContextService.AddResponseHeader("Expires", "0");
            }

            public void SetETag(string etag)
            {
                _httpContextService.AddResponseHeader("ETag", $"\"{etag}\"");
            }
        }
}

