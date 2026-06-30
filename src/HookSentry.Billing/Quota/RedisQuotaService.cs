using HookSentry.Billing.Plans;
using StackExchange.Redis;

namespace HookSentry.Billing.Quota;

public class RedisQuotaService(
    IConnectionMultiplexer redis,
    IPlanCache planCache) : IQuotaService
{
    public async Task<QuotaCheckResult> CheckAndIncrementEventsAsync(Guid tenantId, CancellationToken ct)
    {
        var plan  = await planCache.GetPlanAsync(ct);
        var key   = $"cloud:usage:{tenantId}:{DateTime.UtcNow:yyyy-MM}:events";
        var db    = redis.GetDatabase();

        var count = await db.StringIncrementAsync(key);
        if (count == 1) await db.KeyExpireAsync(key, TimeSpan.FromDays(35));

        if (count > plan.MaxEventsPerMonth)
        {
            await db.StringDecrementAsync(key);
            return new QuotaCheckResult(false, (int)count - 1, plan.MaxEventsPerMonth);
        }

        return new QuotaCheckResult(true, (int)count, plan.MaxEventsPerMonth);
    }
}
