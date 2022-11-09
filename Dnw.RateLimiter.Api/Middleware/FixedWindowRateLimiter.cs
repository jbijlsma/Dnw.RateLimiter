using System.Net;
using Dnw.RateLimiter.Api.Services;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using ILogger = Serilog.ILogger;

namespace Dnw.RateLimiter.Api.Middleware;

internal class FixedWindowRateLimiter
{
    private const string Script = @"
            local requests = redis.call('INCR',@key)
            redis.call('EXPIRE', @key, @expiry, 'NX') -- the NX option sets the expiry time only if the key does NOT have an expiry yet
            local window_end = redis.call('EXPIRETIME', @key)
            local window_start = window_end - @expiry
            return { window_start, window_end, requests }
            ";

    private readonly IDatabase _db;
    private readonly ILogger _log = Log.ForContext<FixedWindowRateLimiter>();
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<RateLimiterConfig> _optionsMonitor;

    public FixedWindowRateLimiter(RequestDelegate next, IConnectionMultiplexer mux,
        IOptionsMonitor<RateLimiterConfig> optionsMonitor)
    {
        _next = next;
        _optionsMonitor = optionsMonitor;
        _db = mux.GetDatabase();
    }

    internal static LuaScript LuaScript => LuaScript.Prepare(Script);

    public async Task InvokeAsync(HttpContext httpContext, IApiKeyExtractor apiKeyExtractor)
    {
        var apiKey = apiKeyExtractor.GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }

        var config = _optionsMonitor.CurrentValue;

        var redisResultArray = (RedisResult[])(await _db.ScriptEvaluateAsync(LuaScript,
            new
            {
                key = new RedisKey(apiKey), expiry = config.WindowInSeconds, maxRequests = config.MaxRequestsInWindow
            }))!;

        var requestCountInWindow = (int)redisResultArray[2];

        var maxRequestsExceeded = requestCountInWindow > config.MaxRequestsInWindow;

        var logEventLevel = maxRequestsExceeded ? LogEventLevel.Warning : LogEventLevel.Debug;
        _log.Write(
            logEventLevel,
            "RequestCount in fixed window from {windowStart} until {windowEnd}: {requestCountInWindow} (Max: {maxRequestsInWindow})",
            GetLocalTime(redisResultArray[0]),
            GetLocalTime(redisResultArray[1]),
            requestCountInWindow,
            config.MaxRequestsInWindow);

        if (maxRequestsExceeded)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            return;
        }

        await _next(httpContext);
    }

    private static DateTime GetLocalTime(RedisResult redisResult)
    {
        return DateTimeOffset.FromUnixTimeSeconds((long)redisResult).DateTime.ToLocalTime();
    }
}