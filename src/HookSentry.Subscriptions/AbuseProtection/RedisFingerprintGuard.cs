using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HookSentry.Subscriptions.AbuseProtection;

/// <summary>
/// Tracks how many tenants a single browser fingerprint created, using Redis.
/// Key: <c>abuse:fp:{sha256(fingerprint)}</c> (hashed to keep the raw visitorId — potential
/// PII — out of Redis). Value: a SET of tenant ids. TTL: <c>FingerprintBlockWindowDays</c>.
/// Fails open on any Redis error — fingerprint is an additional layer, never the sole gate.
/// </summary>
public sealed class RedisFingerprintGuard(
    IConnectionMultiplexer redis,
    IOptions<RegistrationAbuseOptions> options,
    ILogger<RedisFingerprintGuard> logger) : IFingerprintGuard
{
    public async Task<FingerprintCheckResult> CheckAsync(string fingerprint, CancellationToken ct)
    {
        var limit = options.Value.MaxAccountsPerFingerprint;
        try
        {
            var db = redis.GetDatabase();
            var count = (long)await db.SetLengthAsync(FpKey(fingerprint));
            return new FingerprintCheckResult(count >= limit, (int)count, limit);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fingerprint check failed; allowing registration (fail-open).");
            return new FingerprintCheckResult(false, 0, limit);
        }
    }

    public async Task RecordAsync(string fingerprint, Guid tenantId, CancellationToken ct)
    {
        try
        {
            var key = FpKey(fingerprint);
            var db = redis.GetDatabase();
            await db.SetAddAsync(key, tenantId.ToString());
            await db.KeyExpireAsync(key, TimeSpan.FromDays(options.Value.FingerprintBlockWindowDays));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fingerprint record failed; continuing (fail-open).");
        }
    }

    private static string FpKey(string fp)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fp));
        return $"abuse:fp:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
