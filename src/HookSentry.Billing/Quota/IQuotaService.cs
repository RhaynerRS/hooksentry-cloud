namespace HookSentry.Billing.Quota;

public interface IQuotaService
{
    Task<QuotaCheckResult> CheckAndIncrementEventsAsync(Guid tenantId, CancellationToken ct);
}

public record QuotaCheckResult(bool Allowed, int Current, int Limit);
