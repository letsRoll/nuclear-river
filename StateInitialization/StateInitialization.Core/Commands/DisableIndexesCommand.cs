using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class DisableIndexesCommand : ICommand
    {
        public DisableIndexesCommand(string tableName)
        {
            TableName = tableName;
        }

        public string TableName { get; }
    }
}