using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HookSentry.Subscriptions.AbuseProtection;

/// <summary>
/// Validates a Cloudflare Turnstile token against the siteverify API.
/// Fails closed on transport errors (returns <c>false</c>) — when Turnstile is enabled,
/// a token that cannot be verified must not pass.
/// </summary>
public sealed class CloudflareTurnstileValidator(
    HttpClient http,
    IOptions<TurnstileOptions> options,
    ILogger<CloudflareTurnstileValidator> logger) : ITurnstileValidator
{
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public async Task<bool> ValidateAsync(string token, string clientIp, CancellationToken ct)
    {
        try
        {
            var response = await http.PostAsJsonAsync(VerifyUrl, new
            {
                secret = options.Value.SecretKey,
                response = token,
                remoteip = clientIp
            }, ct);

            var result = await response.Content.ReadFromJsonAsync<TurnstileResponse>(ct);
            return result?.Success == true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Turnstile verification request failed; rejecting token.");
            return false;
        }
    }

    private record TurnstileResponse(bool Success, string[]? ErrorCodes);
}
