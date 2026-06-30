using HookSentry.Billing.Endpoints.GetPlans;
using HookSentry.Billing.Endpoints.TenantBlocking;
using HookSentry.Billing.Persistence;
using HookSentry.Billing.Persistence.Repositories;
using HookSentry.Billing.Plans;
using HookSentry.Billing.TenantAccess;
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
        services.AddScoped<ITenantCloudStateRepository, TenantCloudStateRepository>();
        services.AddScoped<ITenantStateCache, RedisTenantStateCache>();

        return services;
    }

    public static IApplicationBuilder UseTenantAccess(this IApplicationBuilder app) =>
        app.UseMiddleware<TenantAccessMiddleware>();

    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        new GetPlansEndpoint().MapEndpoints(app);
        new BlockTenantEndpoint().MapEndpoints(app);
        new UnblockTenantEndpoint().MapEndpoints(app);

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
