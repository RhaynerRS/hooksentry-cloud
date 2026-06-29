using HookSentry.Api.Common.Endpoints;
using HookSentry.Billing.DataTransfer.Responses;
using HookSentry.Billing.Plans;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HookSentry.Billing.Endpoints.GetPlans;

public class GetPlansEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/cloud/plans", Handle)
            .WithName("GetPlans")
            .WithTags("Plans")
            .WithSummary("Lista os planos disponíveis")
            .WithDescription("""
                Retorna todos os planos ativos disponíveis para contratação.
                Endpoint público — não requer autenticação.

                **Códigos de retorno:**
                - `200 OK`: lista de planos com limites e features
                """)
            .Produces<IReadOnlyList<PlanResponse>>();
    }

    private static async Task<IResult> Handle(
        IPlanRepository planRepository,
        CancellationToken ct)
    {
        var plans = await planRepository.GetAllActiveAsync(ct);
        return Results.Ok(plans.Select(PlanResponse.From).ToList());
    }
}
