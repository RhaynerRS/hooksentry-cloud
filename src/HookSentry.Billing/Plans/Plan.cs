namespace HookSentry.Billing.Plans;

public class Plan
{
    public virtual Guid Id { get; protected set; }
    public virtual string Name { get; protected set; } = default!;
    public virtual string? StripePriceId { get; protected set; }
    public virtual decimal PriceMonthly { get; protected set; }
    public virtual int MaxUsers { get; protected set; }
    public virtual int MaxDestinations { get; protected set; }
    public virtual int MaxEventsPerMonth { get; protected set; }
    public virtual PlanFeature Features { get; protected set; }
    public virtual bool IsActive { get; protected set; }
    public virtual DateTimeOffset CreatedAt { get; protected set; }
    public virtual DateTimeOffset UpdatedAt { get; protected set; }

    protected Plan() { }

    public Plan(
        string name,
        string? stripePriceId,
        decimal priceMonthly,
        int maxUsers,
        int maxDestinations,
        int maxEventsPerMonth,
        PlanFeature features)
    {
        SetName(name);
        SetStripePriceId(stripePriceId);
        SetPriceMonthly(priceMonthly);
        SetMaxUsers(maxUsers);
        SetMaxDestinations(maxDestinations);
        SetMaxEventsPerMonth(maxEventsPerMonth);
        SetFeatures(features);

        Id = Guid.NewGuid();
        IsActive = true;
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        Name = name.Trim().ToLowerInvariant();
    }

    private void SetStripePriceId(string? stripePriceId)
    {
        if (stripePriceId is not null && string.IsNullOrWhiteSpace(stripePriceId))
            throw new ArgumentException("StripePriceId cannot be empty when provided.", nameof(stripePriceId));
        StripePriceId = stripePriceId;
    }

    private void SetPriceMonthly(decimal priceMonthly)
    {
        if (priceMonthly < 0)
            throw new ArgumentOutOfRangeException(nameof(priceMonthly), "PriceMonthly cannot be negative.");
        PriceMonthly = priceMonthly;
    }

    private void SetMaxUsers(int maxUsers)
    {
        if (maxUsers < -1)
            throw new ArgumentOutOfRangeException(nameof(maxUsers), "MaxUsers must be -1 (unlimited) or a positive number.");
        MaxUsers = maxUsers;
    }

    private void SetMaxDestinations(int maxDestinations)
    {
        if (maxDestinations < -1)
            throw new ArgumentOutOfRangeException(nameof(maxDestinations), "MaxDestinations must be -1 (unlimited) or a positive number.");
        MaxDestinations = maxDestinations;
    }

    private void SetMaxEventsPerMonth(int maxEventsPerMonth)
    {
        if (maxEventsPerMonth < -1)
            throw new ArgumentOutOfRangeException(nameof(maxEventsPerMonth), "MaxEventsPerMonth must be -1 (unlimited) or a positive number.");
        MaxEventsPerMonth = maxEventsPerMonth;
    }

    private void SetFeatures(PlanFeature features)
    {
        Features = features;
    }
}
