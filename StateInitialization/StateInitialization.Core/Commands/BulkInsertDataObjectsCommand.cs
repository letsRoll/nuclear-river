using System;

using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Commands
{
    internal sealed class BulkInsertDataObjectsCommand : ICommand
    {
        public BulkInsertDataObjectsCommand(TimeSpan bulkCopyTimeout, string targetTablePrefix = null)
        {
            BulkCopyTimeout = bulkCopyTimeout;
            TargetTablePrefix = targetTablePrefix ?? string.Empty;
        }

        public string TargetTablePrefix { get; }

        public TimeSpan BulkCopyTimeout { get; }
    }
}
