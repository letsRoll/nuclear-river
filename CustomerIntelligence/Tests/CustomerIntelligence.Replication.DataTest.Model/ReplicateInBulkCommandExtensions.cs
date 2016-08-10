using NuClear.StateInitialization.Core.Commands;

namespace NuClear.CustomerIntelligence.Replication.StateInitialization.Tests
{
    public static class ReplicateInBulkCommandExtensions
    {
        public static ReplicateInBulkCommand Sequential(this ReplicateInBulkCommand command)
        {
            return new ReplicateInBulkCommand(
                command.SourceStorageDescriptor,
                command.TargetStorageDescriptor,
                command.DbManagementMode,
                ExecutionMode.Sequential,
                command.BulkCopyTimeout);
        }
    }
}