using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using Microsoft.SqlServer.Management.Smo;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class CreateTableCopyActor : IActor
    {
        private readonly SqlConnection _sqlConnection;

        public CreateTableCopyActor(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var command = commands.OfType<CreateTableCopyCommand>().SingleOrDefault();
            if (command != null)
            {
                var database = _sqlConnection.GetDatabase();
                var table = database.GetTable(command.SourceTable);

                // If table with prefix already exists - drop it:
                var existedTable = database.GetTable(command.CopiedTable);
                existedTable?.Drop();

                var scripts = table.Script();
                foreach (var script in scripts)
                {
                    if (script.IndexOf("CREATE TABLE", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        var index = script.IndexOf(command.SourceTable.Table, StringComparison.OrdinalIgnoreCase);
                        var newScript = script.Insert(index, CreateTableCopyCommand.Prefix);
                        var createCommand = new SqlCommand(newScript, _sqlConnection);
                        createCommand.ExecuteNonQuery();
                        break;
                    }
                }
            }

            return Array.Empty<IEvent>();
        }
    }
}
