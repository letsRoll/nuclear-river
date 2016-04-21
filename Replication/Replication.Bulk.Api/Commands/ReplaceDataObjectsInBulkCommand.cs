﻿using NuClear.Replication.Bulk.API.Storage;
using NuClear.River.Common.Metadata;

namespace NuClear.Replication.Bulk.API.Commands
{
    public sealed class ReplaceDataObjectsInBulkCommand : ICommand
    {
        public ReplaceDataObjectsInBulkCommand(StorageDescriptor sourceStorageDescriptor, StorageDescriptor targetStorageDescriptor)
        {
            SourceStorageDescriptor = sourceStorageDescriptor;
            TargetStorageDescriptor = targetStorageDescriptor;
        }

        public StorageDescriptor SourceStorageDescriptor { get; }
        public StorageDescriptor TargetStorageDescriptor { get; }
    }
}