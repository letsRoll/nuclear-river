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
        private static readonly IReadOnlyDictionary<Type, Type> AccessorTypes =
            (from type in AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic).SelectMany(x => x.ExportedTypes)
             from @interface in type.GetInterfaces()
             where @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IStorageBasedDataObjectAccessor<>)
             select new { GenericArgument = @interface.GetGenericArguments()[0], Type = type })
                .ToDictionary(x => x.GenericArgument, x => x.Type);

        private readonly Type _dataObjectType;
        private readonly DataConnection _sourceDataConnection;
        private readonly DataConnection _targetDataConnection;

        public ReplaceDataObjectsInBulkActorFactory(
            Type dataObjectType,
            DataConnection sourceDataConnection,
            DataConnection targetDataConnection)
        {
            _dataObjectType = dataObjectType;
            _sourceDataConnection = sourceDataConnection;
            _targetDataConnection = targetDataConnection;
        }

        public IReadOnlyCollection<IActor> Create()
        {
            var actors = new List<IActor>();

            var indexesManagementActorType = typeof(IndexesManagementActor<>).MakeGenericType(_dataObjectType);
            var indexesManagementActor = (IActor)Activator.CreateInstance(indexesManagementActorType, _targetDataConnection.Connection);
            actors.Add(indexesManagementActor);

            var accessorType = AccessorTypes[_dataObjectType];
            var accessorInstance = Activator.CreateInstance(accessorType, new LinqToDbQuery(_sourceDataConnection));
            var replaceDataObjectsActorType = typeof(ReplaceDataObjectsInBulkActor<>).MakeGenericType(_dataObjectType);
            var replaceDataObjectsActor = (IActor)Activator.CreateInstance(replaceDataObjectsActorType, accessorInstance, _targetDataConnection);
            actors.Add(replaceDataObjectsActor);

            var updateStatisticsActorType = typeof(UpdateTableStatisticsActor<>).MakeGenericType(_dataObjectType);
            var updateStatisticsActor = (IActor)Activator.CreateInstance(updateStatisticsActorType, _targetDataConnection.Connection);
            actors.Add(updateStatisticsActor);

            return actors;
        }
    }
}