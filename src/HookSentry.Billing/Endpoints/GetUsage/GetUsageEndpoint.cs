using System.Security.Claims;
using HookSentry.Api.Common.Endpoints;
using HookSentry.Api.Common.Extensions;
using HookSentry.Billing.DataTransfer.Responses;
using HookSentry.Billing.Plans;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StackExchange.Redis;

namespace HookSentry.Billing.Endpoints.GetUsage;

public class GetUsageEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/cloud/usage", Handle)
            .WithName("GetUsage")
            .WithTags("Usage")
            .WithSummary("Retorna o consumo de eventos do tenant no mês atual")
            .WithDescription("""
                Retorna as métricas de consumo do tenant autenticado para o período atual (mês corrente).

                **Resposta:**
                - `period`: mês de referência no formato `yyyy-MM`
                - `usage.events.current`: número de eventos ingeridos no mês
                - `usage.events.limit`: limite mensal de eventos do plano
                - `usage.events.percentage`: percentual de uso (0–100)
                - `usage.events.warning`: `true` quando `percentage >= 80`

                **Códigos de retorno:**
                - `200 OK`: consumo retornado com sucesso
                - `401 Unauthorized`: token ausente ou inválido
                """)
            .RequireAuthorization()
            .Produces<UsageResponse>()
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal principal,
        IConnectionMultiplexer redis,
        IPlanCache planCache,
        CancellationToken ct)
    {
        if (principal.RequireTenantId(out var tenantId) is { } err) return err;

        var period = DateTime.UtcNow.ToString("yyyy-MM");
        var key    = $"cloud:usage:{tenantId}:{period}:events";

        var db      = redis.GetDatabase();
        var raw     = await db.StringGetAsync(key);
        var current = raw.HasValue ? (int)(long)raw : 0;

        var plan       = await planCache.GetPlanAsync(ct);
        var percentage = plan.MaxEventsPerMonth > 0
            ? (int)Math.Round((double)current / plan.MaxEventsPerMonth * 100)
            : 0;

        var response = new UsageResponse(
            period,
            new UsageDetail(
                new EventUsage(current, plan.MaxEventsPerMonth, percentage, percentage >= 80)));

        return Results.Ok(response);
    }
}
