using System;
using System.Collections.Generic;

using NuClear.Replication.Core.DataObjects;

namespace NuClear.StateInitialization.Core.DataObjects
{
    public sealed class CommandRegardlessDataObjectTypesProvider : IDataObjectTypesProvider, ICommandRegardlessDataObjectTypesProvider
    {
        private readonly IReadOnlyCollection<Type> _dataObjectTypes;

        public CommandRegardlessDataObjectTypesProvider(IReadOnlyCollection<Type> dataObjectTypes)
        {
            _dataObjectTypes = dataObjectTypes;
        }

        IReadOnlyCollection<Type> IDataObjectTypesProvider.Get<TCommand>()
        {
            var regardlessProviderName = typeof(ICommandRegardlessDataObjectTypesProvider).Name;
            throw new NotSupportedException($"Instance of type {GetType().Name} must be used throught {regardlessProviderName} interface only. Try to cast it to {regardlessProviderName}");
        }

        IReadOnlyCollection<Type> ICommandRegardlessDataObjectTypesProvider.Get()
        {
            return _dataObjectTypes;
        }
    }
}