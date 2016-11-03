using System;
using System.Data.SqlClient;
using System.Linq;

using Microsoft.SqlServer.Management.Smo;

namespace NuClear.StateInitialization.Core.Actors
{
    internal sealed class IndexManager
    {
        private static readonly object IndexLock = new object();
        private readonly SqlConnection _sqlConnection;

        public IndexManager(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public void DisableIndexes(Storage.Table table)
        {
            DisableIndexes(GetTable(table));
        }

        public void EnableIndexes(Storage.Table table)
        {
            EnableIndexes(GetTable(table));
        }

        private Table GetTable(Storage.Table table)
        {
            var database = _sqlConnection.GetDatabase();
            var tableInDb = string.IsNullOrEmpty(table.SchemaName) ? database.Tables[table.TableName] : database.Tables[table.TableName, table.SchemaName];

            if (tableInDb == null)
            {
                throw new ArgumentException($"Table {table} was not found", nameof(table));
            }

            return tableInDb;
        }

        private void DisableIndexes(Table tableType)
        {
            lock (IndexLock)
            {
                foreach (var index in tableType.Indexes.Cast<Index>().Where(x => x != null && !x.IsClustered && !x.IsDisabled))
                {
                    index.Alter(IndexOperation.Disable);
                }
            }
        }

        private void EnableIndexes(Table tableType)
        {
            lock (IndexLock)
            {
                tableType.EnableAllIndexes(IndexEnableAction.Rebuild);
            }
        }
    }
}