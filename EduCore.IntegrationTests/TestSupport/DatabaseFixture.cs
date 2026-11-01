using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace EduCore.IntegrationTests.TestSupport
{
    /// <summary>
    /// One disposable SQL Server database (EduCoreTests) per test RUN:
    ///   1. drops any leftover EduCoreTests database,
    ///   2. creates a fresh one and applies the repo's EduCore.sql
    ///      (the exact same schema script documented in the README),
    ///   3. points clsDataAccessSettings / the API host at it.
    ///
    /// The developer's real EduCore database is NEVER touched: this fixture
    /// only connects to 'master' to create/drop the dedicated EduCoreTests
    /// database, and rewrites the script's USE batch to the test database.
    ///
    /// Connection string resolution:
    ///   EDUCORE_TEST_DB_CONNECTION when set (e.g. CI service container),
    ///   else "Server=.;Integrated Security=True;TrustServerCertificate=True".
    /// When SQL Server is unreachable the fixture flags DatabaseAvailable=false
    /// and DB tests SKIP themselves (reported as blocked, never failed).
    /// </summary>
    public sealed class DatabaseFixture : IAsyncLifetime
    {
        public const string TestDbName = "EduCoreTests";

        private readonly string _masterConnectionString;
        private string _testDbConnectionString = "";

        public DatabaseFixture()
        {
            string? fromEnv = Environment.GetEnvironmentVariable("EDUCORE_TEST_DB_CONNECTION");

            _masterConnectionString = string.IsNullOrWhiteSpace(fromEnv)
                ? "Server=.;Integrated Security=True;TrustServerCertificate=True"
                : StripInitialCatalog(fromEnv);
        }

        public string MasterConnectionString => _masterConnectionString;

        public string TestDatabaseConnectionString => _testDbConnectionString;

        public bool DatabaseAvailable { get; private set; }

        public string LastInitError { get; private set; } = "";

        public TestSeeder Seed => new(this);

        public async Task InitializeAsync()
        {
            SqlConnectionStringBuilder builder = new(_masterConnectionString)
            {
                InitialCatalog = TestDbName
            };
            _testDbConnectionString = builder.ConnectionString;

            try
            {
                string scriptPath = ResolveScriptPath();
                string script = await File.ReadAllTextAsync(scriptPath);

                await using (SqlConnection master = new SqlConnection(_masterConnectionString))
                {
                    await master.OpenAsync();

                    // Disposable environment: start from a clean copy every run.
                    await Exec(master, $"IF DB_ID('{TestDbName}') IS NOT NULL ALTER DATABASE {TestDbName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");
                    await Exec(master, $"IF DB_ID('{TestDbName}') IS NOT NULL DROP DATABASE {TestDbName};");
                    await Exec(master, $"CREATE DATABASE {TestDbName};");
                }

                // Apply the same schema script the README documents. The script
                // contains GO batches; execute them one by one, with the target
                // database rewritten so nothing can touch the real EduCore DB.
                script = script.Replace("USE EduCore;", $"USE {TestDbName};", StringComparison.OrdinalIgnoreCase);

                await using (SqlConnection con = new SqlConnection(_testDbConnectionString))
                {
                    await con.OpenAsync();
                    foreach (string batch in SplitBatches(script))
                    {
                        await using SqlCommand cmd = new SqlCommand(batch, con);
                        cmd.CommandTimeout = 180;
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Point the data-access layer AND the in-process API host at
                // the isolated test database (clsDataAccessSettings reads
                // DB_CONNECTION on first use; nothing is cached before this).
                Environment.SetEnvironmentVariable("DB_CONNECTION", _testDbConnectionString);
                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JWT_SECRET_KEY")))
                    Environment.SetEnvironmentVariable("JWT_SECRET_KEY", new string('t', 64));

                // Business-layer tests generate certificate PDFs outside the
                // API host, so the QuestPDF community license must be set here.
                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                DatabaseAvailable = true;
            }
            catch (Exception ex)
            {
                DatabaseAvailable = false;
                LastInitError = ex.Message;

                // CI sets EDUCORE_REQUIRE_DB=1: an unreachable SQL Server
                // must FAIL the run, never silently skip every database
                // test and report a green build.
                if (!string.IsNullOrWhiteSpace(
                        Environment.GetEnvironmentVariable("EDUCORE_REQUIRE_DB")))
                {
                    throw new InvalidOperationException(
                        "EDUCORE_REQUIRE_DB is set but SQL Server is unavailable: "
                        + ex.Message, ex);
                }
            }
        }

        public async Task DisposeAsync()
        {
            if (_testDbConnectionString.Length == 0)
                return;

            try
            {
                SqlConnectionStringBuilder masterBuilder = new(_masterConnectionString) { InitialCatalog = "master" };
                await using SqlConnection master = new SqlConnection(masterBuilder.ConnectionString);
                await master.OpenAsync();
                await Exec(master, $"IF DB_ID('{TestDbName}') IS NOT NULL ALTER DATABASE {TestDbName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");
                await Exec(master, $"IF DB_ID('{TestDbName}') IS NOT NULL DROP DATABASE {TestDbName};");
            }
            catch
            {
                // Best-effort cleanup; leftover test DBs are disposable.
            }
        }

        private static async Task Exec(SqlConnection conn, string sql)
        {
            await using SqlCommand cmd = new(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>The repo's EduCore.sql is copied next to the test binaries.</summary>
        private static string ResolveScriptPath()
        {
            string candidate = Path.Combine(AppContext.BaseDirectory, "EduCore.sql");

            if (!File.Exists(candidate))
                throw new FileNotFoundException(
                    "EduCore.sql was not copied to the test output directory.", candidate);

            return candidate;
        }

        private static string[] SplitBatches(string script)
        {
            string[] batches = script.Split(
                new[] { "\r\nGO", "\nGO", "\rgo", "\ngo" },
                StringSplitOptions.RemoveEmptyEntries);

            // Drop any batch that creates the real EduCore database so the
            // isolated test run can never touch it. The test database itself
            // is created separately by the fixture.
            var filtered = new System.Collections.Generic.List<string>();
            foreach (string batch in batches)
            {
                if (batch.IndexOf("CREATE DATABASE EduCore", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    batch.IndexOf("DB_ID('EduCore')", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }
                filtered.Add(batch);
            }
            return filtered.ToArray();
        }

        private static string StripInitialCatalog(string connectionString)
        {
            SqlConnectionStringBuilder builder = new(connectionString) { InitialCatalog = "" };
            return builder.ConnectionString;
        }
    }
}
