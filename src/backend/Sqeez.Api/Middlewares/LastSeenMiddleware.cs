using Microsoft.Extensions.Caching.Memory;
using Sqeez.Api.Data;
using System.Security.Claims;

namespace Sqeez.Api.Middlewares
{
    /// <summary>
    /// Updates the authenticated user's last-seen timestamp at most once per cache window.
    /// </summary>
    public class LastSeenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Creates middleware for throttled last-seen tracking.
        /// </summary>
        public LastSeenMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        /// <summary>
        /// Records activity for authenticated users and then invokes the next middleware.
        /// </summary>
        public async Task InvokeAsync(HttpContext context, SqeezDbContext dbContext)
        {
            // Only process authenticated users
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                // Extract the User ID from the JWT claims.
                var userIdString = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (long.TryParse(userIdString, out long userId))
                {
                    // Throttle updates using Memory Cache (Max 1 DB update per minute per user)
                    var cacheKey = $"LastSeen_{userId}";

                    if (!_cache.TryGetValue(cacheKey, out _))
                    {
                        var user = await dbContext.Students.FindAsync(userId);

                        if (user != null)
                        {
                            user.LastSeen = DateTime.UtcNow;
                            await dbContext.SaveChangesAsync();

                            _cache.Set(cacheKey, true, TimeSpan.FromMinutes(1));
                        }
                    }
                }
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Registration helpers for last-seen tracking.
    /// </summary>
    public static class LastSeenMiddlewareExtensions
    {
        /// <summary>
        /// Adds throttled last-seen tracking to the request pipeline.
        /// </summary>
        public static IApplicationBuilder UseLastSeenTracking(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LastSeenMiddleware>();
        }
    }
}
