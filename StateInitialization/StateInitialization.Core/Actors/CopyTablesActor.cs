using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.DataObjects;
using NuClear.StateInitialization.Core.Factories;
using NuClear.StateInitialization.Core.Storage;
using NuClear.Storage.API.ConnectionStrings;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class CopyTablesActor : IActor
    {
        private readonly IDataObjectTypesProviderFactory _dataObjectTypesProviderFactory;
        private readonly IConnectionStringSettings _connectionStringSettings;

        public CopyTablesActor(
            IDataObjectTypesProviderFactory dataObjectTypesProviderFactory,
            IConnectionStringSettings connectionStringSettings)
        {
            _dataObjectTypesProviderFactory = dataObjectTypesProviderFactory;
            _connectionStringSettings = connectionStringSettings;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            foreach (var command in commands.Cast<ReplicateInBulkCommand>())
            {
                using (var db = CreateSqlConnection(command.TargetStorageDescriptor))
                {
                    var actor = new CreateTableCopyActor(db);
                    actor.ExecuteCommands(CreateCopyTableCommands(command));
                }
            }

            return Array.Empty<IEvent>();
        }

        private IReadOnlyCollection<CreateTableCopyCommand> CreateCopyTableCommands(ReplicateInBulkCommand command)
            => GetDataObjectTypes(command)
                .Select(t => command.TargetStorageDescriptor.MappingSchema.GetTableName(t))
                .Distinct(TableNameComparer.Instance)
                .Select(CreateCopyCommand)
                .ToArray();

        private IReadOnlyCollection<Type> GetDataObjectTypes(ReplicateInBulkCommand command)
            => ((ICommandRegardlessDataObjectTypesProvider)_dataObjectTypesProviderFactory.Create(command)).Get();

        private CreateTableCopyCommand CreateCopyCommand(TableName t)
            => new CreateTableCopyCommand(t, new TableName($"{t.Schema}_{t.Table}", "dbo"));

        private SqlConnection CreateSqlConnection(StorageDescriptor storageDescriptor)
            => new SqlConnection(_connectionStringSettings.GetConnectionString(storageDescriptor.ConnectionStringIdentity));

        public class TableNameComparer : IEqualityComparer<TableName>
        {
            public static readonly TableNameComparer Instance = new TableNameComparer();

            public bool Equals(TableName x, TableName y)
                => string.Equals(x.Schema, y.Schema, StringComparison.InvariantCultureIgnoreCase)
                   && string.Equals(x.Table, y.Table, StringComparison.InvariantCultureIgnoreCase);

            public int GetHashCode(TableName obj)
                => obj.Schema.GetHashCode() ^ obj.Table.GetHashCode();
        }
    }
}
