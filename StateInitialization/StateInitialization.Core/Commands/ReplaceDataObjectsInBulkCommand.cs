using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class ReplaceDataObjectsInBulkCommand : ICommand
    {
        public ReplaceDataObjectsInBulkCommand(int bulkCopyTimeout)
        {
            BulkCopyTimeout = bulkCopyTimeout;
        }

        public int BulkCopyTimeout { get;  }
    }
}