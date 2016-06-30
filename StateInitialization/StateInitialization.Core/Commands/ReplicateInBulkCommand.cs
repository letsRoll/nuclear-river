using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    public enum ExecutionMode
    {
        Sequential = 1,
        Parallel
    }

    public sealed class ReplicateInBulkCommand : ICommand
    {
        public ReplicateInBulkCommand(
            StorageDescriptor sourceStorageDescriptor,
            StorageDescriptor targetStorageDescriptor,
            int bulkCopyTimeout = 1800,
            ExecutionMode executionMode = ExecutionMode.Parallel)
        {
            SourceStorageDescriptor = sourceStorageDescriptor;
            TargetStorageDescriptor = targetStorageDescriptor;
            BulkCopyTimeout = bulkCopyTimeout;
            ExecutionMode = executionMode;
        }

        public StorageDescriptor SourceStorageDescriptor { get; }
        public StorageDescriptor TargetStorageDescriptor { get; }
        public int BulkCopyTimeout { get; set; }
        public ExecutionMode ExecutionMode { get; set; }
    }
}