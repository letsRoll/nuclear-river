using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    internal sealed class CreateTableCopyCommand : ICommand
    {
        public CreateTableCopyCommand(TableName table)
        {
            SourceTable = table;
            CopiedTable = GetTableCopyName(table);
        }

        public static string Prefix => "river_";

        public TableName SourceTable { get; }

        public TableName CopiedTable { get; }

        public static TableName GetTableCopyName(TableName table)
        {
            return new TableName(Prefix + table.Table, table.Schema);
        }
    }
}
