using HookSentry.Domain.Tenants;

namespace HookSentry.Billing.TenantAccess;

// ITenantCreatedPostProcessor called by POST /api/v1/tenants.
// Creates the TenantCloudState row inside the same UoW as tenant + owner creation.
public class CreateTenantCloudStateProcessor(ITenantCloudStateRepository stateRepository)
    : ITenantCreatedPostProcessor
{
    public async Task ProcessAsync(Guid tenantId, Guid adminUserId, CancellationToken ct)
    {
        var state = new TenantCloudState(tenantId);
        await stateRepository.AddAsync(state, ct);
        // NHibernate dirty-tracking — flushed on outer UoW commit
    }
}
