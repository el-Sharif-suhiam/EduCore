
namespace EduCore_DataAccess
{
    internal class clsDataAccessSettings
    {
        private static string? _connectionString;

        public static string ConnectionString
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_connectionString))
                    return _connectionString;

                string? fromEnv = Environment.GetEnvironmentVariable("DB_CONNECTION");

                if (string.IsNullOrWhiteSpace(fromEnv))
                    throw new InvalidOperationException(
                        "DB_CONNECTION environment variable is not set. " +
                        "Set it in EduCoreAPI/.env (see .env.example).");

                _connectionString = fromEnv;
                return _connectionString;
            }
        }

        public static void SetConnectionString(string connectionString)
        {
            _connectionString = connectionString;
        }
    }
}
