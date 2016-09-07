using System;
using System.Data.SqlClient;
using System.Linq;

using LinqToDB.Mapping;

using Microsoft.SqlServer.Management.Smo;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class IndexManagementService
    {
        private static readonly object IndexLock = new object();
        private readonly SqlConnection _sqlConnection;

        public IndexManagementService(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public void DisableIndexes(MappingSchema mappingSchema, Type tableType)
        {
            var table = GetTable(mappingSchema, tableType);
            DisableIndexes(table);
        }

        public void EnableIndexes(MappingSchema mappingSchema, Type tableType)
        {
            var table = GetTable(mappingSchema, tableType);
            EnableIndexes(table);
        }

        private Table GetTable(MappingSchema mappingSchema, Type tableType)
        {
            var attributes = mappingSchema.GetAttributes<TableAttribute>(tableType);
            var tableName = attributes.Select(x => x.Name).FirstOrDefault() ?? tableType.Name;
            var schemaName = attributes.Select(x => x.Schema).FirstOrDefault();

            var database = _sqlConnection.GetDatabase();
            var table = !string.IsNullOrEmpty(schemaName) ? database.Tables[tableName, schemaName] : database.Tables[tableName];

            if (table == null)
            {
                throw new ArgumentException($"Table {tableName} in schema {schemaName} for type {tableType.Name} was not found", nameof(tableType));
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