using HookSentry.Api.Common.Endpoints;
using HookSentry.Billing.TenantAccess;
using HookSentry.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace HookSentry.Billing.Endpoints.TenantBlocking;

public class UnblockTenantEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/internal/tenants/{tenantId:guid}/unblock", Handle)
            .WithName("UnblockTenant")
            .WithTags("TenantBlocking")
            .WithSummary("Desbloqueia um tenant")
            .WithDescription("""
                Remove o bloqueio de acesso de um tenant à API. Persiste no Postgres e invalida o cache Redis.
                TODO: Requer autenticação de serviço interno (fora do escopo do MVP).

                **Parâmetros de rota:**
                - `tenantId` *(obrigatório)*: UUID do tenant a desbloquear

                **Códigos de retorno:**
                - `204 No Content`: tenant desbloqueado com sucesso
                - `404 Not Found`: nenhum registro de bloqueio encontrado para o tenant
                """)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid tenantId,
        ITenantCloudStateRepository stateRepository,
        ITenantStateCache stateCache,
        IUnitOfWorkFactory uowFactory,
        ILogger<UnblockTenantEndpoint> logger,
        CancellationToken ct)
    {
        await using var uow = uowFactory.Create();

        var state = await stateRepository.FindByTenantAsync(tenantId, ct);
        if (state is null)
            return Results.NotFound();

        state.Unblock();
        await uow.CommitAsync(ct);
        await stateCache.InvalidateAsync(tenantId, ct);

        logger.LogInformation("Tenant desbloqueado. TenantId={TenantId}", tenantId);

        return Results.NoContent();
    }
}
