namespace HookSentry.Billing.Plans;

[Flags]
public enum PlanFeature : long
{
    None            = 0,
    Webhooks        = 1L << 0,
    RetryPolicy     = 1L << 1,
    CustomHeaders   = 1L << 2,
    Analytics       = 1L << 3,
    TeamRoles       = 1L << 4,
    SSOSaml         = 1L << 5,
    AuditLog        = 1L << 6,
    CustomRateLimit = 1L << 7,
}
