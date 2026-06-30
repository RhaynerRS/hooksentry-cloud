using HookSentry.Billing.TenantAccess;
using HookSentry.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.Linq;
using StackExchange.Redis;

namespace HookSentry.Billing.Jobs;

public class DataPurgeJob(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    ILogger<DataPurgeJob> logger)
    : ScheduledJob(scopeFactory, redis, logger)
{
    protected override string JobName => "data-purge";

    // Runs on the 1st of each month at 04:00 UTC.
    protected override DateTime GetNextRunUtc(DateTime now)
    {
        var next = new DateTime(now.Year, now.Month, 1, 4, 0, 0, DateTimeKind.Utc);
        return next <= now ? next.AddMonths(1) : next;
    }

    protected override async Task RunCoreAsync(CancellationToken ct)
    {
        IList<Guid> tenantIds;
        await using (var readScope = ScopeFactory.CreateAsyncScope())
        {
            var stateRepo = readScope.ServiceProvider.GetRequiredService<ITenantCloudStateRepository>();
            var cutoff    = DateTimeOffset.UtcNow.AddDays(-30);
            tenantIds = await stateRepo.Query()
                .Where(s => s.IsBlocked && s.BlockedAt < cutoff)
                .Select(s => s.TenantId)
                .ToListAsync(ct);
        }

        Logger.LogInformation("DataPurgeJob: {Count} tenants scheduled for purge.", tenantIds.Count);

        foreach (var tenantId in tenantIds)
        {
            try
            {
                await PurgeTenantAsync(tenantId, ct);
                Logger.LogInformation("DataPurgeJob: tenant {TenantId} purged.", tenantId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogError(ex, "DataPurgeJob: purge failed for tenant {TenantId}. Rolled back.", tenantId);
            }
        }
    }

    private async Task PurgeTenantAsync(Guid tenantId, CancellationToken ct)
    {
        await using var scope      = ScopeFactory.CreateAsyncScope();
        var session    = scope.ServiceProvider.GetRequiredService<ISession>();
        var uowFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();

        await using var uow = uowFactory.Create();

        // Delete in FK dependency order.
        await Exec(session, "DELETE FROM eventos          WHERE tenant_id = :id", tenantId, ct);
        await Exec(session, "DELETE FROM webhook_senders  WHERE tenant_id = :id", tenantId, ct);
        await Exec(session, "DELETE FROM urls_destino     WHERE tenant_id = :id", tenantId, ct);
        await Exec(session, "DELETE FROM api_keys         WHERE tenant_id = :id", tenantId, ct);
        await Exec(session, "DELETE FROM invite_tokens    WHERE tenant_id = :id", tenantId, ct);
        await Exec(session, "DELETE FROM users            WHERE tenant_id = :id", tenantId, ct);
        await Exec(session, "DELETE FROM usage_snapshots  WHERE tenant_id = :id", tenantId, ct);
        await Exec(session, "DELETE FROM tenant_cloud_states WHERE tenant_id = :id", tenantId, ct);
        await Exec(session, "DELETE FROM tenants          WHERE id = :id", tenantId, ct);

        await uow.CommitAsync(ct);
    }

    private static Task ExecuteUpdateAsync(ISession session, string sql, Guid tenantId, CancellationToken ct) =>
        session.CreateSQLQuery(sql)
            .SetParameter("id", tenantId)
            .ExecuteUpdateAsync(ct);

    private static Task Exec(ISession session, string sql, Guid tenantId, CancellationToken ct) =>
        ExecuteUpdateAsync(session, sql, tenantId, ct);
}
