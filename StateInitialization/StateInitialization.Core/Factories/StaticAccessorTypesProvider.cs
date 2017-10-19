using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NuClear.Replication.Core.DataObjects;

namespace NuClear.StateInitialization.Core.Factories
{
    public sealed class StaticAccessorTypesProvider : IAccessorTypesProvider
    {
        private static readonly Dictionary<Type, List<Type>> StorageAccessorTypes = new Dictionary<Type, List<Type>>();
        private static readonly Dictionary<Type, List<Type>> MemoryAccessorTypes = new Dictionary<Type, List<Type>>();

        static StaticAccessorTypesProvider()
        {
            var tuples = AppDomain.CurrentDomain.GetAssemblies()
                     .Where(x => !x.IsDynamic)
                     .SelectMany(SafeGetAssemblyExportedTypes)
                     .SelectMany(type => type.GetInterfaces(), (type, @interface) => new { type, @interface })
                     .Where(x => !x.type.IsAbstract && x.@interface.IsGenericType);

            foreach (var tuple in tuples)
            {
                var interfaceDefinition = tuple.@interface.GetGenericTypeDefinition();

                if (interfaceDefinition == typeof(IStorageBasedDataObjectAccessor<>))
                {
                    var key = tuple.@interface.GetGenericArguments()[0];
                    if (!StorageAccessorTypes.TryGetValue(key, out var list))
                    {
                        list = new List<Type>();
                        StorageAccessorTypes.Add(key, list);
                    }
                    list.Add(tuple.type);
                }

                if (interfaceDefinition == typeof(IMemoryBasedDataObjectAccessor<>))
                {
                    var key = tuple.@interface.GetGenericArguments()[0];
                    if (!MemoryAccessorTypes.TryGetValue(key, out var list))
                    {
                        list = new List<Type>();
                        MemoryAccessorTypes.Add(key, list);
                    }
                    list.Add(tuple.type);
                }
            }
        }

        private static IEnumerable<Type> SafeGetAssemblyExportedTypes(Assembly assembly)
        {
            try
            {
                return assembly.ExportedTypes;
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        public IReadOnlyCollection<Type> GetStorageAccessorTypes(Type dataObjectType)
        {
            if (StorageAccessorTypes.TryGetValue(dataObjectType, out var result))
            {
                return result;
            }

            return Array.Empty<Type>();
        }

        public IReadOnlyCollection<Type> GetMemoryAccessorTypes(Type dataObjectType)
        {
            if (MemoryAccessorTypes.TryGetValue(dataObjectType, out var result))
            {
                return result;
            }

            return Array.Empty<Type>();
        }
    }
}