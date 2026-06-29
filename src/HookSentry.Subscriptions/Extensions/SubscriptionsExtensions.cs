using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HookSentry.Subscriptions.Extensions;

public static class SubscriptionsExtensions
{
    public static IServiceCollection AddSubscriptions(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO: register subscription plan checker, usage limit enforcer, feature gate service
        return services;
    }
}
