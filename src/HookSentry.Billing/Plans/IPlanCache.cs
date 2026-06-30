namespace HookSentry.Billing.Plans;

public record PlanLimits(int MaxEventsPerMonth, int RetentionDays);

public interface IPlanCache
{
    Task<PlanLimits> GetPlanAsync(CancellationToken ct);
}
