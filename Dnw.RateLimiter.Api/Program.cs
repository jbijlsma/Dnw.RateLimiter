using Dnw.RateLimiter.Api.Middleware;
using Dnw.RateLimiter.Api.Services;
using JetBrains.Annotations;
using Serilog;
using StackExchange.Redis;

namespace Dnw.RateLimiter.Api;

[UsedImplicitly]
public class Program
{
    private static int Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        Log.Logger = logger;

        try
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(logger);

            // Add services to the container.
            // allowAdmin is necessary to be able to clear the keys on startup
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect("localhost,allowAdmin=true"));
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IApiKeyExtractor, ApiKeyExtractor>();

            builder.Services.Configure<RateLimiterConfig>(builder.Configuration.GetSection("RateLimiter"));

            builder.Services.AddHostedService<ClearDatabaseStartupService>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            var rateLimiterTypeEnvVariable = Environment.GetEnvironmentVariable("RATE_LIMITER_TYPE");
            if (!Enum.TryParse<RateLimiterType>(rateLimiterTypeEnvVariable, out var rateLimiterType))
                rateLimiterType = RateLimiterType.FixedWindow;

            if (rateLimiterType == RateLimiterType.FixedWindow)
            {
                app.UseMiddleware<FixedWindowRateLimiter>();
                Log.ForContext<Program>().Debug("Using RateLimiter: {rateLimiterType}", nameof(FixedWindowRateLimiter));
            }
            else
            {
                app.UseMiddleware<SlidingWindowRateLimiter>();
                Log.ForContext<Program>()
                    .Debug("Using RateLimiter: {rateLimiterType}", nameof(SlidingWindowRateLimiter));
            }

            app.MapControllers();

            app.Run();

            return 0;
        }
        catch (Exception ex)
        {
            Log.ForContext<Program>().Fatal(ex, "Startup error");
            return -1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}