using Dnw.RateLimiter.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dnw.RateLimiter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RateLimitedController : ControllerBase
{
    private readonly IApiKeyExtractor _apiKeyExtractor;

    public RateLimitedController(IApiKeyExtractor apiKeyExtractor)
    {
        _apiKeyExtractor = apiKeyExtractor;
    }

    [HttpGet("apiKey")]
    public IActionResult GetApiKey()
    {
        return new JsonResult(new { Key = _apiKeyExtractor.GetApiKey(), Environment.MachineName });
    }
}