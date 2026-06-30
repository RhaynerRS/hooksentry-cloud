namespace HookSentry.Billing.Notifications;

public interface ICloudNotificationService
{
    Task SendQuotaWarningAsync(string ownerEmail, int current, int limit, CancellationToken ct);
}
