using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.DataObjects;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.DataObjects;
using NuClear.StateInitialization.Core.Events;
using NuClear.StateInitialization.Core.Factories;
using NuClear.StateInitialization.Core.Storage;
using NuClear.Storage.API.ConnectionStrings;

using IsolationLevel = System.Transactions.IsolationLevel;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class BulkReplicationActor : IActor
    {
        private static readonly TransactionOptions TransactionOptions =
            new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Serializable,
                Timeout = TimeSpan.Zero
            };

        private readonly IDataObjectTypesProviderFactory _dataObjectTypesProviderFactory;
        private readonly IConnectionStringSettings _connectionStringSettings;

        public BulkReplicationActor(
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
                var commandStopwatch = Stopwatch.StartNew();

                var dataObjectTypes = GetDataObjectTypes(_dataObjectTypesProviderFactory.Create(command));

                var tableTypesDictionary = dataObjectTypes
                    .GroupBy(t => GetTable(command.TargetStorageDescriptor.MappingSchema, t))
                    .ToDictionary(g => g.Key, g => g.ToArray());

                var executionStrategy = DetermineExecutionStrategy(command);
                executionStrategy.Invoke(command, tableTypesDictionary);

                commandStopwatch.Stop();
                Console.WriteLine($"[{command.SourceStorageDescriptor.ConnectionStringIdentity}] -> " +
                                  $"[{command.TargetStorageDescriptor.ConnectionStringIdentity}]: {commandStopwatch.Elapsed.TotalSeconds} seconds");
            }

            return Array.Empty<IEvent>();
        }

        private static IEnumerable<Type> GetDataObjectTypes(IDataObjectTypesProvider dataObjectTypesProvider)
        {
            var commandRegardlessProvider = (ICommandRegardlessDataObjectTypesProvider)dataObjectTypesProvider;
            return commandRegardlessProvider.Get();
        }

        private static SequentialPipelineActor CreateDbSchemaManagementActor(SqlConnection sqlConnection, TimeSpan commandTimeout)
        {
            return new SequentialPipelineActor(
                new IActor[]
                    {
                        new ViewManagementActor(sqlConnection, commandTimeout),
                        new ReplaceTableActor(sqlConnection),
                        new ConstraintsManagementActor(sqlConnection, commandTimeout)
                    });
        }

        private static IReadOnlyCollection<ICommand> CreateSchemaChangesCommands(DbManagementMode mode)
        {
            var commands = new List<ICommand>();
            if (mode.HasFlag(DbManagementMode.DropAndRecreateViews))
            {
                commands.Add(new DropViewsCommand());
            }

            if (mode.HasFlag(DbManagementMode.DropAndRecreateConstraints))
            {
                commands.Add(new DisableConstraintsCommand());
            }

            return commands;
        }

        private static IReadOnlyCollection<ICommand> CreateSchemaChangesCompensationalCommands(IReadOnlyCollection<IEvent> events)
        {
            var commands = new List<ICommand>();

            var constraintsDisabledEvent = events.OfType<ConstraintsDisabledEvent>().SingleOrDefault();
            if (constraintsDisabledEvent != null)
            {
                commands.Add(new EnableConstraintsCommand(constraintsDisabledEvent.Checks, constraintsDisabledEvent.ForeignKeys));
            }

            var viewsDroppedEvent = events.OfType<ViewsDroppedEvent>().SingleOrDefault();
            if (viewsDroppedEvent != null)
            {
                commands.Add(new RestoreViewsCommand(viewsDroppedEvent.ViewsToRestore));
            }

            return commands;
        }

        private static IReadOnlyCollection<ICommand> CreateReplicationCommands(TableName table, TimeSpan bulkCopyTimeout, DbManagementMode mode)
        {
            var commands = new List<ICommand>();
            if (mode.HasFlag(DbManagementMode.EnableIndexManagment))
            {
                commands.Add(new DisableIndexesCommand(table));
            }

            commands.Add(new TruncateTableCommand(table));
            commands.Add(new BulkInsertDataObjectsCommand(bulkCopyTimeout));

            if (mode.HasFlag(DbManagementMode.EnableIndexManagment))
            {
                commands.Add(new EnableIndexesCommand(table));
            }

            if (mode.HasFlag(DbManagementMode.UpdateTableStatistics))
            {
                commands.Add(new UpdateTableStatisticsCommand(table));
            }

            return commands;
        }

        private static IReadOnlyCollection<ICommand> CreateShadowReplicationCommands(TableName table, TimeSpan bulkCopyTimeout, DbManagementMode mode)
        {
            var createTableCopyCommand = new CreateTableCopyCommand(table);
            var commands = new List<ICommand> { createTableCopyCommand, new BulkInsertDataObjectsCommand(bulkCopyTimeout) };

            if (mode.HasFlag(DbManagementMode.EnableIndexManagment))
            {
                commands.Add(new EnableIndexesCommand(createTableCopyCommand.CopiedTable));
            }

            if (mode.HasFlag(DbManagementMode.UpdateTableStatistics))
            {
                commands.Add(new UpdateTableStatisticsCommand(createTableCopyCommand.CopiedTable));
            }

            return commands;
        }

        private static IReadOnlyCollection<ICommand> CreateTablesReplacingCommands(IEnumerable<TableName> tableTypesDictionary)
        {
            return tableTypesDictionary
                .Select(t => new ReplaceTableCommand(t, CreateTableCopyCommand.GetTableCopyName(t)))
                .ToList();
        }

        private Action<ReplicateInBulkCommand, IReadOnlyDictionary<TableName, Type[]>> DetermineExecutionStrategy(ReplicateInBulkCommand command)
        {
            switch (command.ExecutionMode)
            {
                case ExecutionMode.Parallel:
                    return ParallelExecutionStrategy;
                case ExecutionMode.Sequential:
                    return SequentialExecutionStrategy;
                case ExecutionMode.ShadowParallel:
                    return ShadowParallelExecutionStrategy;
                default:
                    throw new ArgumentException($"Execution mode {command.ExecutionMode} is not supported", nameof(command));
            }
        }

        private void ShadowParallelExecutionStrategy(ReplicateInBulkCommand command, IReadOnlyDictionary<TableName, Type[]> tableTypesDictionary)
        {
            Parallel.ForEach(
                    tableTypesDictionary,
                    tableTypesPair =>
                    {
                        using (var connection = CreateDataConnection(command.TargetStorageDescriptor))
                        {
                            try
                            {
                                var replicationCommands = CreateShadowReplicationCommands(tableTypesPair.Key, command.BulkCopyTimeout, command.DbManagementMode);
                                ReplaceInBulk(tableTypesPair.Value, command.SourceStorageDescriptor, connection, replicationCommands);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Failed to replicate using shadow parallel strategy {tableTypesPair.Key}", ex);
                            }
                        }
                    });

            ExecuteInTransactionScope(
                command,
                (targetConnection, schemaManagenentActor) =>
                {
                    var schemaChangedEvents = schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCommands(command.DbManagementMode));

                    // Delete existed tables then rename new ones (remove prefix):
                    schemaManagenentActor.ExecuteCommands(CreateTablesReplacingCommands(tableTypesDictionary.Keys));
                    schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCompensationalCommands(schemaChangedEvents));
                });
        }

        private void ParallelExecutionStrategy(ReplicateInBulkCommand command, IReadOnlyDictionary<TableName, Type[]> tableTypesDictionary)
        {
            IReadOnlyCollection<IEvent> schemaChangedEvents = null;
            ExecuteInTransactionScope(
                command,
                (targetConnection, schemaManagenentActor) =>
                {
                    schemaChangedEvents = schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCommands(command.DbManagementMode));
                });

            Parallel.ForEach(
                    tableTypesDictionary,
                    tableTypesPair =>
                    {
                        using (var connection = CreateDataConnection(command.TargetStorageDescriptor))
                        {
                            try
                            {
                                var replicationCommands = CreateReplicationCommands(tableTypesPair.Key, command.BulkCopyTimeout, command.DbManagementMode);
                                ReplaceInBulk(tableTypesPair.Value, command.SourceStorageDescriptor, connection, replicationCommands);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Failed to replicate using parallel strategy {tableTypesPair.Key}", ex);
                            }
                        }
                    });

            ExecuteInTransactionScope(
                command,
                (targetConnection, schemaManagenentActor) =>
                {
                    schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCompensationalCommands(schemaChangedEvents));
                });
        }

        private void SequentialExecutionStrategy(ReplicateInBulkCommand command, IReadOnlyDictionary<TableName, Type[]> tableTypesDictionary)
        {
            ExecuteInTransactionScope(
                command,
                (targetConnection, schemaManagenentActor) =>
                {
                    var schemaChangedEvents = schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCommands(command.DbManagementMode));

                    foreach (var tableTypesPair in tableTypesDictionary)
                    {
                        try
                        {
                            var replicationCommands = CreateReplicationCommands(tableTypesPair.Key, command.BulkCopyTimeout, command.DbManagementMode);
                            ReplaceInBulk(tableTypesPair.Value, command.SourceStorageDescriptor, targetConnection, replicationCommands);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Failed to replicate using sequential strategy {tableTypesPair.Key}", ex);
                        }
                    }

                    schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCompensationalCommands(schemaChangedEvents));
                });
        }

        private TableName GetTable(MappingSchema mappingSchema, Type dataObjectType)
        {
            var attribute = mappingSchema
                .GetAttributes<TableAttribute>(dataObjectType)
                .FirstOrDefault();

            var tableName = attribute?.Name ?? dataObjectType.Name;
            var schemaName = attribute?.Schema;
            return new TableName(tableName, schemaName);
        }

        private void ExecuteInTransactionScope(ReplicateInBulkCommand command, Action<DataConnection, SequentialPipelineActor> action)
        {
            using (var transation = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionOptions))
            {
                using (var targetConnection = CreateDataConnection(command.TargetStorageDescriptor))
                {
                    var schemaManagenentActor = CreateDbSchemaManagementActor((SqlConnection)targetConnection.Connection, command.TargetStorageDescriptor.CommandTimeout);
                    action(targetConnection, schemaManagenentActor);
                    transation.Complete();
                }
            }
        }

        private DataConnection CreateDataConnection(StorageDescriptor storageDescriptor)
        {
            var connectionString = _connectionStringSettings.GetConnectionString(storageDescriptor.ConnectionStringIdentity);
            var connection = SqlServerTools.CreateDataConnection(connectionString);
            connection.AddMappingSchema(storageDescriptor.MappingSchema);
            connection.CommandTimeout = (int)storageDescriptor.CommandTimeout.TotalMilliseconds;
            return connection;
        }

        private void ReplaceInBulk(IReadOnlyCollection<Type> dataObjectTypes, StorageDescriptor sourceStorageDescriptor, DataConnection targetConnection, IReadOnlyCollection<ICommand> replicationCommands)
        {
            DataConnection sourceConnection;

            // Creating connection to source that will NOT be enlisted in transactions
            using (var scope = new TransactionScope(TransactionScopeOption.Suppress))
            {
                sourceConnection = CreateDataConnection(sourceStorageDescriptor);
                if (sourceConnection.Connection.State != ConnectionState.Open)
                {
                    sourceConnection.Connection.Open();
                }

                scope.Complete();
            }

            using (sourceConnection)
            {
                var actorsFactory = new ReplaceDataObjectsInBulkActorFactory(dataObjectTypes, sourceConnection, targetConnection);
                var actors = actorsFactory.Create();

                foreach (var actor in actors)
                {
                    var sw = Stopwatch.StartNew();
                    actor.ExecuteCommands(replicationCommands);
                    sw.Stop();

                    Console.WriteLine($"[{DateTime.Now}] [{Environment.CurrentManagedThreadId}] {actor.GetType().GetFriendlyName()}: {sw.Elapsed.TotalSeconds} seconds");
                }
            }
        }
    }
}