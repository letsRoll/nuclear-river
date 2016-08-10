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

                var executionStrategy = DetermineExecutionStrategy(command);
                executionStrategy.Invoke(command, dataObjectTypes);

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

        private Action<ReplicateInBulkCommand, IEnumerable<Type>> DetermineExecutionStrategy(ReplicateInBulkCommand command)
        {
            switch (command.ExecutionMode)
            {
                case ExecutionMode.Parallel:
                    return ParallelExecutionStrategy;
                case ExecutionMode.Sequential:
                    return SequentialExecutionStrategy;
                default:
                    throw new ArgumentException($"Execution mode {command.ExecutionMode} is not supported", nameof(command));
            }
        }

        private void ParallelExecutionStrategy(ReplicateInBulkCommand command, IEnumerable<Type> dataObjectTypes)
        {
            IReadOnlyCollection<IEvent> schemaChangedEvents = null;
            ExecuteInTransactionScope(
                command,
                (targetConnection, schemaManagenentActor) =>
                {
                    schemaChangedEvents = schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCommands(command.DbManagementMode));
                });

            Parallel.ForEach(
                    dataObjectTypes,
                    dataObjectType =>
                    {
                        using (var connection = CreateDataConnection(command.TargetStorageDescriptor))
                        {
                            ReplaceInBulk(dataObjectType, command.SourceStorageDescriptor, connection, command.BulkCopyTimeout);
                        }
                    });

            ExecuteInTransactionScope(
                command,
                (targetConnection, schemaManagenentActor) =>
                {
                    schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCompensationalCommands(schemaChangedEvents));
                });
        }

        private void SequentialExecutionStrategy(ReplicateInBulkCommand command, IEnumerable<Type> dataObjectTypes)
        {
            ExecuteInTransactionScope(
                command,
                (targetConnection, schemaManagenentActor) =>
                {
                    var schemaChangedEvents = schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCommands(command.DbManagementMode));

                    foreach (var dataObjectType in dataObjectTypes)
                    {
                        ReplaceInBulk(dataObjectType, command.SourceStorageDescriptor, targetConnection, command.BulkCopyTimeout);
                    }

                    schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCompensationalCommands(schemaChangedEvents));
                });
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

        private void ReplaceInBulk(Type dataObjectType, StorageDescriptor sourceStorageDescriptor, DataConnection targetConnection, TimeSpan bulkCopyTimeout)
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
                var actorsFactory = new ReplaceDataObjectsInBulkActorFactory(dataObjectType, sourceConnection, targetConnection);
                var actors = actorsFactory.Create();

                foreach (var actor in actors)
                {
                    var sw = Stopwatch.StartNew();
                    actor.ExecuteCommands(
                        new ICommand[]
                            {
                                new DisableIndexesCommand(targetConnection.MappingSchema),
                                new ReplaceDataObjectsInBulkCommand(bulkCopyTimeout),
                                new EnableIndexesCommand(targetConnection.MappingSchema),
                                new UpdateTableStatisticsCommand(targetConnection.MappingSchema)
                            });
                    sw.Stop();

                    Console.WriteLine($"[{DateTime.Now}] [{Environment.CurrentManagedThreadId}] {actor.GetType().GetFriendlyName()}: {sw.Elapsed.TotalSeconds} seconds");
                }
            }
        }
    }
}