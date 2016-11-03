using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class EnableIndexesCommand : ICommand
    {
        public EnableIndexesCommand(Table table)
        {
            Table = table;
        }

        public Table Table { get; }
    }
}