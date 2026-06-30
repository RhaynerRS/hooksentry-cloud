using HookSentry.Billing.Plans;
using HookSentry.Infrastructure.Persistence.Repositories;
using NHibernate;
using NHibernate.Linq;

namespace HookSentry.Billing.Persistence.Repositories;

public class PlanRepository : NHibernateRepository<Plan>, IPlanRepository
{
    public PlanRepository(ISession session) : base(session) { }

    public async Task<Plan?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await Session.Query<Plan>()
            .Where(p => p.Name == name)
            .SingleOrDefaultAsync(ct);

    public async Task<bool> AnyAsync(CancellationToken ct = default) =>
        await Session.Query<Plan>().AnyAsync(ct);
}
