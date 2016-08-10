using System.Data;
using System.Data.SqlClient;

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace NuClear.StateInitialization.Core
{
    public static class SqlConnectionExtensions
    {
        public static Database GetDatabase(this SqlConnection sqlConnection)
        {
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }

            var connection = new ServerConnection(sqlConnection);
            var server = new Server(connection);

            var connectionStringBuilder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
            return server.Databases[connectionStringBuilder.InitialCatalog];
        }
    }
}