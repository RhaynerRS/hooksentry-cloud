using HookSentry.Billing.Endpoints.GetPlans;
using HookSentry.Billing.Persistence;
using HookSentry.Billing.Persistence.Repositories;
using HookSentry.Billing.Plans;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HookSentry.Billing.Extensions;

public static class BillingExtensions
{
    public static IServiceCollection AddBilling(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddHostedService<PlanSeeder>();

        return services;
    }

    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        new GetPlansEndpoint().MapEndpoints(app);

        return app;
    }

    public static void MigrateBillingDatabase(this IServiceProvider services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var loggerFactory = services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
        BillingDatabaseMigrator.Migrate(connectionString, loggerFactory);
    }
}
