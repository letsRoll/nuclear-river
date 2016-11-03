using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class UpdateTableStatisticsCommand : ICommand
    {
        public UpdateTableStatisticsCommand(Table table)
        {
            Table = table;
        }

        public Table Table { get; }
    }
}