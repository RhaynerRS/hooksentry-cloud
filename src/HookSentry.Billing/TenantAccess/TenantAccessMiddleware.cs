using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HookSentry.Billing.TenantAccess;

public class TenantAccessMiddleware(RequestDelegate next)
{
    private static readonly string[] ExemptPrefixes = ["/health", "/metrics", "/swagger"];

    public async Task InvokeAsync(
        HttpContext context,
        ITenantStateCache stateCache,
        ILogger<TenantAccessMiddleware> logger)
    {
        if (IsExempt(context.Request.Path))
        {
            await next(context);
            return;
        }

        var claim = context.User.FindFirst("tenant_id");
        if (claim is null || !Guid.TryParse(claim.Value, out var tenantId))
        {
            await next(context);
            return;
        }

        var state = await stateCache.GetAsync(tenantId, context.RequestAborted);
        if (state?.IsBlocked == true)
        {
            logger.LogWarning(
                "Blocked tenant attempted access. TenantId={TenantId} Reason={Reason}",
                tenantId, state.BlockReason);

            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                error  = "tenant_blocked",
                reason = state.BlockReason
            }, context.RequestAborted);
            return;
        }

        await next(context);
    }

    private static bool IsExempt(PathString path) =>
        ExemptPrefixes.Any(p => path.StartsWithSegments(p));
}
