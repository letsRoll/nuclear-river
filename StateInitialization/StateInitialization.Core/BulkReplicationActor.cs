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
using NuClear.StateInitialization.Core.Actors;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.DataObjects;
using NuClear.StateInitialization.Core.Events;
using NuClear.StateInitialization.Core.Factories;
using NuClear.StateInitialization.Core.Settings;
using NuClear.StateInitialization.Core.Storage;
using NuClear.Storage.API.ConnectionStrings;

using IsolationLevel = System.Transactions.IsolationLevel;

namespace NuClear.StateInitialization.Core
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
        private readonly IDbSchemaManagementSettings _schemaManagementSettings;

        public BulkReplicationActor(
            IDataObjectTypesProviderFactory dataObjectTypesProviderFactory,
            IConnectionStringSettings connectionStringSettings,
            IDbSchemaManagementSettings schemaManagementSettings)
        {
            _dataObjectTypesProviderFactory = dataObjectTypesProviderFactory;
            _connectionStringSettings = connectionStringSettings;
            _schemaManagementSettings = schemaManagementSettings;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            foreach (var command in commands.Cast<ReplicateInBulkCommand>())
            {
                var commandStopwatch = Stopwatch.StartNew();

                var dataObjectTypes = GetDataObjectTypes(_dataObjectTypesProviderFactory.Create(command));

                if (command.ExecutionMode == ExecutionMode.Parallel)
                {
                    IReadOnlyCollection<IEvent> schemaChangedEvents = null;
                    ExecuteInTransactionScope(
                        command,
                        (targetConnection, schemaManagenentActor) =>
                            {
                                schemaChangedEvents = schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCommands());
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
                else
                {
                    ExecuteInTransactionScope(
                        command,
                        (targetConnection, schemaManagenentActor) =>
                            {
                                var schemaChangedEvents = schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCommands());

                                foreach (var dataObjectType in dataObjectTypes)
                                {
                                    ReplaceInBulk(dataObjectType, command.SourceStorageDescriptor, targetConnection, command.BulkCopyTimeout);
                                }

                                schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCompensationalCommands(schemaChangedEvents));
                            });
                }

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

        private static IReadOnlyCollection<ICommand> CreateSchemaChangesCommands()
        {
            return new ICommand[] { new DropViewsCommand(), new DisableContraintsCommand() };
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
            connection.CommandTimeout = (int)TimeSpan.FromMinutes(storageDescriptor.CommandTimeout).TotalMilliseconds;
            return connection;
        }

        private SequentialPipelineActor CreateDbSchemaManagementActor(SqlConnection sqlConnection, int commandTimeout)
        {
            var timeout = (int)TimeSpan.FromMinutes(commandTimeout).TotalSeconds;
            return new SequentialPipelineActor(
                new IActor[]
                    {
                        new ViewManagementActor(sqlConnection, timeout, _schemaManagementSettings),
                        new ConstraintsManagementActor(sqlConnection, timeout, _schemaManagementSettings)
                    });
        }

        private void ReplaceInBulk(Type dataObjectType, StorageDescriptor sourceStorageDescriptor, DataConnection targetConnection, int bulkCopyTimeout)
        {
            var firstStageCommands = new ICommand[]
                                         {
                                             new DisableIndexesCommand(targetConnection.MappingSchema),
                                             new ReplaceDataObjectsInBulkCommand(bulkCopyTimeout)
                                         };

            var secondStageCommands = new ICommand[]
                                          {
                                              new EnableIndexesCommand(targetConnection.MappingSchema),
                                              new UpdateTableStatisticsCommand(targetConnection.MappingSchema)
                                          };

            DataConnection sourceConnection;
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

                Action<IReadOnlyCollection<ICommand>> execute =
                    commands =>
                        {
                            foreach (var actor in actors)
                            {
                                var sw = Stopwatch.StartNew();
                                actor.ExecuteCommands(commands);
                                sw.Stop();

                                Console.WriteLine($"[{DateTime.Now}]: {actor.GetType().GetFriendlyName()}, {sw.Elapsed.TotalSeconds} seconds");
                            }
                        };

                execute(firstStageCommands);
                execute(secondStageCommands);
            }
        }
    }
}