using NuClear.CustomerIntelligence.StateInitialization.Host;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.CustomerIntelligence.Replication.StateInitialization.Tests
{
    public interface IKey
    {
        ReplicateInBulkCommand Command { get; }
    }

    public sealed class Facts : IKey
    {
        public ReplicateInBulkCommand Command => BulkReplicationCommands.ErmToFacts.Sequential();
    }

    public sealed class CustomerIntelligence : IKey
    {
        public ReplicateInBulkCommand Command => BulkReplicationCommands.FactsToCi.Sequential();
    }
}