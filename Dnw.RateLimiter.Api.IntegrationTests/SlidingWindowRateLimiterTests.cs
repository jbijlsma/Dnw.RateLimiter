using System.Net;
using Dnw.RateLimiter.Api.IntegrationTests.Utils;
using Dnw.RateLimiter.Api.Middleware;
using FluentAssertions;
using Xunit;

namespace Dnw.RateLimiter.Api.IntegrationTests;

public class SlidingWindowRateLimiterTests
{
    private const string Url = "api/RateLimited/apiKey";

    [Fact]
    public async Task SingleRequest()
    {
        await RateLimiterApiTester.ExecuteTest(
            () =>
            {
                // Given
                Environment.SetEnvironmentVariable("RATE_LIMITER_TYPE", RateLimiterType.SlidingWindow.ToString());
            },
            async client =>
            {
                // When
                var actual = await client.GetAsync(Url);

                // Then
                actual.StatusCode.Should().Be(HttpStatusCode.OK);
            });
    }

    [Fact]
    public async Task TooManyRequests()
    {
        await RateLimiterApiTester.ExecuteTest(
            () =>
            {
                // Given
                Environment.SetEnvironmentVariable("RATE_LIMITER_TYPE", RateLimiterType.SlidingWindow.ToString());
                Environment.SetEnvironmentVariable("RateLimiter__WindowInSeconds", "10");
                Environment.SetEnvironmentVariable("RateLimiter__MaxRequestsInWindow", "1");
            },
            async client =>
            {
                // When
                var actual = await client.GetAsync(Url);

                // Then
                actual.StatusCode.Should().Be(HttpStatusCode.OK);

                // When
                actual = await client.GetAsync(Url);

                // Then
                actual.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            });
    }
}