namespace HookSentry.Subscriptions.AbuseProtection;

public interface IDisposableEmailChecker
{
    Task<bool> IsDisposableAsync(string email, CancellationToken ct);
}
