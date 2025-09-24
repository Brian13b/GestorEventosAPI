using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Security.Claims;

namespace EventManagementAPI.Filters
{
    public class RateLimitFilter : ActionFilterAttribute
    {
        private readonly int _maxRequests;
        private readonly TimeSpan _timeWindow;
        private readonly IMemoryCache _cache;

        public RateLimitFilter(int maxRequests = 60, int timeWindowInMinutes = 1)
        {
            _maxRequests = maxRequests;
            _timeWindow = TimeSpan.FromMinutes(timeWindowInMinutes);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
            if (cache == null) return;

            var key = GetRateLimitKey(context.HttpContext);
            var requests = cache.Get<List<DateTime>>(key) ?? new List<DateTime>();

            var cutoffTime = DateTime.UtcNow.Subtract(_timeWindow);
            requests = requests.Where(time => time > cutoffTime).ToList();

            if (requests.Count >= _maxRequests)
            {
                context.Result = new ContentResult
                {
                    StatusCode = (int)HttpStatusCode.TooManyRequests,
                    Content = "Demasiadas peticiones. Intenta más tarde.",
                    ContentType = "application/json"
                };
                return;
            }

            requests.Add(DateTime.UtcNow);
            cache.Set(key, requests, _timeWindow);
        }

        private string GetRateLimitKey(HttpContext context)
        {
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();

            return $"rate_limit_{userId ?? ipAddress}_{context.Request.Path}";
        }
    }
}
