namespace HookSentry.Billing.TenantAccess;

public interface ITenantStateCache
{
    Task<TenantState?> GetAsync(Guid tenantId, CancellationToken ct);
    Task InvalidateAsync(Guid tenantId, CancellationToken ct);
}
