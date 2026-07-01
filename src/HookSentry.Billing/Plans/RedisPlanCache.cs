using System.Text.Json;
using StackExchange.Redis;

namespace HookSentry.Billing.Plans;

public class RedisPlanCache(
    IConnectionMultiplexer redis,
    IPlanRepository planRepository) : IPlanCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private const string CacheKey = "cloud:plan:free";

    public async Task<PlanLimits> GetPlanAsync(CancellationToken ct)
    {
        var db = redis.GetDatabase();

        var cached = await db.StringGetAsync(CacheKey);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<PlanLimits>((string)cached!)!;

        var plan = await planRepository.GetByNameAsync("free", ct)
            ?? throw new InvalidOperationException("Free plan not found in database.");

        var limits = new PlanLimits(plan.MaxEventsPerMonth, plan.RetentionDays);
        await db.StringSetAsync(CacheKey, JsonSerializer.Serialize(limits), Ttl);

        return limits;
    }
}
