namespace HookSentry.Subscriptions.AbuseProtection;

public sealed class TurnstileOptions
{
    public bool Enabled { get; set; }
    public string SecretKey { get; set; } = "";
    public string SiteKey { get; set; } = "";
}
