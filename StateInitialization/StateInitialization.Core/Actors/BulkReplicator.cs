using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Data;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.DataObjects;
using NuClear.StateInitialization.Core.Factories;
using NuClear.StateInitialization.Core.Storage;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;

namespace NuClear.StateInitialization.Core.Actors
{
    internal sealed class BulkReplicator
    {
        private readonly IAccessorTypesProvider _accessorTypesProvider;

        public BulkReplicator(IAccessorTypesProvider accessorTypesProvider)
        {
            _accessorTypesProvider = accessorTypesProvider;
        }

        public void ReplicateStorage(IReadOnlyCollection<Type> dataObjectTypes,
                              DataConnection sourceConnection,
                              DataConnection targetConnection,
                              IReadOnlyCollection<ICommand> replicationCommands)
        {
            var actors = CreateStorageActors(dataObjectTypes, sourceConnection, targetConnection);
            Replicate(actors, targetConnection, replicationCommands);
        }

        public void ReplicateMemory(IReadOnlyCollection<Type> dataObjectTypes,
                                    IEnumerable<ICommand> memoryReplicationCommands,
                                    DataConnection targetConnection,
                                    IReadOnlyCollection<ICommand> replicationCommands)
        {
            var actors = CreateMemoryActors(dataObjectTypes, memoryReplicationCommands, targetConnection);
            Replicate(actors, targetConnection, replicationCommands);
        }

        private IEnumerable<IActor> CreateStorageActors(
            IReadOnlyCollection<Type> dataObjectTypes,
            DataConnection sourceDataConnection,
            DataConnection targetDataConnection)
        {
            return dataObjectTypes.SelectMany(x =>
            {
                var accessorTypes = _accessorTypesProvider.GetStorageAccessorTypes(x);

                return accessorTypes.Select(y =>
                {
                    var accessor = Activator.CreateInstance(y, new LinqToDbQuery(sourceDataConnection));
                    var actorType = typeof(BulkInsertDataObjectsActor<>).MakeGenericType(x);
                    return (IActor)Activator.CreateInstance(actorType, accessor, targetDataConnection);
                });
            });
        }

        private static void Replicate(IEnumerable<IActor> actors, DataConnection targetConnection, IReadOnlyCollection<ICommand> replicationCommands)
        {
            var allActors = CreateBeforeActors(targetConnection).Concat(actors).Concat(CreateAfterActors(targetConnection));
            foreach (var actor in allActors)
            {
                actor.ExecuteCommands(replicationCommands);
            }
        }

        private IEnumerable<IActor> CreateMemoryActors(
            IReadOnlyCollection<Type> dataObjectTypes,
            IEnumerable<ICommand> commands,
            DataConnection targetDataConnection)
        {
            return dataObjectTypes.SelectMany(x =>
            {
                var accessorTypes = _accessorTypesProvider.GetMemoryAccessorTypes(x);

                return accessorTypes.Select(y =>
                {
                    var accessor = Activator.CreateInstance(y, (IQuery)null);
                    var accessorAdapterType = typeof(MemoryToStorageAccessorAdapter<>).MakeGenericType(x);
                    var accessorAdapter = Activator.CreateInstance(accessorAdapterType, accessor, commands);
                    var actorType = typeof(BulkInsertDataObjectsActor<>).MakeGenericType(x);
                    return (IActor)Activator.CreateInstance(actorType, accessorAdapter, targetDataConnection);
                });
            });
        }

        private static IEnumerable<IActor> CreateBeforeActors(DataConnection targetDataConnection)
        {
            return new []
            {
                (IActor)Activator.CreateInstance(typeof(CreateTableCopyActor), targetDataConnection.Connection),
                (IActor)Activator.CreateInstance(typeof(DisableIndexesActor), targetDataConnection.Connection),
                (IActor)Activator.CreateInstance(typeof(TruncateTableActor), targetDataConnection)
            };
        }

        private static IEnumerable<IActor> CreateAfterActors(DataConnection targetDataConnection)
        {
            return new[]
            {
                (IActor)Activator.CreateInstance(typeof(EnableIndexesActor), targetDataConnection.Connection),
                (IActor)Activator.CreateInstance(typeof(UpdateTableStatisticsActor), targetDataConnection.Connection),
            };
        }

        private sealed class MemoryToStorageAccessorAdapter<TDataObject> : IStorageBasedDataObjectAccessor<TDataObject>
        {
            private readonly IMemoryBasedDataObjectAccessor<TDataObject> _accessor;
            private readonly IEnumerable<ICommand> _commands;

            public MemoryToStorageAccessorAdapter(IMemoryBasedDataObjectAccessor<TDataObject> accessor, IEnumerable<ICommand> commands)
            {
                _accessor = accessor;
                _commands = commands;
            }

            public IQueryable<TDataObject> GetSource() => _commands.SelectMany(x => _accessor.GetDataObjects(x)).AsQueryable();

            public FindSpecification<TDataObject> GetFindSpecification(IReadOnlyCollection<ICommand> commands) => throw new NotSupportedException();
        }
    }
}