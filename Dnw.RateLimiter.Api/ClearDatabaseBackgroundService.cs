using StackExchange.Redis;

namespace Dnw.RateLimiter.Api;

public class ClearDatabaseStartupService : BackgroundService
{
    private readonly IConnectionMultiplexer _mux;
    private readonly ILogger<ClearDatabaseStartupService> _logger;

    public ClearDatabaseStartupService(IConnectionMultiplexer mux, ILogger<ClearDatabaseStartupService> logger)
    {
        _mux = mux;
        _logger = logger;
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var server in _mux.GetServers())
        {
            server.FlushDatabase();
        }
        
        _logger.LogInformation("Redis database keys cleared");
        
        return Task.CompletedTask;
    }
}