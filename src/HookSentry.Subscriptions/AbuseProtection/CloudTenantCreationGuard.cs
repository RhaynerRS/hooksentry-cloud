using HookSentry.Api.Common.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace HookSentry.Subscriptions.AbuseProtection;

/// <summary>
/// Cloud implementation of the OSS <see cref="ITenantCreationGuard"/> extension point.
/// Layers the anti-abuse checks around tenant registration, in the order defined by
/// spec-anti-abuse-registration-12: enhanced IP rate limit → disposable email →
/// device fingerprint → Cloudflare Turnstile. Each layer is independently feature-flagged;
/// with every flag off this guard is a no-op and self-hosted behavior is unchanged.
/// </summary>
public sealed class CloudTenantCreationGuard(
    IRegistrationRateLimiter rateLimiter,
    IDisposableEmailChecker emailChecker,
    IFingerprintGuard fingerprintGuard,
    ITurnstileValidator turnstileValidator,
    IOptions<RegistrationAbuseOptions> abuseOptions,
    IOptions<TurnstileOptions> turnstileOptions) : ITenantCreationGuard
{
    public async Task<IResult?> CheckAsync(TenantCreationContext ctx, CancellationToken ct)
    {
        var opts = abuseOptions.Value;

        // 1. Enhanced per-IP rate limit (stricter than the OSS 5/h outer bound).
        if (opts.RegistrationRateLimitEnabled && await rateLimiter.IsBlockedAsync(ctx.ClientIp, ct))
            return Results.Json(
                new { error = "too_many_requests", retry_after_seconds = 3600 },
                statusCode: StatusCodes.Status429TooManyRequests);

        // 2. Disposable email domains.
        if (opts.DisposableEmailBlockEnabled && await emailChecker.IsDisposableAsync(ctx.OwnerEmail, ct))
            return Results.Json(
                new { error = "disposable_email", message = "Please use a permanent email address." },
                statusCode: StatusCodes.Status422UnprocessableEntity);

        // 3. Device fingerprint (same 429 as rate limit — never reveal which layer blocked).
        if (opts.FingerprintEnabled && !string.IsNullOrWhiteSpace(ctx.DeviceFingerprint))
        {
            var fp = await fingerprintGuard.CheckAsync(ctx.DeviceFingerprint, ct);
            if (fp.Blocked)
                return Results.Json(
                    new { error = "too_many_requests" },
                    statusCode: StatusCodes.Status429TooManyRequests);
        }

        // 4. Cloudflare Turnstile.
        if (turnstileOptions.Value.Enabled)
        {
            if (string.IsNullOrWhiteSpace(ctx.CfTurnstileToken) ||
                !await turnstileValidator.ValidateAsync(ctx.CfTurnstileToken, ctx.ClientIp, ct))
                return Results.Json(
                    new { error = "invalid_turnstile", message = "Captcha verification failed." },
                    statusCode: StatusCodes.Status400BadRequest);
        }

        return null;
    }

    public async Task RecordAsync(TenantCreationContext ctx, Guid tenantId, CancellationToken ct)
    {
        var opts = abuseOptions.Value;

        if (opts.RegistrationRateLimitEnabled)
            await rateLimiter.RecordAsync(ctx.ClientIp, ct);

        if (opts.FingerprintEnabled && !string.IsNullOrWhiteSpace(ctx.DeviceFingerprint))
            await fingerprintGuard.RecordAsync(ctx.DeviceFingerprint, tenantId, ct);
    }
}
