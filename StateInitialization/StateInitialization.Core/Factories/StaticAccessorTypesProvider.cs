using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core.DataObjects;

namespace NuClear.StateInitialization.Core.Factories
{
    public sealed class StaticAccessorTypesProvider : IAccessorTypesProvider
    {
        private static readonly IReadOnlyDictionary<Type, Type[]> AccessorTypes =
            (from type in AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic).SelectMany(x => x.ExportedTypes)
             from @interface in type.GetInterfaces()
             where !type.IsAbstract && @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IStorageBasedDataObjectAccessor<>)
             select new { GenericArgument = @interface.GetGenericArguments()[0], Type = type })
            .GroupBy(x => x.GenericArgument, x => x.Type)
            .ToDictionary(x => x.Key, x => x.ToArray());

        public IReadOnlyCollection<Type> GetAccessorsFor(Type dataObjectType)
        {
            return AccessorTypes[dataObjectType];
        }
    }
}