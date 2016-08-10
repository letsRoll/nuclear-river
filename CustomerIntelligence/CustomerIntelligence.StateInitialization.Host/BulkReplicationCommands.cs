using NuClear.CustomerIntelligence.Storage;
using NuClear.CustomerIntelligence.Storage.Identitites.Connections;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.CustomerIntelligence.StateInitialization.Host
{
    public static class BulkReplicationCommands
    {
        public static ReplicateInBulkCommand FactsToCi { get; } =
            new ReplicateInBulkCommand(
                new StorageDescriptor(FactsConnectionStringIdentity.Instance, Schema.Facts),
                new StorageDescriptor(CustomerIntelligenceConnectionStringIdentity.Instance, Schema.CustomerIntelligence));

        public static ReplicateInBulkCommand ErmToFacts { get; } =
            new ReplicateInBulkCommand(
                new StorageDescriptor(ErmConnectionStringIdentity.Instance, Schema.Erm),
                new StorageDescriptor(FactsConnectionStringIdentity.Instance, Schema.Facts),
                DbManagementMode.DropAndRecreateViews | DbManagementMode.DropAndRecreateConstraints);
    }
}