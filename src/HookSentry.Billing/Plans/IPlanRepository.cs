using HookSentry.Domain.Repositories;

namespace HookSentry.Billing.Plans;

public interface IPlanRepository : IRepository<Plan>
{
    Task<Plan?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Plan?> GetByStripePriceIdAsync(string stripePriceId, CancellationToken ct = default);
    Task<IReadOnlyList<Plan>> GetAllActiveAsync(CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task InsertManyAsync(IEnumerable<Plan> plans, CancellationToken ct = default);
}
