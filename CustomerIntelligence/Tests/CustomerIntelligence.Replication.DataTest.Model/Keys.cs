﻿using NuClear.CustomerIntelligence.Storage;
using NuClear.Replication.Bulk.API.Commands;
using NuClear.Replication.Bulk.API.Storage;

namespace NuClear.CustomerIntelligence.Replication.StateInitialization.Tests
{
    public interface IKey
    {
        ReplaceDataObjectsInBulkCommand Command { get; }
    }

    public sealed class Facts : IKey
    {
        public ReplaceDataObjectsInBulkCommand Command => new ReplaceDataObjectsInBulkCommand(
            new StorageDescriptor(ContextName.Erm, Schema.Erm),
            new StorageDescriptor(ContextName.Facts, Schema.Facts));
    }

    public sealed class CustomerIntelligence : IKey
    {
        public ReplaceDataObjectsInBulkCommand Command => new ReplaceDataObjectsInBulkCommand(
            new StorageDescriptor(ContextName.Facts, Schema.Facts),
            new StorageDescriptor(ContextName.CustomerIntelligence, Schema.CustomerIntelligence));
    }
}