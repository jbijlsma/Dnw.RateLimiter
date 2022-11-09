using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Dnw.RateLimiter.Api.IntegrationTests.Utils;

[UsedImplicitly]
public class RateLimiterApiFactory : WebApplicationFactory<Program>
{
    private readonly ushort _redisPort;

    public RateLimiterApiFactory(ushort redisPort)
    {
        _redisPort = redisPort;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IConnectionMultiplexer));

            var connectionString = $"localhost:{_redisPort}";
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(connectionString));
        });
    }
}