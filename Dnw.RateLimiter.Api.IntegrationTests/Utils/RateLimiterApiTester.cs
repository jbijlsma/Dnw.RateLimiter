using System.Net.Http.Headers;
using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Dnw.RateLimiter.Api.IntegrationTests.Utils;

public static class RateLimiterApiTester
{
    public static async Task ExecuteTest(
        Action setEnvironmentVariables,
        Func<HttpClient, Task> test)
    {
        // Default redis:5.0.14 image does not work with StackExchange.Redis version
        // Getting error: Wrong number of args calling Redis command From Lua script
        var redisContainer = new TestcontainersBuilder<RedisTestcontainer>()
            .WithDatabase(new RedisTestcontainerConfiguration("redis:latest"))
            .Build();

        try
        {
            await redisContainer.StartAsync().ConfigureAwait(false);

            setEnvironmentVariables();

            var factory = new RateLimiterApiFactory(redisContainer.GetMappedPublicPort(6379));

            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Authorization =
                AuthenticationHeaderValue.Parse(
                    $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes("ApiKey:Pwd"))}");

            await test(client);
        }
        finally
        {
            await redisContainer.StopAsync().ConfigureAwait(false);
        }
    }
}