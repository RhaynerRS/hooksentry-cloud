using HookSentry.Billing.TenantAccess;
using HookSentry.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.Linq;
using StackExchange.Redis;

namespace HookSentry.Billing.Jobs;

public class UsageFlushJob(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    ILogger<UsageFlushJob> logger)
    : ScheduledJob(scopeFactory, redis, logger)
{
    protected override string JobName => "usage-flush";

    protected override DateTime GetNextRunUtc(DateTime now)
    {
        var next = new DateTime(now.Year, now.Month, now.Day, 3, 0, 0, DateTimeKind.Utc);
        return next <= now ? next.AddDays(1) : next;
    }

    protected override async Task RunCoreAsync(CancellationToken ct)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();
        var stateRepo  = scope.ServiceProvider.GetRequiredService<ITenantCloudStateRepository>();
        var session    = scope.ServiceProvider.GetRequiredService<ISession>();
        var uowFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();

        var tenantIds = await stateRepo.Query()
            .Select(s => s.TenantId)
            .ToListAsync(ct);

        var period = DateTime.UtcNow.ToString("yyyy-MM");
        var db     = Redis.GetDatabase();

        await using var uow = uowFactory.Create();

        foreach (var tenantId in tenantIds)
        {
            var key   = $"cloud:usage:{tenantId}:{period}:events";
            var raw   = await db.StringGetAsync(key);
            var count = raw.HasValue ? (int)(long)raw : 0;

            await session.CreateSQLQuery("""
                INSERT INTO usage_snapshots (tenant_id, period_year_month, event_count, updated_at)
                VALUES (:tenantId, :period, :count, NOW())
                ON CONFLICT (tenant_id, period_year_month)
                DO UPDATE SET event_count = :count, updated_at = NOW()
                """)
                .SetParameter("tenantId", tenantId)
                .SetParameter("period", period)
                .SetParameter("count", count)
                .ExecuteUpdateAsync(ct);
        }

        await uow.CommitAsync(ct);

        Logger.LogInformation(
            "UsageFlushJob: flushed {Count} tenants for period {Period}.",
            tenantIds.Count, period);
    }
}
