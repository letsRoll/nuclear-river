using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class ReplicateInBulkCommand : ICommand
    {
        public ReplicateInBulkCommand(
            StorageDescriptor sourceStorageDescriptor,
            StorageDescriptor targetStorageDescriptor,
            int bulkCopyTimeout = 1800)
        {
            SourceStorageDescriptor = sourceStorageDescriptor;
            TargetStorageDescriptor = targetStorageDescriptor;
            BulkCopyTimeout = bulkCopyTimeout;
        }

        public StorageDescriptor SourceStorageDescriptor { get; }
        public StorageDescriptor TargetStorageDescriptor { get; }
        public int BulkCopyTimeout { get; set; }
    }
}