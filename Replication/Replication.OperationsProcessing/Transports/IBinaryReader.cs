namespace NuClear.Replication.OperationsProcessing.Transports
{
    public interface IBinaryReader
    {
        int Read(byte[] buffer, int index, int count);

        byte[] ReadToEnd();
    }
}