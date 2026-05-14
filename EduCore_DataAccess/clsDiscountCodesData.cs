using Common.Dtos;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace EduCore_DataAccess
{
    public class clsDiscountCodesData
    {
        public static async Task<short> AddDiscountCode(DtoDiscountCode dto)
        {
            string query = @"INSERT INTO DiscountCodes
                             (DiscountCode, DiscountRate, CreatedById, ExpireAt, AllowedUseNumber, TotalUserNumber)
                             VALUES
                             (@Code, @Rate, @CreatedBy, @ExpireAt, @AllowedUse, @TotalUsers);
                             SELECT SCOPE_IDENTITY();";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@Code", SqlDbType.VarChar, 200).Value = dto.DiscountCode;
                cmd.Parameters.Add("@Rate", SqlDbType.Decimal).Value = (object?)dto.DiscountRate ?? DBNull.Value;
                cmd.Parameters.Add("@CreatedBy", SqlDbType.Int).Value = dto.CreatedById;
                cmd.Parameters.Add("@ExpireAt", SqlDbType.Date).Value = (object?)dto.ExpireAt ?? DBNull.Value;
                cmd.Parameters.Add("@AllowedUse", SqlDbType.SmallInt).Value = (object?)dto.AllowedUseNumber ?? DBNull.Value;
                cmd.Parameters.Add("@TotalUsers", SqlDbType.SmallInt).Value = (object?)dto.TotalUsedNumber ?? DBNull.Value;

                await con.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();

                return (result != null && short.TryParse(result.ToString(), out short id)) ? id : (short)-1;
            }
        }

        public static async Task<DtoDiscountCode> GetDiscountCodeById(short id)
        {
            string query = @"SELECT Id, DiscountCode, DiscountRate, CreatedById, ExpireAt,
                                AllowedUseNumber, TotalUserNumber
                         FROM DiscountCodes
                         WHERE Id = @Id;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@Id", SqlDbType.SmallInt).Value = id;

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int discountCodeIndex = reader.GetOrdinal("DiscountCode");
                    int discountRateIndex = reader.GetOrdinal("DiscountRate");
                    int createdByIdIndex = reader.GetOrdinal("CreatedById");
                    int expireAtIndex = reader.GetOrdinal("ExpireAt");
                    int allowedUseIndex = reader.GetOrdinal("AllowedUseNumber");
                    int totalUsedIndex = reader.GetOrdinal("TotalUserNumber");
                    if (await reader.ReadAsync())
                    {
                        return new DtoDiscountCode
                        {
                            Id = reader.GetInt16(idIndex),
                            DiscountCode = reader.GetString(discountCodeIndex),
                            DiscountRate = reader.IsDBNull(discountRateIndex) ? null : reader.GetDecimal(discountRateIndex),
                            CreatedById = reader.GetInt32(createdByIdIndex),
                            ExpireAt = reader.IsDBNull(expireAtIndex) ? null : reader.GetDateTime(expireAtIndex),
                            AllowedUseNumber = reader.IsDBNull(allowedUseIndex) ? null : reader.GetInt16(allowedUseIndex),
                            TotalUsedNumber = reader.IsDBNull(totalUsedIndex) ? null : reader.GetInt16(totalUsedIndex)
                        };
                    }
                }
            }

            return null;
        }

        public static async Task<bool> UpdateDiscountCode(DtoDiscountCode dto)
        {
            string query = @"UPDATE DiscountCodes
                            SET DiscountCode = @Code,
                                DiscountRate = @Rate,
                                ExpireAt = @ExpireAt,
                                AllowedUseNumber = @AllowedUse,
                                TotalUserNumber = @TotalUsers
                            WHERE Id = @Id;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@Id", SqlDbType.SmallInt).Value = dto.Id;
                cmd.Parameters.Add("@Code", SqlDbType.VarChar, 200).Value = dto.DiscountCode;
                cmd.Parameters.Add("@Rate", SqlDbType.Decimal).Value = (object?)dto.DiscountRate ?? DBNull.Value;
                cmd.Parameters.Add("@ExpireAt", SqlDbType.Date).Value = (object?)dto.ExpireAt ?? DBNull.Value;
                cmd.Parameters.Add("@AllowedUse", SqlDbType.SmallInt).Value = (object?)dto.AllowedUseNumber ?? DBNull.Value;
                cmd.Parameters.Add("@TotalUsers", SqlDbType.SmallInt).Value = (object?)dto.TotalUsedNumber ?? DBNull.Value;

                await con.OpenAsync();
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
        }

        public static async Task<bool> DeleteDiscountCode(short id)
        {
            string query = @"DELETE FROM DiscountCodes WHERE Id = @Id;";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@Id", SqlDbType.SmallInt).Value = id;

                await con.OpenAsync();
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
        }

        public static async Task<List<DtoDiscountCode>> GetValidDiscountCodes()
        {
            List<DtoDiscountCode> discounts = new List<DtoDiscountCode>();
            string query = @"SELECT Id, DiscountCode, DiscountRate, CreatedById, ExpireAt,
                                AllowedUseNumber, TotalUserNumber
                             FROM DiscountCodes
                             WHERE (ExpireAt > GETDATE() OR ExpireAt IS NULL) AND 
                            (AllowedUseNumber = 0 OR AllowedUseNumber IS NULL OR AllowedUseNumber > TotalUserNumber);";

            using (SqlConnection con = new SqlConnection(clsDataAccessSettings.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    int idIndex = reader.GetOrdinal("Id");
                    int discountCodeIndex = reader.GetOrdinal("DiscountCode");
                    int discountRateIndex = reader.GetOrdinal("DiscountRate");
                    int createdByIdIndex = reader.GetOrdinal("CreatedById");
                    int expireAtIndex = reader.GetOrdinal("ExpireAt");
                    int allowedUseIndex = reader.GetOrdinal("AllowedUseNumber");
                    int totalUsedIndex = reader.GetOrdinal("TotalUserNumber");
                    
                    while (await reader.ReadAsync())
                    {
                        discounts.Add(new DtoDiscountCode
                        {
                            Id = reader.GetInt16(idIndex),
                            DiscountCode = reader.GetString(discountCodeIndex),
                            DiscountRate = reader.IsDBNull(discountRateIndex) ? null : reader.GetDecimal(discountRateIndex),
                            CreatedById = reader.GetInt32(createdByIdIndex),
                            ExpireAt = reader.IsDBNull(expireAtIndex) ? null : reader.GetDateTime(expireAtIndex),
                            AllowedUseNumber = reader.IsDBNull(allowedUseIndex) ? null : reader.GetInt16(allowedUseIndex),
                            TotalUsedNumber = reader.IsDBNull(totalUsedIndex) ? null : reader.GetInt16(totalUsedIndex)
                        });
                    }
                }
            }

            return discounts;
        }


    }
}
