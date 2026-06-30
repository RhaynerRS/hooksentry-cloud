using HookSentry.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HookSentry.Billing.Plans;

public class PlanSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<PlanSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var plans = scope.ServiceProvider.GetRequiredService<IPlanRepository>();
        var uowFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();

        if (await plans.AnyAsync(ct)) return;

        var free = new Plan(
            name:               "free",
            maxUsers:           -1,
            maxDestinations:    -1,
            maxEventsPerMonth:  1_000,
            retentionDays:      7);

        await using var uow = uowFactory.Create();
        await plans.AddAsync(free, ct);
        await uow.CommitAsync(ct);

        logger.LogInformation("Free plan seeded.");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
