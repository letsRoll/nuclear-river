using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class UpdateTableStatisticsCommand : ICommand
    {
        public UpdateTableStatisticsCommand(string tableName)
        {
            TableName = tableName;
        }

        public string TableName { get; }
    }
}