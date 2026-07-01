using System.Text.Json;
using StackExchange.Redis;

namespace HookSentry.Billing.TenantAccess;

public class RedisTenantStateCache(
    IConnectionMultiplexer redis,
    ITenantCloudStateRepository repository) : ITenantStateCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private const string KeyPrefix = "cloud:tenant-state:";

    public async Task<TenantState?> GetAsync(Guid tenantId, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var key = KeyPrefix + tenantId;

        var cached = await db.StringGetAsync(key);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<TenantState>((string)cached!);

        // Cache miss — load from DB; absent record = active (not blocked)
        var state = await repository.FindByTenantAsync(tenantId, ct);
        var tenantState = state is null
            ? new TenantState(false, null)
            : new TenantState(state.IsBlocked, state.BlockReason);

        await db.StringSetAsync(key, JsonSerializer.Serialize(tenantState), Ttl);
        return tenantState;
    }

    public async Task InvalidateAsync(Guid tenantId, CancellationToken ct)
    {
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync(KeyPrefix + tenantId);
    }
}
