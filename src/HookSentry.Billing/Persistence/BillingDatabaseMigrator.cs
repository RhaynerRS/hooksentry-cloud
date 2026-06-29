using DbUp;
using DbUp.Engine.Output;
using Microsoft.Extensions.Logging;

namespace HookSentry.Billing.Persistence;

public static class BillingDatabaseMigrator
{
    public static void Migrate(string connectionString, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BillingMigrations");

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(BillingDatabaseMigrator).Assembly,
                s => s.Contains(".Persistence.Migrations."))
            .WithTransactionPerScript()
            .LogTo(new DbUpLogAdapter(logger))
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
            throw new InvalidOperationException("Billing database migration failed.", result.Error);
    }

    private sealed class DbUpLogAdapter(ILogger logger) : IUpgradeLog
    {
        public void WriteInformation(string format, params object[] args) =>
            logger.LogInformation(format, args);

        public void WriteWarning(string format, params object[] args) =>
            logger.LogWarning(format, args);

        public void WriteError(string format, params object[] args) =>
            logger.LogError(format, args);
    }
}
