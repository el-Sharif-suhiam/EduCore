using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduCore_DataAccess
{
    public class clsGeneralData
    {

            public static T ExecuteTransaction<T>(Func<SqlConnection, SqlTransaction, T> work)
            {
                using (SqlConnection conn = new SqlConnection(clsDataAccessSettings.ConnectionString))
                {
                    conn.Open();

                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        try
                        {
                            T result = work(conn, tx);
                            tx.Commit();
                            return result;
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
            }

            public static void ExecuteTransaction(Action<SqlConnection, SqlTransaction> work)
            {
                ExecuteTransaction<object>((conn, tx) =>
                {
                    work(conn, tx);
                    return null;
                });
            }
        
    }
}
