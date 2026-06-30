using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HookSentry.Billing.Jobs;

// Full implementation: see spec-event-retention-11.
public class EventRetentionJob(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    ILogger<EventRetentionJob> logger)
    : ScheduledJob(scopeFactory, redis, logger)
{
    protected override string JobName => "event-retention";

    protected override DateTime GetNextRunUtc(DateTime now)
    {
        var next = new DateTime(now.Year, now.Month, now.Day, 2, 0, 0, DateTimeKind.Utc);
        return next <= now ? next.AddDays(1) : next;
    }

    protected override Task RunCoreAsync(CancellationToken ct)
    {
        Logger.LogInformation("EventRetentionJob: pending implementation (spec-event-retention-11).");
        return Task.CompletedTask;
    }
}
