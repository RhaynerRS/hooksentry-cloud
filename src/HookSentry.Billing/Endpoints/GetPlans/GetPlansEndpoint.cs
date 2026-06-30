using HookSentry.Api.Common.Endpoints;
using HookSentry.Billing.DataTransfer.Responses;
using HookSentry.Billing.Plans;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NHibernate.Linq;

namespace HookSentry.Billing.Endpoints.GetPlans;

public class GetPlansEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/plans", Handle)
            .WithName("GetPlans")
            .AllowAnonymous()
            .WithSummary("Lista os planos disponíveis")
            .WithDescription("""
                Retorna todos os planos disponíveis.
                Endpoint público — não requer autenticação.

                **Códigos de retorno:**
                - `200 OK`: lista de planos com limites
                """)
            .Produces<IReadOnlyList<PlanResponse>>();
    }

    private static async Task<IResult> Handle(
        IPlanRepository planRepository,
        CancellationToken ct)
    {
        var plans = await planRepository.Query().ToListAsync(ct);
        return Results.Ok(plans.Select(PlanResponse.From).ToList());
    }
}
