using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class DisableIndexesCommand : ICommand
    {
        public DisableIndexesCommand(Table table)
        {
            Table = table;
        }

        public Table Table{ get; }
    }
}