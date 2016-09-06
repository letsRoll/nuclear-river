using System;
using System.Collections.Generic;

namespace NuClear.StateInitialization.Core.DataObjects
{
    internal interface ICommandRegardlessDataObjectTypesProvider
    {
        IReadOnlyCollection<Type> Get();
    }
}