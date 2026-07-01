using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HookSentry.Subscriptions.AbuseProtection;

/// <summary>
/// Redis-backed per-IP registration rate limiter. Key: <c>abuse:regip:{ip}</c>, TTL 1 hour.
/// Fails open on any Redis error — never blocks a legitimate registration on infra failure.
/// </summary>
public sealed class RedisRegistrationRateLimiter(
    IConnectionMultiplexer redis,
    IOptions<RegistrationAbuseOptions> options,
    ILogger<RedisRegistrationRateLimiter> logger) : IRegistrationRateLimiter
{
    public async Task<bool> IsBlockedAsync(string ip, CancellationToken ct)
    {
        try
        {
            var value = await redis.GetDatabase().StringGetAsync(Key(ip));
            return value.HasValue && (long)value >= options.Value.MaxRegistrationsPerIpPerHour;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Registration IP rate-limit check failed; allowing (fail-open).");
            return false;
        }
    }

    public async Task RecordAsync(string ip, CancellationToken ct)
    {
        try
        {
            var db = redis.GetDatabase();
            var key = Key(ip);
            var count = await db.StringIncrementAsync(key);
            if (count == 1)
                await db.KeyExpireAsync(key, TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Registration IP rate-limit record failed; continuing.");
        }
    }

    private static string Key(string ip) => $"abuse:regip:{ip}";
}
