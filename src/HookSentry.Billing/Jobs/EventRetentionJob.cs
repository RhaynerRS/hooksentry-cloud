using HookSentry.Domain.Tenants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHibernate;
using NHibernate.Linq;
using StackExchange.Redis;

namespace HookSentry.Billing.Jobs;

public class EventRetentionJob(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    ILogger<EventRetentionJob> logger,
    IOptions<EventRetentionOptions> options)
    : ScheduledJob(scopeFactory, redis, logger)
{
    protected override string JobName => "event-retention";

    protected override DateTime GetNextRunUtc(DateTime now)
    {
        var next = new DateTime(now.Year, now.Month, now.Day, 2, 0, 0, DateTimeKind.Utc);
        return next <= now ? next.AddDays(1) : next;
    }

    protected override async Task RunCoreAsync(CancellationToken ct)
    {
        if (!options.Value.Enabled)
        {
            Logger.LogInformation("EventRetentionJob: disabled via config.");
            return;
        }

        var cutoff = DateTime.UtcNow.AddDays(-options.Value.RetentionDays);

        IList<Guid> tenantIds;
        await using (var readScope = ScopeFactory.CreateAsyncScope())
        {
            var tenantRepo = readScope.ServiceProvider.GetRequiredService<ITenantRepository>();
            tenantIds = await tenantRepo.Query().Select(t => t.Id).ToListAsync(ct);
        }

        var totalDeleted = 0;

        foreach (var tenantId in tenantIds)
        {
            int deleted;
            do
            {
                deleted = await DeleteBatchAsync(tenantId, cutoff, ct);
                totalDeleted += deleted;
            }
            while (deleted == 5_000);

            if (deleted > 0)
                Logger.LogInformation(
                    "EventRetentionJob: tenant={TenantId} cutoff={Cutoff:u} deleted={Count}",
                    tenantId, cutoff, deleted);
        }

        Logger.LogInformation(
            "EventRetentionJob: completed. tenants={Tenants} totalDeleted={Total} retentionDays={Days}",
            tenantIds.Count, totalDeleted, options.Value.RetentionDays);
    }

    private async Task<int> DeleteBatchAsync(Guid tenantId, DateTime cutoff, CancellationToken ct)
    {
        await using var scope  = ScopeFactory.CreateAsyncScope();
        var session    = scope.ServiceProvider.GetRequiredService<ISession>();

        return await session.CreateSQLQuery("""
            DELETE FROM eventos
            WHERE tenant_id = :tenantId
              AND created_at < :cutoff
              AND id IN (
                SELECT id FROM eventos
                WHERE tenant_id = :tenantId
                  AND created_at < :cutoff
                LIMIT 5000
              )
            """)
            .SetParameter("tenantId", tenantId)
            .SetParameter("cutoff", cutoff)
            .ExecuteUpdateAsync(ct);
    }
}
