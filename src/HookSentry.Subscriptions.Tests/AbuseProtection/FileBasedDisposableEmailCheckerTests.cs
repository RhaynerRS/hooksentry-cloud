using HookSentry.Subscriptions.AbuseProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace HookSentry.Subscriptions.Tests.AbuseProtection;

public class FileBasedDisposableEmailCheckerTests
{
    private sealed class FakeEnv(string contentRoot) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "tests";
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static FileBasedDisposableEmailChecker CheckerWithDomains(params string[] lines)
    {
        var root = Path.Combine(Path.GetTempPath(), "hs-abuse-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(root, "Resources"));
        File.WriteAllLines(Path.Combine(root, "Resources", "disposable-domains.txt"), lines);
        return new FileBasedDisposableEmailChecker(new FakeEnv(root));
    }

    [Fact]
    public async Task Deve_Bloquear_Dominio_Descartavel()
    {
        var checker = CheckerWithDomains("# comment", "mailinator.com", "yopmail.com");
        Assert.True(await checker.IsDisposableAsync("owner@mailinator.com", CancellationToken.None));
    }

    [Fact]
    public async Task Nao_Deve_Bloquear_Dominio_Nao_Listado()
    {
        var checker = CheckerWithDomains("mailinator.com");
        Assert.False(await checker.IsDisposableAsync("owner@gmail.com", CancellationToken.None));
    }

    [Fact]
    public async Task Deve_Ignorar_Maiusculas_Minusculas()
    {
        var checker = CheckerWithDomains("mailinator.com");
        Assert.True(await checker.IsDisposableAsync("Owner@MailInator.COM", CancellationToken.None));
    }

    [Fact]
    public async Task Deve_Fazer_Fail_Open_Quando_Arquivo_Ausente()
    {
        var root = Path.Combine(Path.GetTempPath(), "hs-abuse-" + Guid.NewGuid());
        Directory.CreateDirectory(root); // no Resources/disposable-domains.txt
        var checker = new FileBasedDisposableEmailChecker(new FakeEnv(root));
        Assert.False(await checker.IsDisposableAsync("owner@mailinator.com", CancellationToken.None));
    }
}
