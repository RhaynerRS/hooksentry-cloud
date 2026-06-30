namespace HookSentry.Billing.Plans;

public class Plan
{
    public virtual Guid Id { get; protected set; }
    public virtual string Name { get; protected set; } = default!;
    public virtual int MaxUsers { get; protected set; }
    public virtual int MaxDestinations { get; protected set; }
    public virtual int MaxEventsPerMonth { get; protected set; }
    public virtual int RetentionDays { get; protected set; }
    public virtual DateTimeOffset CreatedAt { get; protected set; }
    public virtual DateTimeOffset UpdatedAt { get; protected set; }

    protected Plan() { }

    public Plan(string name, int maxUsers, int maxDestinations, int maxEventsPerMonth, int retentionDays)
    {
        SetName(name);
        SetMaxUsers(maxUsers);
        SetMaxDestinations(maxDestinations);
        SetMaxEventsPerMonth(maxEventsPerMonth);
        SetRetentionDays(retentionDays);

        Id = Guid.NewGuid();
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        Name = name.Trim().ToLowerInvariant();
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

    private void SetRetentionDays(int retentionDays)
    {
        if (retentionDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(retentionDays), "RetentionDays must be a positive number.");
        RetentionDays = retentionDays;
    }
}
