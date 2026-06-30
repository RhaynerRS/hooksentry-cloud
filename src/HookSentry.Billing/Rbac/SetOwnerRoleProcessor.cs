using HookSentry.Domain.Tenants;
using HookSentry.Domain.Users;

namespace HookSentry.Billing.Rbac;

// ITenantCreatedPostProcessor is an OSS extension point called by POST /api/v1/tenants
// after the first admin user is created. This implementation upgrades that user to Owner.
public class SetOwnerRoleProcessor(IUserRepository users) : ITenantCreatedPostProcessor
{
    public async Task ProcessAsync(Guid tenantId, Guid adminUserId, CancellationToken ct)
    {
        var user = await users.FindAsync(adminUserId, ct);
        if (user is null) return;
        user.SetRole((UserRole)CloudRoles.Owner);
        // NHibernate dirty-tracking — persisted on outer UoW commit
    }
}
