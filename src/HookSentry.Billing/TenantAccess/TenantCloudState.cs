namespace HookSentry.Billing.TenantAccess;

public class TenantCloudState
{
    public virtual Guid Id { get; protected set; }
    public virtual Guid TenantId { get; protected set; }
    public virtual bool IsBlocked { get; protected set; }
    public virtual DateTimeOffset? BlockedAt { get; protected set; }
    public virtual string? BlockReason { get; protected set; }

    protected TenantCloudState() { }

    public TenantCloudState(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        TenantId = tenantId;
        Id = Guid.NewGuid();
    }

    public virtual void Block(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Block reason cannot be empty.", nameof(reason));
        IsBlocked = true;
        BlockedAt = DateTimeOffset.UtcNow;
        BlockReason = reason.Trim();
    }

    public virtual void Unblock()
    {
        IsBlocked = false;
        BlockedAt = null;
        BlockReason = null;
    }
}
