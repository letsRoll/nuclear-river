using System;
using System.Collections.Generic;

using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    [Flags]
    public enum DbManagementMode
    {
        None = 0,
        DropAndRecreateViews = 1,
        DropAndRecreateConstraints = 2,
        EnableIndexManagment = 4,
        UpdateTableStatistics = 8,
        TruncateTable = 16,

        All = DropAndRecreateConstraints | EnableIndexManagment | UpdateTableStatistics | TruncateTable
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class ReplicateInBulkCommand : ReplicateInBulkCommandBase
    {
        public ReplicateInBulkCommand(
            StorageDescriptor sourceStorageDescriptor,
            StorageDescriptor targetStorageDescriptor,
            DbManagementMode databaseManagementMode = DbManagementMode.All,
            ExecutionMode executionMode = null,
            TimeSpan? bulkCopyTimeout = null)
            : base(targetStorageDescriptor, databaseManagementMode, bulkCopyTimeout)
        {
            SourceStorageDescriptor = sourceStorageDescriptor;
            ExecutionMode = executionMode ?? ExecutionMode.Parallel;
        }

        public StorageDescriptor SourceStorageDescriptor { get; }
        public ExecutionMode ExecutionMode { get; }
    }

    public sealed class MemoryReplicateInBulkCommand : ReplicateInBulkCommandBase
    {
        public MemoryReplicateInBulkCommand(
            IEnumerable<ICommand> replicationCommands,
            StorageDescriptor targetStorageDescriptor,
            DbManagementMode databaseManagementMode = DbManagementMode.All,
            TimeSpan? bulkCopyTimeout = null)
            : base(targetStorageDescriptor, databaseManagementMode, bulkCopyTimeout)
        {
            ReplicationCommands = replicationCommands;
        }

        public IEnumerable<ICommand> ReplicationCommands { get; }
    }

    public abstract class ReplicateInBulkCommandBase : ICommand
    {
        private static readonly TimeSpan DefaultBulkCopyTimeout = TimeSpan.FromMinutes(30);

        protected ReplicateInBulkCommandBase(StorageDescriptor targetStorageDescriptor, DbManagementMode databaseManagementMode, TimeSpan? bulkCopyTimeout)
        {
            TargetStorageDescriptor = targetStorageDescriptor;
            DbManagementMode = databaseManagementMode;
            BulkCopyTimeout = bulkCopyTimeout ?? DefaultBulkCopyTimeout;
        }

        public StorageDescriptor TargetStorageDescriptor { get; }
        public DbManagementMode DbManagementMode { get; }

        public TimeSpan BulkCopyTimeout { get; }
    }
}
