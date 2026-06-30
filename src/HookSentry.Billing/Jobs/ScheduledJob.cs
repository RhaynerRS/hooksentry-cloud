using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HookSentry.Billing.Jobs;

public abstract class ScheduledJob(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    ILogger logger) : BackgroundService
{
    protected readonly IServiceScopeFactory ScopeFactory = scopeFactory;
    protected readonly IConnectionMultiplexer Redis = redis;
    protected readonly ILogger Logger = logger;

    protected abstract string JobName { get; }
    protected abstract DateTime GetNextRunUtc(DateTime now);
    protected abstract Task RunCoreAsync(CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetNextRunUtc(DateTime.UtcNow) - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            var db = Redis.GetDatabase();
            var lockKey = $"cloud:job-lock:{JobName}";
            var acquired = await db.StringSetAsync(lockKey, "1", TimeSpan.FromHours(2), When.NotExists);

            if (!acquired)
            {
                Logger.LogInformation("{Job} skipped — lock held by another instance.", JobName);
                continue;
            }

            try
            {
                Logger.LogInformation("{Job} starting.", JobName);
                await RunCoreAsync(stoppingToken);
                Logger.LogInformation("{Job} completed.", JobName);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { Logger.LogError(ex, "{Job} failed.", JobName); }
            finally { await db.KeyDeleteAsync(lockKey); }
        }
    }
}
