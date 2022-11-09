using System.Text;
using Dnw.RateLimiter.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace Dnw.RateLimiter.Api.UnitTests.Services;

public class ApiKeyHeaderExtractorTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApiKeyExtractor _apiKeyExtractor;
    
    public ApiKeyHeaderExtractorTests()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _apiKeyExtractor = new ApiKeyExtractor(_httpContextAccessor);
    }
    
    
    [Theory]
    [InlineData("ApiKey:Pwd", "ApiKey")]
    [InlineData("ApiKey", "ApiKey")]
    public void GetApiKey(string basicAuthCredentials, string expected)
    {
        // Given
        var base64BasicAuthCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(basicAuthCredentials));
        var expectedHttpContext = new DefaultHttpContext { Request = { Headers = { { "Authorization", $"Basic {base64BasicAuthCredentials}" } } } };
        _httpContextAccessor.HttpContext.Returns(expectedHttpContext);

        // When
        var actual = _apiKeyExtractor.GetApiKey();

        // Then
        actual.Should().Be(expected);
    }
    
    [Fact]
    public void GetApiKey_AuthorizationHeaderMissing()
    {
        // Given
        var expectedHttpContext = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Returns(expectedHttpContext);

        // When
        var actual = _apiKeyExtractor.GetApiKey();

        // Then
        actual.Should().BeEmpty();
    }
}