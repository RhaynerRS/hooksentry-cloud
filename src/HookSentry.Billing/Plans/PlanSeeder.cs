using HookSentry.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HookSentry.Billing.Plans;

public class PlanSeeder(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<PlanSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var plans = scope.ServiceProvider.GetRequiredService<IPlanRepository>();
        var uowFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();

        if (await plans.AnyAsync(ct)) return;

        var seeds = new[]
        {
            new Plan("free",       config["Stripe:Prices:Free"],    0m,  3,  5,   10_000,    PlanFeature.Webhooks),
            new Plan("starter",    config["Stripe:Prices:Starter"], 29m, 10, 20,  100_000,   PlanFeature.Webhooks | PlanFeature.RetryPolicy),
            new Plan("pro",        config["Stripe:Prices:Pro"],     99m, 50, 100, 1_000_000, PlanFeature.Webhooks | PlanFeature.RetryPolicy | PlanFeature.CustomHeaders | PlanFeature.Analytics | PlanFeature.TeamRoles),
            new Plan("enterprise", null,                            0m,  -1, -1,  -1,        PlanFeature.Webhooks | PlanFeature.RetryPolicy | PlanFeature.CustomHeaders | PlanFeature.Analytics | PlanFeature.TeamRoles | PlanFeature.SSOSaml | PlanFeature.AuditLog | PlanFeature.CustomRateLimit),
        };

        try
        {
            await using var uow = uowFactory.Create();
            await plans.InsertManyAsync(seeds, ct);
            await uow.CommitAsync(ct);

            logger.LogInformation("Plans seeded successfully.");
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            logger.LogInformation("Plans already seeded by another instance.");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private static bool IsUniqueViolation(Exception ex) =>
        ex is NpgsqlException { SqlState: "23505" }
        || ex.InnerException is NpgsqlException { SqlState: "23505" };
}
