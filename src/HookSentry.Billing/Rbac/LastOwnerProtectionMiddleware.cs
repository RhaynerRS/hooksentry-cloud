using HookSentry.Domain.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NHibernate.Linq;

namespace HookSentry.Billing.Rbac;

public class LastOwnerProtectionMiddleware(RequestDelegate next)
{
    private static readonly PathString UsersPrefix = "/api/v1/users";

    public async Task InvokeAsync(
        HttpContext context,
        IUserRepository userRepository,
        ILogger<LastOwnerProtectionMiddleware> logger)
    {
        var ct = context.RequestAborted;

        if (!IsUserDeleteRequest(context) || !TryExtractUserId(context.Request.Path, out var userId))
        {
            await next(context);
            return;
        }

        var tenantClaim = context.User.FindFirst("tenant_id");
        if (tenantClaim is null || !Guid.TryParse(tenantClaim.Value, out var tenantId))
        {
            await next(context);
            return;
        }

        var user = await userRepository.FindAsync(userId, ct);
        if (user is null || user.TenantId != tenantId || (int)user.Role != CloudRoles.Owner)
        {
            await next(context);
            return;
        }

        var ownerCount = await userRepository.Query()
            .Where(u => u.TenantId == tenantId && u.Role == (UserRole)CloudRoles.Owner)
            .CountAsync(ct);

        if (ownerCount <= 1)
        {
            logger.LogWarning(
                "Attempt to remove last owner. TenantId={TenantId} UserId={UserId}",
                tenantId, userId);

            context.Response.StatusCode = 409;
            await context.Response.WriteAsJsonAsync(new { error = "last_owner" }, ct);
            return;
        }

        await next(context);
    }

    private static bool IsUserDeleteRequest(HttpContext context) =>
        HttpMethods.IsDelete(context.Request.Method) &&
        context.Request.Path.StartsWithSegments(UsersPrefix);

    private static bool TryExtractUserId(PathString path, out Guid userId)
    {
        // Path: /api/v1/users/{id}
        if (path.StartsWithSegments(UsersPrefix, out var remaining))
        {
            var segments = remaining.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments?.Length == 1 && Guid.TryParse(segments[0], out userId))
                return true;
        }

        userId = Guid.Empty;
        return false;
    }
}
