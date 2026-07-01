namespace HookSentry.Subscriptions.AbuseProtection;

/// <summary>
/// Enhanced per-IP rate limit for tenant registration (stricter than the OSS 5/h outer
/// bound). Keyed by client IP with a 1-hour window; limit from
/// <see cref="RegistrationAbuseOptions.MaxRegistrationsPerIpPerHour"/>.
/// </summary>
public interface IRegistrationRateLimiter
{
    Task<bool> IsBlockedAsync(string ip, CancellationToken ct);
    Task RecordAsync(string ip, CancellationToken ct);
}
