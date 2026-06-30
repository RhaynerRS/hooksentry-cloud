using Microsoft.Extensions.Logging;

namespace HookSentry.Billing.Notifications;

public class LogOnlyNotificationService(ILogger<LogOnlyNotificationService> logger) : ICloudNotificationService
{
    public Task SendQuotaWarningAsync(string ownerEmail, int current, int limit, CancellationToken ct)
    {
        logger.LogWarning(
            "QUOTA WARNING: {Email} — {Current}/{Limit} events ({Pct}%) this month.",
            ownerEmail, current, limit, current * 100 / limit);
        return Task.CompletedTask;
    }
}
