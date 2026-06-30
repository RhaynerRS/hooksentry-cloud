namespace HookSentry.Billing.TenantAccess;

public record TenantState(bool IsBlocked, string? BlockReason);
