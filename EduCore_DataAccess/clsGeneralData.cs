using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduCore_DataAccess
{
    public class clsGeneralData
    {

            public static async Task<T> ExecuteTransaction<T>(Func<SqlConnection, SqlTransaction, Task<T>> work)
            {
                using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
                {
                    await conn.OpenAsync();

                    using (SqlTransaction tx = (SqlTransaction)await conn.BeginTransactionAsync())
                    {
                        try
                        {
                            T result = await work(conn, tx);
                            await tx.CommitAsync();
                            return result;
                        }
                        catch
                        {
                            await tx.RollbackAsync();
                            throw;
                        }
                    }
                }
            }

            public static async Task ExecuteTransaction(Func<SqlConnection, SqlTransaction,Task> work)
            {
                await ExecuteTransaction<object>(async (conn, tx) =>
                {
                    await work(conn, tx);
                    return null;
                });
            }
        
    }
}
