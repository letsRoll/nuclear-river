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
    public sealed class ReplaceTableActor : IActor
    {
        private readonly SqlConnection _sqlConnection;

        public ReplaceTableActor(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var replaceCommands = commands.OfType<ReplaceTableCommand>().ToList();
            if (replaceCommands.Count > 0)
            {
                var database = _sqlConnection.GetDatabase();
                foreach (var command in replaceCommands)
                {
                    var tableToReplace = database.GetTable(command.TableToReplace);
                    var replacementTable = database.GetTable(command.ReplacementTable);

                    var indexScripts = new List<string>(tableToReplace.Indexes.Count);
                    indexScripts.AddRange(tableToReplace.Indexes.OfType<Index>().SelectMany(i => i.Script().OfType<string>()));

                    tableToReplace.Drop();
                    replacementTable.Rename(tableToReplace.Name);
                    foreach (var indexScript in indexScripts)
                    {
                        var createCommand = new SqlCommand(indexScript, _sqlConnection);
                        createCommand.ExecuteNonQuery();
                    }
                }
            }

            return Array.Empty<IEvent>();
        }
    }
}
