using Common;
using Common.Dtos;
using Common.Enums;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EduCore_DataAccess
{
    public static class clsLogsData
    {
        public static async Task<int> AddLogAsync(DtoLog log)
        {
            string query = @"
INSERT INTO Logs
(
    LogType,
    Message,
    Source,
    IpAddress,
    UserAgent,
    RequestPath
)
VALUES
(
    @LogType,
    @Message,
    @Source,
    @IpAddress,
    @UserAgent,
    @RequestPath
);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using (SqlConnection conn =
                new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@LogType", SqlDbType.NVarChar, 30)
                    .Value = log.LogType.ToString();

                cmd.Parameters.Add("@Message", SqlDbType.NVarChar)
                    .Value = log.Message;

                cmd.Parameters.Add("@Source", SqlDbType.NVarChar, 200)
                    .Value = (object?)log.Source ?? DBNull.Value;

                cmd.Parameters.Add("@IpAddress", SqlDbType.VarChar, 45)
                    .Value = (object?)log.IpAddress ?? DBNull.Value;

                cmd.Parameters.Add("@UserAgent", SqlDbType.NVarChar, 500)
                    .Value = (object?)log.UserAgent ?? DBNull.Value;

                cmd.Parameters.Add("@RequestPath", SqlDbType.NVarChar, 300)
                    .Value = (object?)log.RequestPath ?? DBNull.Value;

                await conn.OpenAsync();

                object? result = await cmd.ExecuteScalarAsync();

                return (result != null && int.TryParse(result.ToString(), out int id))
                    ? id
                    : -1;
            }
        }

        public static async Task<DtoLog?> GetLogByIdAsync(int id)
        {
            string query = @"SELECT
                                 Id,
                                 LogType,
                                 Message,
                                 Source,
                                 IpAddress,
                                 UserAgent,
                                 RequestPath
                             FROM Logs
                             WHERE Id = @Id;";

            using (SqlConnection conn =
                new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                await conn.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int idIndex = reader.GetOrdinal("Id");
                        int logTypeIndex = reader.GetOrdinal("LogType");
                        int messageIndex = reader.GetOrdinal("Message");
                        int sourceIndex = reader.GetOrdinal("Source");
                        int ipIndex = reader.GetOrdinal("IpAddress");
                        int userAgentIndex = reader.GetOrdinal("UserAgent");
                        int requestPathIndex = reader.GetOrdinal("RequestPath");

                        return new DtoLog
                        {
                            Id = reader.GetInt32(idIndex),
                            LogType = Enum.Parse<enLogType>(reader.GetString(logTypeIndex)),
                            Message = reader.GetString(messageIndex),
                            Source = reader.IsDBNull(sourceIndex) ? null : reader.GetString(sourceIndex),
                            IpAddress = reader.IsDBNull(ipIndex) ? null : reader.GetString(ipIndex),
                            UserAgent = reader.IsDBNull(userAgentIndex) ? null : reader.GetString(userAgentIndex),
                            RequestPath = reader.IsDBNull(requestPathIndex) ? null : reader.GetString(requestPathIndex)
                        };
                    }
                }
            }

            return null;
        }

        public static async Task<List<DtoLog>> GetAllLogsAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 20;

            List<DtoLog> logs = new();

            string query = @"SELECT
                                Id,
                                LogType,
                                Message,
                                Source,
                                IpAddress,
                                UserAgent,
                                RequestPath
                            FROM Logs
                            ORDER BY Id DESC
                            OFFSET (@PageNumber - 1) * @PageSize ROWS
                            FETCH NEXT @PageSize ROWS ONLY;";

            using (SqlConnection conn =
                new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

                await conn.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int logTypeIndex = reader.GetOrdinal("LogType");
                    int messageIndex = reader.GetOrdinal("Message");
                    int sourceIndex = reader.GetOrdinal("Source");
                    int ipIndex = reader.GetOrdinal("IpAddress");
                    int userAgentIndex = reader.GetOrdinal("UserAgent");
                    int requestPathIndex = reader.GetOrdinal("RequestPath");

                    while (await reader.ReadAsync())
                    {
                        logs.Add(new DtoLog
                        {
                            Id = reader.GetInt32(idIndex),
                            LogType = Enum.Parse<enLogType>(reader.GetString(logTypeIndex)),
                            Message = reader.GetString(messageIndex),
                            Source = reader.IsDBNull(sourceIndex) ? null : reader.GetString(sourceIndex),
                            IpAddress = reader.IsDBNull(ipIndex) ? null : reader.GetString(ipIndex),
                            UserAgent = reader.IsDBNull(userAgentIndex) ? null : reader.GetString(userAgentIndex),
                            RequestPath = reader.IsDBNull(requestPathIndex) ? null : reader.GetString(requestPathIndex)
                        });
                    }
                }
            }

            return logs;
        }

        public static async Task<List<DtoLog>> GetLogsByTypeAsync(enLogType logType, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 20;

            List<DtoLog> logs = new();

            string query = @"SELECT
                                Id,
                                LogType,
                                Message,
                                Source,
                                IpAddress,
                                UserAgent,
                                RequestPath
                            FROM Logs
                            WHERE LogType = @LogType
                            ORDER BY Id DESC
                            OFFSET (@PageNumber - 1) * @PageSize ROWS
                            FETCH NEXT @PageSize ROWS ONLY;";

            using (SqlConnection conn =
                new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@LogType", SqlDbType.NVarChar, 30).Value = logType.ToString();
                cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

                await conn.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int logTypeIndex = reader.GetOrdinal("LogType");
                    int messageIndex = reader.GetOrdinal("Message");
                    int sourceIndex = reader.GetOrdinal("Source");
                    int ipIndex = reader.GetOrdinal("IpAddress");
                    int userAgentIndex = reader.GetOrdinal("UserAgent");
                    int requestPathIndex = reader.GetOrdinal("RequestPath");

                    while (await reader.ReadAsync())
                    {
                        logs.Add(new DtoLog
                        {
                            Id = reader.GetInt32(idIndex),
                            LogType = Enum.Parse<enLogType>(reader.GetString(logTypeIndex)),
                            Message = reader.GetString(messageIndex),
                            Source = reader.IsDBNull(sourceIndex) ? null : reader.GetString(sourceIndex),
                            IpAddress = reader.IsDBNull(ipIndex) ? null : reader.GetString(ipIndex),
                            UserAgent = reader.IsDBNull(userAgentIndex) ? null : reader.GetString(userAgentIndex),
                            RequestPath = reader.IsDBNull(requestPathIndex) ? null : reader.GetString(requestPathIndex)
                        });
                    }
                }
            }

            return logs;
        }
    }
}