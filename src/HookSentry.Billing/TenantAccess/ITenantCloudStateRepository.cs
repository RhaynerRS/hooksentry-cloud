using HookSentry.Domain.Repositories;

namespace HookSentry.Billing.TenantAccess;

public interface ITenantCloudStateRepository : IRepository<TenantCloudState>
{
    Task<TenantCloudState?> FindByTenantAsync(Guid tenantId, CancellationToken ct = default);
}
