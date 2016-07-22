namespace NuClear.Replication.OperationsProcessing.Transports
{
    public interface IBinaryWriter
    {
        void Write(byte[] buffer);

        void Write(byte[] buffer, int index, int count);
    }
}