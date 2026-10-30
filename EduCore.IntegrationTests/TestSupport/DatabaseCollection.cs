using EduCore.IntegrationTests.TestSupport;
using Xunit;

namespace EduCore.IntegrationTests
{
    /// <summary>
    /// Everything runs inside ONE non-parallelized collection: the tests
    /// share a single disposable SQL Server database and a single API host,
    /// and some tests (EnvConfigTests) mutate process environment variables.
    /// </summary>
    [CollectionDefinition(DatabaseCollection.Name, DisableParallelization = true)]
    public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
        public const string Name = "DatabaseCollection";
    }
}
