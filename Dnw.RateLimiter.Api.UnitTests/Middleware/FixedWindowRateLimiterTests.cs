using System.Net;
using Dnw.RateLimiter.Api.Middleware;
using Dnw.RateLimiter.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;
using Xunit;

namespace Dnw.RateLimiter.Api.UnitTests.Middleware;

public class FixedWindowRateLimiterTests
{
    private readonly IApiKeyExtractor _apiKeyExtractor;
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<RateLimiterConfig> _optionsMonitor;

    private readonly FixedWindowRateLimiter _rateLimiter;
    private readonly IDatabase _redisDb;

    public FixedWindowRateLimiterTests()
    {
        _apiKeyExtractor = Substitute.For<IApiKeyExtractor>();
        _next = Substitute.For<RequestDelegate>();
        _redisDb = Substitute.For<IDatabase>();

        var redisMux = Substitute.For<IConnectionMultiplexer>();
        redisMux.GetDatabase().Returns(_redisDb);

        _optionsMonitor = Substitute.For<IOptionsMonitor<RateLimiterConfig>>();

        _rateLimiter = new FixedWindowRateLimiter(_next, redisMux, _optionsMonitor);
    }

    [Fact]
    public async Task InvokeAsync()
    {
        // Given
        const int currentRequestCount = 10;
        const int maxRequestCountInWindow = 20;

        _apiKeyExtractor.GetApiKey().Returns("apiKey");

        var redisResults = RedisResult.Create(new[]
        {
            new RedisValue("0"),
            new RedisValue("0"),
            new RedisValue(currentRequestCount.ToString())
        });
        _redisDb
            .ScriptEvaluateAsync(FixedWindowRateLimiter.LuaScript, Arg.Any<object>())
            .Returns(redisResults);

        _optionsMonitor.CurrentValue.Returns(new RateLimiterConfig { MaxRequestsInWindow = maxRequestCountInWindow });

        var httpContext = new DefaultHttpContext();

        // When
        await _rateLimiter.InvokeAsync(httpContext, _apiKeyExtractor);

        // Then
        await _next.Received(1).Invoke(httpContext);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task InvokeAsync_ApiKeyMissing(string apiKey)
    {
        // Given
        _apiKeyExtractor.GetApiKey().Returns(apiKey);

        var httpContext = new DefaultHttpContext();

        // When
        await _rateLimiter.InvokeAsync(httpContext, _apiKeyExtractor);

        // Then
        httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        await _next.DidNotReceive().Invoke(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task InvokeAsync_MaxRequestsExceeded()
    {
        // Given
        const int currentRequestCount = 21;
        const int maxRequestCountInWindow = 20;

        _apiKeyExtractor.GetApiKey().Returns("apiKey");

        var redisResults = RedisResult.Create(new[]
        {
            new RedisValue("0"),
            new RedisValue("0"),
            new RedisValue(currentRequestCount.ToString())
        });
        _redisDb
            .ScriptEvaluateAsync(FixedWindowRateLimiter.LuaScript, Arg.Any<object>())
            .Returns(redisResults);

        _optionsMonitor.CurrentValue.Returns(new RateLimiterConfig { MaxRequestsInWindow = maxRequestCountInWindow });

        var httpContext = new DefaultHttpContext();

        // When
        await _rateLimiter.InvokeAsync(httpContext, _apiKeyExtractor);

        // Then
        httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);
        await _next.DidNotReceive().Invoke(Arg.Any<HttpContext>());
    }
}