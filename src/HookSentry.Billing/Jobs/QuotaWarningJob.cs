using HookSentry.Billing.Notifications;
using HookSentry.Billing.Plans;
using HookSentry.Billing.Rbac;
using HookSentry.Domain.Tenants;
using HookSentry.Domain.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NHibernate.Linq;
using StackExchange.Redis;

namespace HookSentry.Billing.Jobs;

public class QuotaWarningJob(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    ILogger<QuotaWarningJob> logger)
    : ScheduledJob(scopeFactory, redis, logger)
{
    protected override string JobName => "quota-warning";

    protected override DateTime GetNextRunUtc(DateTime now)
    {
        var next = new DateTime(now.Year, now.Month, now.Day, 9, 0, 0, DateTimeKind.Utc);
        return next <= now ? next.AddDays(1) : next;
    }

    protected override async Task RunCoreAsync(CancellationToken ct)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();
        var tenantRepo    = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        var userRepo      = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var planCache     = scope.ServiceProvider.GetRequiredService<IPlanCache>();
        var notifications = scope.ServiceProvider.GetRequiredService<ICloudNotificationService>();

        var tenantIds = await tenantRepo.Query()
            .Select(t => t.Id)
            .ToListAsync(ct);

        var plan      = await planCache.GetPlanAsync(ct);
        var threshold = (long)(plan.MaxEventsPerMonth * 0.8);
        var period    = DateTime.UtcNow.ToString("yyyy-MM");
        var db        = Redis.GetDatabase();

        var warned = 0;

        foreach (var tenantId in tenantIds)
        {
            var key   = $"cloud:usage:{tenantId}:{period}:events";
            var raw   = await db.StringGetAsync(key);
            var count = raw.HasValue ? (long)raw : 0L;

            if (count < threshold) continue;

            var antiSpamKey = $"cloud:quota-warned:{tenantId}:{period}";
            var added = await db.StringSetAsync(antiSpamKey, "1", TimeSpan.FromDays(35), When.NotExists);

            if (!added) continue;

            var ownerEmails = await userRepo.Query()
                .Where(u => u.TenantId == tenantId && u.Role == UserRole.Owner)
                .Select(u => u.Email)
                .ToListAsync(ct);

            foreach (var email in ownerEmails)
            {
                await notifications.SendQuotaWarningAsync(email, (int)count, plan.MaxEventsPerMonth, ct);
                warned++;
            }
        }

        Logger.LogInformation(
            "QuotaWarningJob: sent {Count} warnings for period {Period}.",
            warned, period);
    }
}
