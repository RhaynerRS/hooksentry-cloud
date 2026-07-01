using HookSentry.Api.Common.Tenants;
using HookSentry.Subscriptions.AbuseProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace HookSentry.Subscriptions.Tests.AbuseProtection;

public class CloudTenantCreationGuardTests
{
    // ─── Test doubles ──────────────────────────────────────────────────────────
    private sealed class FakeRateLimiter(bool blocked) : IRegistrationRateLimiter
    {
        public int Records { get; private set; }
        public Task<bool> IsBlockedAsync(string ip, CancellationToken ct) => Task.FromResult(blocked);
        public Task RecordAsync(string ip, CancellationToken ct) { Records++; return Task.CompletedTask; }
    }

    private sealed class FakeEmailChecker(bool disposable) : IDisposableEmailChecker
    {
        public Task<bool> IsDisposableAsync(string email, CancellationToken ct) => Task.FromResult(disposable);
    }

    private sealed class FakeFingerprintGuard(bool blocked) : IFingerprintGuard
    {
        public int Records { get; private set; }
        public Task<FingerprintCheckResult> CheckAsync(string fingerprint, CancellationToken ct) =>
            Task.FromResult(new FingerprintCheckResult(blocked, blocked ? 3 : 0, 3));
        public Task RecordAsync(string fingerprint, Guid tenantId, CancellationToken ct) { Records++; return Task.CompletedTask; }
    }

    private sealed class FakeTurnstile(bool valid) : ITurnstileValidator
    {
        public Task<bool> ValidateAsync(string token, string clientIp, CancellationToken ct) => Task.FromResult(valid);
    }

    private static CloudTenantCreationGuard Build(
        RegistrationAbuseOptions abuse,
        TurnstileOptions turnstile,
        FakeRateLimiter? rate = null,
        FakeEmailChecker? email = null,
        FakeFingerprintGuard? fp = null,
        FakeTurnstile? ts = null) =>
        new(
            rate ?? new FakeRateLimiter(false),
            email ?? new FakeEmailChecker(false),
            fp ?? new FakeFingerprintGuard(false),
            ts ?? new FakeTurnstile(true),
            Options.Create(abuse),
            Options.Create(turnstile));

    private static TenantCreationContext Ctx(string? fingerprint = null, string? token = null) =>
        new("acme", "owner@acme.com", fingerprint, token, "203.0.113.9");

    private static int? StatusOf(IResult? result) =>
        (result as IStatusCodeHttpResult)?.StatusCode;

    // ─── Cenários ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task Sem_Flags_Deve_Permitir()
    {
        var guard = Build(new RegistrationAbuseOptions(), new TurnstileOptions());
        Assert.Null(await guard.CheckAsync(Ctx(), CancellationToken.None));
    }

    [Fact]
    public async Task Rate_Limit_Excedido_Deve_Retornar_429()
    {
        var guard = Build(
            new RegistrationAbuseOptions { RegistrationRateLimitEnabled = true },
            new TurnstileOptions(),
            rate: new FakeRateLimiter(blocked: true));

        Assert.Equal(StatusCodes.Status429TooManyRequests, StatusOf(await guard.CheckAsync(Ctx(), CancellationToken.None)));
    }

    [Fact]
    public async Task Email_Descartavel_Deve_Retornar_422()
    {
        var guard = Build(
            new RegistrationAbuseOptions { DisposableEmailBlockEnabled = true },
            new TurnstileOptions(),
            email: new FakeEmailChecker(disposable: true));

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, StatusOf(await guard.CheckAsync(Ctx(), CancellationToken.None)));
    }

    [Fact]
    public async Task Fingerprint_Bloqueado_Deve_Retornar_429()
    {
        var guard = Build(
            new RegistrationAbuseOptions { FingerprintEnabled = true },
            new TurnstileOptions(),
            fp: new FakeFingerprintGuard(blocked: true));

        Assert.Equal(StatusCodes.Status429TooManyRequests, StatusOf(await guard.CheckAsync(Ctx(fingerprint: "abc"), CancellationToken.None)));
    }

    [Fact]
    public async Task Fingerprint_Ausente_Deve_Permitir_Mesmo_Habilitado()
    {
        var guard = Build(
            new RegistrationAbuseOptions { FingerprintEnabled = true },
            new TurnstileOptions(),
            fp: new FakeFingerprintGuard(blocked: true));

        Assert.Null(await guard.CheckAsync(Ctx(fingerprint: null), CancellationToken.None));
    }

    [Fact]
    public async Task Turnstile_Off_Deve_Ignorar_Token()
    {
        var guard = Build(new RegistrationAbuseOptions(), new TurnstileOptions { Enabled = false });
        Assert.Null(await guard.CheckAsync(Ctx(token: null), CancellationToken.None));
    }

    [Fact]
    public async Task Turnstile_On_Sem_Token_Deve_Retornar_400()
    {
        var guard = Build(new RegistrationAbuseOptions(), new TurnstileOptions { Enabled = true });
        Assert.Equal(StatusCodes.Status400BadRequest, StatusOf(await guard.CheckAsync(Ctx(token: null), CancellationToken.None)));
    }

    [Fact]
    public async Task Turnstile_On_Token_Invalido_Deve_Retornar_400()
    {
        var guard = Build(
            new RegistrationAbuseOptions(),
            new TurnstileOptions { Enabled = true },
            ts: new FakeTurnstile(valid: false));

        Assert.Equal(StatusCodes.Status400BadRequest, StatusOf(await guard.CheckAsync(Ctx(token: "bad"), CancellationToken.None)));
    }

    [Fact]
    public async Task Turnstile_On_Token_Valido_Deve_Permitir()
    {
        var guard = Build(
            new RegistrationAbuseOptions(),
            new TurnstileOptions { Enabled = true },
            ts: new FakeTurnstile(valid: true));

        Assert.Null(await guard.CheckAsync(Ctx(token: "good"), CancellationToken.None));
    }

    [Fact]
    public async Task Record_Deve_Registrar_Rate_Limit_E_Fingerprint()
    {
        var rate = new FakeRateLimiter(false);
        var fp = new FakeFingerprintGuard(false);
        var guard = Build(
            new RegistrationAbuseOptions { RegistrationRateLimitEnabled = true, FingerprintEnabled = true },
            new TurnstileOptions(),
            rate: rate, fp: fp);

        await guard.RecordAsync(Ctx(fingerprint: "abc"), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(1, rate.Records);
        Assert.Equal(1, fp.Records);
    }
}
