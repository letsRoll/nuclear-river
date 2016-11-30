using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Data;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.DataObjects;
using NuClear.StateInitialization.Core.Actors;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Factories
{
    public sealed class ReplaceDataObjectsInBulkActorFactory : IDataObjectsActorFactory
    {
        private static readonly IReadOnlyDictionary<Type, Type[]> AccessorTypes =
            (from type in AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic).SelectMany(x => x.ExportedTypes)
             from @interface in type.GetInterfaces()
             where !type.IsAbstract && @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IStorageBasedDataObjectAccessor<>)
             select new { GenericArgument = @interface.GetGenericArguments()[0], Type = type })
                .GroupBy(x => x.GenericArgument, x => x.Type)
                .ToDictionary(x => x.Key, x => x.ToArray());

        private readonly IReadOnlyCollection<Type> _dataObjectTypes;
        private readonly DataConnection _sourceDataConnection;
        private readonly DataConnection _targetDataConnection;

        public ReplaceDataObjectsInBulkActorFactory(
            IReadOnlyCollection<Type> dataObjectTypes,
            DataConnection sourceDataConnection,
            DataConnection targetDataConnection)
        {
            _dataObjectTypes = dataObjectTypes;
            _sourceDataConnection = sourceDataConnection;
            _targetDataConnection = targetDataConnection;
        }

        public IReadOnlyCollection<IActor> Create()
        {
            var actors = new List<IActor>();

            var disableIndexesActorType = typeof(DisableIndexesActor);
            var disableIndexesActor = (IActor)Activator.CreateInstance(disableIndexesActorType, _targetDataConnection.Connection);
            actors.Add(disableIndexesActor);

            var truncateTableActorType = typeof(TruncateTableActor);
            var truncateTableActor = (IActor)Activator.CreateInstance(truncateTableActorType, _targetDataConnection);
            actors.Add(truncateTableActor);

            var createTableCopyActorType = typeof(CreateTableCopyActor);
            var createTableCopyActor = (IActor)Activator.CreateInstance(createTableCopyActorType, _targetDataConnection.Connection);
            actors.Add(createTableCopyActor);

            foreach (var dataObjectType in _dataObjectTypes)
            {
                var accessorTypes = AccessorTypes[dataObjectType];
                foreach (var accessorType in accessorTypes)
                {
                    var accessorInstance = Activator.CreateInstance(accessorType, new LinqToDbQuery(_sourceDataConnection));
                    var replaceDataObjectsActorType = typeof(BulkInsertDataObjectsActor<>).MakeGenericType(dataObjectType);
                    var replaceDataObjectsActor = (IActor)Activator.CreateInstance(replaceDataObjectsActorType, accessorInstance, _targetDataConnection);
                    actors.Add(replaceDataObjectsActor);
                }
            }

            var enableIndexesActorType = typeof(EnableIndexesActor);
            var enableIndexesActor = (IActor)Activator.CreateInstance(enableIndexesActorType, _targetDataConnection.Connection);
            actors.Add(enableIndexesActor);

            var updateStatisticsActorType = typeof(UpdateTableStatisticsActor);
            var updateStatisticsActor = (IActor)Activator.CreateInstance(updateStatisticsActorType, _targetDataConnection.Connection);
            actors.Add(updateStatisticsActor);

            return actors;
        }
    }
}
