using System;
using System.Collections.Generic;

namespace NuClear.StateInitialization.Core.Factories
{
    /// <summary>
    /// Позволяет управлять соответствием между типами объектов данных и accessors, выполняющими копирование этих объектов
    /// </summary>
    public interface IAccessorTypesProvider
    {
        IReadOnlyCollection<Type> GetStorageAccessorTypes(Type dataObjectType);
        IReadOnlyCollection<Type> GetMemoryAccessorTypes(Type dataObjectType);
    }
}