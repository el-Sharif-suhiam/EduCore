using Common;
using Common.Dtos;
using Common.Enums;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EduCore_DataAccess
{
    public static class clsAuditsData
    {
        public static async Task<int> AddAuditAsync(
    DtoAudit audit,
    SqlConnection? conn = null,
    SqlTransaction? tx = null)
        {
            string query = @"
INSERT INTO Audits
(
    UserId,
    ActionType,
    EntityType,
    EntityId,
    Description,
    IpAddress,
    UserAgent
)
VALUES
(
    @UserId,
    @ActionType,
    @EntityType,
    @EntityId,
    @Description,
    @IpAddress,
    @UserAgent
);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

            bool localConnection = conn is null;

            if (localConnection)
            {
                conn = new SqlConnection(clsDataAccessSettings.ConnectionString);
                await conn.OpenAsync();
            }

            try
            {
                using SqlCommand cmd = new SqlCommand(query, conn, tx);

                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = audit.UserId;

                cmd.Parameters.Add("@ActionType", SqlDbType.NVarChar, 100)
                    .Value = audit.ActionType.ToString();

                cmd.Parameters.Add("@EntityType", SqlDbType.VarChar, 50)
                    .Value = audit.EntityType;

                cmd.Parameters.Add("@EntityId", SqlDbType.Int)
                    .Value = audit.EntityId;

                cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 300)
                    .Value = audit.Description;

                cmd.Parameters.Add("@IpAddress", SqlDbType.VarChar, 45)
                    .Value = (object?)audit.IpAddress ?? DBNull.Value;

                cmd.Parameters.Add("@UserAgent", SqlDbType.NVarChar, 500)
                    .Value = (object?)audit.UserAgent ?? DBNull.Value;

                object? result = await cmd.ExecuteScalarAsync();

                return (result != null &&
                        int.TryParse(result.ToString(), out int id))
                    ? id
                    : -1;
            }
            finally
            {
                if (localConnection && conn is not null)
                    await conn.DisposeAsync();
            }
        }
        public static async Task<DtoAudit?> GetAuditByIdAsync(int id)
        {
            string query = @"
 SELECT
    Id,
    UserId,
    ActionType,
    EntityType,
    EntityId,
    Description,
    DoneAt,
    IpAddress,
    UserAgent
FROM Audits
WHERE Id = @Id;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int idIndex = reader.GetOrdinal("Id");
                        int userIdIndex = reader.GetOrdinal("UserId");
                        int actionTypeIndex = reader.GetOrdinal("ActionType");
                        int entityTypeIndex = reader.GetOrdinal("EntityType");
                        int entityIdIndex = reader.GetOrdinal("EntityId");
                        int descriptionIndex = reader.GetOrdinal("Description");
                        int doneAtIndex = reader.GetOrdinal("DoneAt");
                        int ipAddressIndex = reader.GetOrdinal("IpAddress");
                        int userAgentIndex = reader.GetOrdinal("UserAgent");

                        return new DtoAudit
                        {
                            Id = reader.GetInt32(idIndex),
                            UserId = reader.GetInt32(userIdIndex),
                            ActionType = Enum.Parse<enAuditActionType>(reader.GetString(actionTypeIndex)),
                            EntityType = reader.GetString(entityTypeIndex),
                            EntityId = reader.GetInt32(entityIdIndex),
                            Description = reader.GetString(descriptionIndex),
                            DoneAt = reader.GetDateTime(doneAtIndex),
                            IpAddress = reader.IsDBNull(ipAddressIndex) ? null : reader.GetString(ipAddressIndex),
                            UserAgent = reader.IsDBNull(userAgentIndex) ? null : reader.GetString(userAgentIndex)
                        };
                    }
                }
            }

            return null;
        }

        public static async Task<List<DtoAudit>> GetAllAuditsAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            List<DtoAudit> audits = new();

            string query = @"SELECT
                                Id,
                                UserId,
                                ActionType,
                                EntityType,
                                EntityId,
                                Description,
                                DoneAt,
                                IpAddress,
                                UserAgent
                            FROM Audits
                            ORDER BY DoneAt DESC
                            OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                            FETCH NEXT @RowsPerPage ROWS ONLY;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                cmd.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int userIdIndex = reader.GetOrdinal("UserId");
                    int actionTypeIndex = reader.GetOrdinal("ActionType");
                    int entityTypeIndex = reader.GetOrdinal("EntityType");
                    int entityIdIndex = reader.GetOrdinal("EntityId");
                    int descriptionIndex = reader.GetOrdinal("Description");
                    int doneAtIndex = reader.GetOrdinal("DoneAt");
                    int ipAddressIndex = reader.GetOrdinal("IpAddress");
                    int userAgentIndex = reader.GetOrdinal("UserAgent");

                    while (await reader.ReadAsync())
                    {
                        audits.Add(new DtoAudit
                        {
                            Id = reader.GetInt32(idIndex),
                            UserId = reader.GetInt32(userIdIndex),
                            ActionType = Enum.Parse<enAuditActionType>(reader.GetString(actionTypeIndex)),
                            EntityType = reader.GetString(entityTypeIndex),
                            EntityId = reader.GetInt32(entityIdIndex),
                            Description = reader.GetString(descriptionIndex),
                            DoneAt = reader.GetDateTime(doneAtIndex),
                            IpAddress = reader.IsDBNull(ipAddressIndex) ? null : reader.GetString(ipAddressIndex),
                            UserAgent = reader.IsDBNull(userAgentIndex) ? null : reader.GetString(userAgentIndex)
                        });
                    }
                }
            }

            return audits;
        }

        public static async Task<List<DtoAudit>> GetAuditsByUserAsync(int userId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            List<DtoAudit> audits = new();

            string query = @"
                                SELECT
                                    Id,
                                    UserId,
                                    ActionType,
                                    EntityType,
                                    EntityId,
                                    Description,
                                    DoneAt,
                                    IpAddress,
                                    UserAgent
                                FROM Audits
                                WHERE UserId = @UserId
                                ORDER BY DoneAt DESC
                                OFFSET (@PageNumber - 1) * @RowsPerPage ROWS
                                FETCH NEXT @RowsPerPage ROWS ONLY;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                cmd.Parameters.Add("@RowsPerPage", SqlDbType.Int).Value = pageSize;

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int userIdIndex = reader.GetOrdinal("UserId");
                    int actionTypeIndex = reader.GetOrdinal("ActionType");
                    int entityTypeIndex = reader.GetOrdinal("EntityType");
                    int entityIdIndex = reader.GetOrdinal("EntityId");
                    int descriptionIndex = reader.GetOrdinal("Description");
                    int doneAtIndex = reader.GetOrdinal("DoneAt");
                    int ipAddressIndex = reader.GetOrdinal("IpAddress");
                    int userAgentIndex = reader.GetOrdinal("UserAgent");

                    while (await reader.ReadAsync())
                    {
                        audits.Add(new DtoAudit
                        {
                            Id = reader.GetInt32(idIndex),
                            UserId = reader.GetInt32(userIdIndex),
                            ActionType = Enum.Parse<enAuditActionType>(reader.GetString(actionTypeIndex)),
                            EntityType = reader.GetString(entityTypeIndex),
                            EntityId = reader.GetInt32(entityIdIndex),
                            Description = reader.GetString(descriptionIndex),
                            DoneAt = reader.GetDateTime(doneAtIndex),
                            IpAddress = reader.IsDBNull(ipAddressIndex) ? null : reader.GetString(ipAddressIndex),
                            UserAgent = reader.IsDBNull(userAgentIndex) ? null : reader.GetString(userAgentIndex)
                        });
                    }
                }
            }

            return audits;
        }
    }
}