using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HookSentry.Billing.Quota;

public class QuotaEnforcementMiddleware(RequestDelegate next)
{
    private static readonly PathString IngestPrefix = "/api/v1/ingest";

    public async Task InvokeAsync(
        HttpContext context,
        IQuotaService quotaService,
        ILogger<QuotaEnforcementMiddleware> logger)
    {
        if (!IsIngestRequest(context))
        {
            await next(context);
            return;
        }

        if (!TryExtractTenantId(context.Request.Path, out var tenantId))
        {
            await next(context);
            return;
        }

        var result = await quotaService.CheckAndIncrementEventsAsync(tenantId, context.RequestAborted);
        if (!result.Allowed)
        {
            logger.LogWarning(
                "Quota exceeded. TenantId={TenantId} Current={Current} Limit={Limit}",
                tenantId, result.Current, result.Limit);

            context.Response.StatusCode = 429;
            return;
        }

        await next(context);
    }

    private static bool IsIngestRequest(HttpContext context) =>
        HttpMethods.IsPost(context.Request.Method) &&
        context.Request.Path.StartsWithSegments(IngestPrefix);

    private static bool TryExtractTenantId(PathString path, out Guid tenantId)
    {
        // Path: /api/v1/ingest/{tenantId}/{token}
        if (path.StartsWithSegments(IngestPrefix, out var remaining))
        {
            var segments = remaining.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments?.Length >= 1 && Guid.TryParse(segments[0], out tenantId))
                return true;
        }

        tenantId = Guid.Empty;
        return false;
    }
}
