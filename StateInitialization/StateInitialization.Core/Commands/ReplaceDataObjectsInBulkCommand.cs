using System;

using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class ReplaceDataObjectsInBulkCommand : ICommand
    {
        public ReplaceDataObjectsInBulkCommand(TimeSpan bulkCopyTimeout)
        {
            BulkCopyTimeout = bulkCopyTimeout;
        }

        public TimeSpan BulkCopyTimeout { get; }
    }
}