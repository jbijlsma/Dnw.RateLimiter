using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Dnw.RateLimiter.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Dnw.RateLimiter.Api.IntegrationTests;

/// <summary>
/// These tests don't mock the Redis service, which means there has to be a Redis instance running that is
/// accessible on the default redis port
/// </summary>
public class RateLimiterTests
{
    private const string Url = "api/RateLimited/apiKey";

    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task Simple_SingleRequestSucceeds(string rateLimiterType)
    {
        // Given
        Environment.SetEnvironmentVariable("RATE_LIMITER_TYPE", rateLimiterType);
        
        // When
        var actual = await CreateHttpClient().GetAsync(Url);

        // Then
        actual.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task Simple_TooManyRequests(string rateLimiterType)
    {
        // Given
        Environment.SetEnvironmentVariable("RATE_LIMITER_TYPE", rateLimiterType);
        Environment.SetEnvironmentVariable("RateLimiter__WindowInSeconds", "10");
        Environment.SetEnvironmentVariable("RateLimiter__MaxRequestsInWindow", "1");

        var client = CreateHttpClient();

        // When
        var actual = await client.GetAsync(Url);

        // Then
        actual.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // When
        actual = await client.GetAsync(Url);

        // Then
        actual.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    public static IEnumerable<object[]> TestCases => new List<object[]>
    {
        new object[] { RateLimiterType.FixedWindow.ToString() },
        new object[] { RateLimiterType.SlidingWindow.ToString() },
        new object[] { "UnknownRateLimiter" },
    };

    private static HttpClient CreateHttpClient()
    {
        var webAppFactory = new WebApplicationFactory<Program>();
        
        var client = webAppFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes("ApiKey:Pwd"))}");

        return client;
    }
}