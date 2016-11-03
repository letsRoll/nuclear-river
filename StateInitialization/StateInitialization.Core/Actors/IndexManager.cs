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

        public void DisableIndexes(string tableName)
        {
            var table = GetTable(tableName);
            DisableIndexes(table);
        }

        public void EnableIndexes(string tableName)
        {
            var table = GetTable(tableName);
            EnableIndexes(table);
        }

        private Table GetTable(string tableName)
        {
            var database = _sqlConnection.GetDatabase();
            var table = database.Tables[tableName];

            if (table == null)
            {
                throw new ArgumentException($"Table {tableName} was not found", nameof(tableName));
            }

            return table;
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