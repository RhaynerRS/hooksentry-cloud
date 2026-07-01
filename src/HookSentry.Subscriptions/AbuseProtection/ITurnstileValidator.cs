namespace HookSentry.Subscriptions.AbuseProtection;

public interface ITurnstileValidator
{
    Task<bool> ValidateAsync(string token, string clientIp, CancellationToken ct);
}
