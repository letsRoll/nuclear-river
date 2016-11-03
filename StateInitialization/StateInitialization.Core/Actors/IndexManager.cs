using System;
using System.Data.SqlClient;
using System.Linq;

using Microsoft.SqlServer.Management.Smo;

using NuClear.StateInitialization.Core.Storage;

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

        public void DisableIndexes(TableName table)
        {
            DisableIndexes(GetTable(table));
        }

        public void EnableIndexes(TableName table)
        {
            EnableIndexes(GetTable(table));
        }

        private Table GetTable(TableName table)
        {
            var database = _sqlConnection.GetDatabase();
            var tableInDb = string.IsNullOrEmpty(table.Schema) ? database.Tables[table.Table] : database.Tables[table.Table, table.Schema];

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