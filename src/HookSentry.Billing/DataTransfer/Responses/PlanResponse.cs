using HookSentry.Billing.Plans;

namespace HookSentry.Billing.DataTransfer.Responses;

public record PlanLimits(int MaxEventsPerMonth, int RetentionDays);

public record PlanResponse(
    string Name,
    decimal PriceMonthly,
    bool AlwaysFree,
    PlanLimits Limits)
{
    public static PlanResponse From(Plan plan) => new(
        plan.Name,
        0.00m,
        true,
        new PlanLimits(plan.MaxEventsPerMonth, plan.RetentionDays));
}
