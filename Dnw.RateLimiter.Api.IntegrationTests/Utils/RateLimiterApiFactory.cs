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
    private readonly string _connectionString;

    public RateLimiterApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IConnectionMultiplexer));

            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(_connectionString));
        });
    }
}