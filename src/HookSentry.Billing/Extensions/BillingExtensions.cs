using HookSentry.Billing.Endpoints.GetPlans;
using HookSentry.Billing.Endpoints.GetUsage;
using HookSentry.Billing.Endpoints.TenantBlocking;
using HookSentry.Billing.Persistence;
using HookSentry.Billing.Persistence.Repositories;
using HookSentry.Billing.Plans;
using HookSentry.Billing.Quota;
using HookSentry.Billing.Rbac;
using HookSentry.Billing.TenantAccess;
using HookSentry.Domain.Tenants;
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
        services.AddScoped<IPlanCache, RedisPlanCache>();
        services.AddHostedService<PlanSeeder>();
        services.AddScoped<ITenantCloudStateRepository, TenantCloudStateRepository>();
        services.AddScoped<ITenantStateCache, RedisTenantStateCache>();
        services.AddScoped<IQuotaService, RedisQuotaService>();
        services.AddScoped<ITenantCreatedPostProcessor, SetOwnerRoleProcessor>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(CloudPolicies.RequiresOwner, policy =>
                policy.RequireClaim("role", CloudRoles.Owner.ToString()));

            options.AddPolicy(CloudPolicies.RequiresManagement, policy =>
                policy.RequireClaim("role",
                    CloudRoles.Owner.ToString(),
                    CloudRoles.Admin.ToString()));

            options.AddPolicy(CloudPolicies.RequiresReadAccess, policy =>
                policy.RequireClaim("role",
                    CloudRoles.Owner.ToString(),
                    CloudRoles.Admin.ToString(),
                    CloudRoles.Developer.ToString(),
                    CloudRoles.Viewer.ToString()));
        });

        return services;
    }

    public static IApplicationBuilder UseTenantAccess(this IApplicationBuilder app) =>
        app.UseMiddleware<TenantAccessMiddleware>();

    public static IApplicationBuilder UseQuotaEnforcement(this IApplicationBuilder app) =>
        app.UseMiddleware<QuotaEnforcementMiddleware>();

    public static IApplicationBuilder UseLastOwnerProtection(this IApplicationBuilder app) =>
        app.UseMiddleware<LastOwnerProtectionMiddleware>();

    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        new GetPlansEndpoint().MapEndpoints(app);
        new GetUsageEndpoint().MapEndpoints(app);
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
