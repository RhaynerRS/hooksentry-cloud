using HookSentry.Domain.Repositories;

namespace HookSentry.Billing.Plans;

public interface IPlanRepository : IRepository<Plan>
{
    Task<Plan?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<bool>  AnyAsync(CancellationToken ct = default);
}
