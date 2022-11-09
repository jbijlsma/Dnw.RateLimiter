using Dnw.RateLimiter.Api;
using Dnw.RateLimiter.Api.Middleware;
using Dnw.RateLimiter.Api.Services;
using JetBrains.Annotations;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// allowAdmin is necessary to be able to clear the keys on startup
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost,allowAdmin=true"));
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
if (!Enum.TryParse<RateLimiterType>(rateLimiterTypeEnvVariable, out var rateLimiterType)) {
    rateLimiterType = RateLimiterType.FixedWindow;
}

var logger = app.Services.GetRequiredService<ILogger<Program>>();

if (rateLimiterType == RateLimiterType.FixedWindow)
{
    app.UseMiddleware<FixedWindowRateLimiter>();  
    logger.LogInformation("Using RateLimiter: {rateLimiterType}", nameof(FixedWindowRateLimiter));
}
else
{
    app.UseMiddleware<SlidingWindowRateLimiter>();
    logger.LogInformation("Using RateLimiter: {rateLimiterType}", nameof(SlidingWindowRateLimiter));
}

app.MapControllers();

app.Run();

[UsedImplicitly]
internal partial class Program { }
