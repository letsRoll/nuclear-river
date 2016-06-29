using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class ReplicateInBulkCommand : ICommand
    {
        public ReplicateInBulkCommand(StorageDescriptor sourceStorageDescriptor, StorageDescriptor targetStorageDescriptor)
        {
            SourceStorageDescriptor = sourceStorageDescriptor;
            TargetStorageDescriptor = targetStorageDescriptor;
        }

        public StorageDescriptor SourceStorageDescriptor { get; }
        public StorageDescriptor TargetStorageDescriptor { get; }
    }
}