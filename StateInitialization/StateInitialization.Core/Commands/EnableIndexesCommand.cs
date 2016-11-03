using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class EnableIndexesCommand : ICommand
    {
        public EnableIndexesCommand(string tableName)
        {
            TableName = tableName;
        }

        public string TableName { get; }
    }
}