using HookSentry.Api.Common.Endpoints;
using HookSentry.Billing.TenantAccess;
using HookSentry.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace HookSentry.Billing.Endpoints.TenantBlocking;

public record BlockTenantRequest(string Reason);

public class BlockTenantEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/internal/tenants/{tenantId:guid}/block", Handle)
            .WithName("BlockTenant")
            .WithTags("TenantBlocking")
            .WithSummary("Bloqueia um tenant")
            .WithDescription("""
                Bloqueia o acesso de um tenant à API. Persiste no Postgres e invalida o cache Redis.
                Cria o registro `TenantCloudState` se não existir (upsert).
                TODO: Requer autenticação de serviço interno (fora do escopo do MVP).

                **Parâmetros de rota:**
                - `tenantId` *(obrigatório)*: UUID do tenant a bloquear

                **Body:**
                - `reason` *(obrigatório)*: motivo — `abuse`, `manual` ou `quota_abuse`

                **Códigos de retorno:**
                - `204 No Content`: tenant bloqueado com sucesso
                - `400 Bad Request`: motivo inválido
                """)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<string>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> Handle(
        Guid tenantId,
        BlockTenantRequest request,
        ITenantCloudStateRepository stateRepository,
        ITenantStateCache stateCache,
        IUnitOfWorkFactory uowFactory,
        ILogger<BlockTenantEndpoint> logger,
        CancellationToken ct)
    {
        await using var uow = uowFactory.Create();

        var state = await stateRepository.FindByTenantAsync(tenantId, ct);
        bool isNew = state is null;

        if (isNew)
            state = new TenantCloudState(tenantId);

        try { state!.Block(request.Reason); }
        catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }

        if (isNew)
            await stateRepository.AddAsync(state!, ct);

        await uow.CommitAsync(ct);
        await stateCache.InvalidateAsync(tenantId, ct);

        logger.LogInformation(
            "Tenant bloqueado. TenantId={TenantId} Reason={Reason}",
            tenantId, request.Reason);

        return Results.NoContent();
    }
}
