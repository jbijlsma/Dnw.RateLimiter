using Serilog;
using StackExchange.Redis;
using ILogger = Serilog.ILogger;

namespace Dnw.RateLimiter.Api;

public class ClearDatabaseStartupService : BackgroundService
{
    private readonly ILogger _log = Log.ForContext<ClearDatabaseStartupService>();
    private readonly IConnectionMultiplexer _mux;

    public ClearDatabaseStartupService(IConnectionMultiplexer mux)
    {
        _mux = mux;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var server in _mux.GetServers()) server.FlushDatabase();

        _log.Debug("Redis keys cleared");

        return Task.CompletedTask;
    }
}