using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    internal sealed class CreateTableCopyCommand : ICommand
    {
        public CreateTableCopyCommand(TableName table)
        {
            SourceTable = table;
            CreatedTable = new TableName(Prefix + table.Table, table.Schema);
        }

        public TableName SourceTable { get; }

        public TableName CreatedTable { get; }

        public static string Prefix => "river_";
    }
}
