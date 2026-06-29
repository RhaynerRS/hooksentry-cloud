using HookSentry.Billing.Plans;

namespace HookSentry.Billing.DataTransfer.Responses;

public record PlanLimits(int MaxUsers, int MaxDestinations, int MaxEventsPerMonth);

public record PlanResponse(
    Guid Id,
    string Name,
    decimal? PriceMonthly,
    PlanLimits Limits,
    IReadOnlyList<string> Features)
{
    public static PlanResponse From(Plan plan) => new(
        plan.Id,
        plan.Name,
        plan.Name == "enterprise" ? null : plan.PriceMonthly,
        new PlanLimits(plan.MaxUsers, plan.MaxDestinations, plan.MaxEventsPerMonth),
        Enum.GetValues<PlanFeature>()
            .Where(f => f != PlanFeature.None && plan.Features.HasFlag(f))
            .Select(f => f.ToString())
            .ToList());
}
