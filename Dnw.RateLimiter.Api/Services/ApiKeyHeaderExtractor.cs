using System.Net.Http.Headers;
using System.Text;

namespace Dnw.RateLimiter.Api.Services
{
    public interface IApiKeyExtractor
    {
        public string? GetApiKey();
    }

    internal class ApiKeyExtractor : IApiKeyExtractor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiKeyExtractor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetApiKey()
        {
            var encoded = string.Empty;
            var auth = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(auth))
            {
                encoded = AuthenticationHeaderValue.Parse(auth).Parameter;
            }

            return string.IsNullOrEmpty(encoded) 
                ? encoded 
                : Encoding.UTF8.GetString(Convert.FromBase64String(encoded)).Split(':')[0];
        }
    }
}