namespace HookSentry.Subscriptions.AbuseProtection;

public class RegistrationAbuseOptions
{
    public bool FingerprintEnabled { get; set; }
    public int MaxAccountsPerFingerprint { get; set; } = 3;
    public int FingerprintBlockWindowDays { get; set; } = 30;
    public bool RegistrationRateLimitEnabled { get; set; }
    public int MaxRegistrationsPerIpPerHour { get; set; } = 3;
    public bool DisposableEmailBlockEnabled { get; set; }
}
