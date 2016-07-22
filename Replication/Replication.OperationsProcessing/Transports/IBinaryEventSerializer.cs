using NuClear.Replication.Core;

namespace NuClear.Replication.OperationsProcessing.Transports
{
    public interface IBinaryEventSerializer
    {
        void Serialize(IBinaryWriter writer, IEvent @event);

        IEvent Deserialize(IBinaryReader binaryReader);
    }
}