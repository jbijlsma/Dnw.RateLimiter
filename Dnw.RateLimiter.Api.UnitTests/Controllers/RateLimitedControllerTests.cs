using Dnw.RateLimiter.Api.Controllers;
using Dnw.RateLimiter.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Dnw.RateLimiter.Api.UnitTests.Controllers;

public class RateLimitedControllerTests
{
    [Fact]
    public void GetApiKey()
    {
        // Given
        var apiKeyHeaderExtractor = Substitute.For<IApiKeyExtractor>();
        const string expectedApiKey = "apiKey";
        apiKeyHeaderExtractor.GetApiKey().Returns(expectedApiKey);

        var controller = new RateLimitedController(apiKeyHeaderExtractor);

        // When
        var actual = (JsonResult)controller.GetApiKey();

        // Then
        var expected = new { key = expectedApiKey };
        actual.Value.Should().BeEquivalentTo(expected);
    }
}