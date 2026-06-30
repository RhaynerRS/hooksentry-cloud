namespace HookSentry.Billing.DataTransfer.Responses;

public record EventUsage(int Current, int Limit, int Percentage, bool Warning);

public record UsageDetail(EventUsage Events);

public record UsageResponse(string Period, UsageDetail Usage);
