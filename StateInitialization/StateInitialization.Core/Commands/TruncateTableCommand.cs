using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class TruncateTableCommand : ICommand
    {
        public TruncateTableCommand(string tableName)
        {
            TableName = tableName;
        }

        public string TableName { get; }
    }
}