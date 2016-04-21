﻿using NuClear.Replication.Bulk.API.Commands;
using NuClear.Replication.Core.API;

namespace NuClear.Replication.Bulk.API.Factories
{
    public interface IDataObjectTypesProviderFactory
    {
        IDataObjectTypesProvider Create(ReplaceDataObjectsInBulkCommand command);
    }
}