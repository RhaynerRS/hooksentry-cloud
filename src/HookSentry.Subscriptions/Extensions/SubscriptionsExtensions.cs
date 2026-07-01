using HookSentry.Api.Common.Tenants;
using HookSentry.Subscriptions.AbuseProtection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HookSentry.Subscriptions.Extensions;

public static class SubscriptionsExtensions
{
    public static IServiceCollection AddSubscriptions(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO: register subscription plan checker, usage limit enforcer, feature gate service
        return services;
    }

    public static IServiceCollection AddAbuseProtection(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RegistrationAbuseOptions>(
            configuration.GetSection("CloudProtection:RegistrationAbuse"));
        services.Configure<TurnstileOptions>(
            configuration.GetSection("CloudProtection:Turnstile"));

        services.AddSingleton<IDisposableEmailChecker, FileBasedDisposableEmailChecker>();
        services.AddSingleton<IFingerprintGuard, RedisFingerprintGuard>();
        services.AddSingleton<IRegistrationRateLimiter, RedisRegistrationRateLimiter>();
        services.AddHttpClient<ITurnstileValidator, CloudflareTurnstileValidator>();

        // The OSS CreateTenantEndpoint discovers guards via IEnumerable<ITenantCreationGuard>.
        services.AddScoped<ITenantCreationGuard, CloudTenantCreationGuard>();

        return services;
    }

    /// <summary>
    /// Public, unauthenticated endpoint the site queries before rendering the registration
    /// form, so it can decide whether to collect a fingerprint / render the Turnstile widget.
    /// Only mapped by the cloud host — absent (404) on self-hosted.
    /// </summary>
    public static IEndpointRouteBuilder MapAbuseEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/cloud/registration-capabilities", (
                IOptions<RegistrationAbuseOptions> abuse,
                IOptions<TurnstileOptions> turnstile) =>
            Results.Ok(new
            {
                fingerprintEnabled = abuse.Value.FingerprintEnabled,
                turnstileEnabled = turnstile.Value.Enabled,
                turnstileSiteKey = turnstile.Value.Enabled ? turnstile.Value.SiteKey : null
            }))
            .AllowAnonymous()
            .WithName("GetRegistrationCapabilities")
            .WithTags("Cloud")
            .WithSummary("Returns which anti-abuse widgets the registration form should render")
            .WithDescription("""
                Public endpoint (no authentication). The site calls this before showing the
                registration form to decide whether to collect a device fingerprint and/or
                render the Cloudflare Turnstile widget.

                **Response:**
                - `fingerprintEnabled`: collect a device fingerprint on submit
                - `turnstileEnabled`: render the Turnstile widget and require a token
                - `turnstileSiteKey`: public Turnstile site key (null when disabled)
                """);

        return app;
    }
}
