using HookSentry.Billing.TenantAccess;
using HookSentry.Infrastructure.Persistence.Repositories;
using NHibernate;
using NHibernate.Linq;

namespace HookSentry.Billing.Persistence.Repositories;

public class TenantCloudStateRepository : NHibernateRepository<TenantCloudState>, ITenantCloudStateRepository
{
    public TenantCloudStateRepository(ISession session) : base(session) { }

    public async Task<TenantCloudState?> FindByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        await Session.Query<TenantCloudState>()
            .Where(s => s.TenantId == tenantId)
            .SingleOrDefaultAsync(ct);
}
