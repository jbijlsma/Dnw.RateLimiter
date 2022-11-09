using System.Net;
using Dnw.RateLimiter.Api.Services;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Dnw.RateLimiter.Api.Middleware;

internal class SlidingWindowRateLimiter
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SlidingWindowRateLimiter> _logger;
    private readonly IOptionsMonitor<RateLimiterConfig> _optionsMonitor;
    private readonly IDatabase _db;

    public SlidingWindowRateLimiter(
        RequestDelegate next, 
        IConnectionMultiplexer mux,
        ILogger<SlidingWindowRateLimiter> logger,
        IOptionsMonitor<RateLimiterConfig> optionsMonitor)
    {
        _next = next;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _db = mux.GetDatabase();
    }
    
    public async Task InvokeAsync(HttpContext httpContext, IApiKeyExtractor apiKeyExtractor)
    {
        var apiKey = apiKeyExtractor.GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }

        var config = _optionsMonitor.CurrentValue;

        var requestCountInWindow = await _db.ScriptEvaluateAsync(LuaScript, new {key = new RedisKey(apiKey), window = config.WindowInSeconds});
        
        _logger.LogInformation("RequestCount during last {windowInSeconds} seconds: {requestCountInWindow} (Max: {maxRequestsInWindow})", config.WindowInSeconds, requestCountInWindow, config.MaxRequestsInWindow);
        
        if ((int)requestCountInWindow > config.MaxRequestsInWindow)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            return;
        }
        
        await _next(httpContext);
    }

    internal static LuaScript LuaScript => LuaScript.Prepare(Script);
    private const string Script = @"
            local current_time = redis.call('TIME') -- t[1] contains seconds since epoch, t[2] contains milliseconds
            local trim_time = tonumber(current_time[1]) - @window -- trim_time  
            redis.call('ZREMRANGEBYSCORE', @key, 0, trim_time) -- remove values between 0 and trim_time
            local request_count = redis.call('ZCARD',@key) -- return the cardinality of the values (array length)

            -- Here we decide to also count rate-limited requests
            -- So someone hammering the api has to stop sending requests first  
            redis.call('ZADD', @key, current_time[1], current_time[2])
            redis.call('EXPIRE', @key, @window)
            request_count = request_count + 1

            return request_count
            ";
}