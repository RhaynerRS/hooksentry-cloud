namespace HookSentry.Billing.Jobs;

public class EventRetentionOptions
{
    public bool Enabled { get; set; } = true;
    public int RetentionDays { get; set; } = 7;
}
