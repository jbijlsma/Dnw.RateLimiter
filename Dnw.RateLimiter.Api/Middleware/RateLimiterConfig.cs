using JetBrains.Annotations;

namespace Dnw.RateLimiter.Api.Middleware;

internal class RateLimiterConfig
{
    public int WindowInSeconds { get; [UsedImplicitly]set; }
    public int MaxRequestsInWindow { get; [UsedImplicitly]set; }
}