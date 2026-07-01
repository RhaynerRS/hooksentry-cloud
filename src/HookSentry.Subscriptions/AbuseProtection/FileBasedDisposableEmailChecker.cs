using Microsoft.AspNetCore.Hosting;

namespace HookSentry.Subscriptions.AbuseProtection;

/// <summary>
/// Blocks disposable/temporary email domains using a bundled blocklist
/// (<c>Resources/disposable-domains.txt</c>, updated in CI). Missing file → fail-open
/// (returns <c>false</c>): the blocklist is an extra layer, never the sole gate.
/// </summary>
public sealed class FileBasedDisposableEmailChecker : IDisposableEmailChecker
{
    private readonly HashSet<string> _blocklist;

    public FileBasedDisposableEmailChecker(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "Resources", "disposable-domains.txt");
        _blocklist = File.Exists(path)
            ? File.ReadAllLines(path)
                  .Select(l => l.Trim().ToLowerInvariant())
                  .Where(l => l.Length > 0 && !l.StartsWith('#'))
                  .ToHashSet()
            : [];
    }

    public Task<bool> IsDisposableAsync(string email, CancellationToken ct)
    {
        var domain = email.Split('@').LastOrDefault()?.Trim().ToLowerInvariant() ?? "";
        return Task.FromResult(domain.Length > 0 && _blocklist.Contains(domain));
    }
}
