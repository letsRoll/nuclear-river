using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Microsoft.SqlServer.Management.Smo;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class ReplaceTableActor : IActor
    {
        private readonly int _commandTimeout;
        private readonly SqlConnection _sqlConnection;

        public ReplaceTableActor(SqlConnection sqlConnection, TimeSpan commandTimeout)
        {
            _sqlConnection = sqlConnection;
            _commandTimeout = (int)commandTimeout.TotalSeconds;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var replaceCommands = commands.OfType<ReplaceTableCommand>().ToList();
            if (replaceCommands.Count > 0)
            {
                var tablesIndexes = new Dictionary<TableName, List<string>>();
                var database = _sqlConnection.GetDatabase();

                // Replacing tables:
                foreach (var command in replaceCommands)
                {
                    try
                    {
                        var tableToReplace = database.GetTable(command.TableToReplace);
                        var replacementTable = database.GetTable(command.ReplacementTable);

                        tablesIndexes.Add(
                            command.TableToReplace,
                            new List<string>(
                                tableToReplace.Indexes
                                              .OfType<Index>()
                                              .SelectMany(i => i.Script().OfType<string>())));

                        tableToReplace.Drop();
                        replacementTable.Rename(tableToReplace.Name);

                        Console.WriteLine($"[{DateTime.Now}] Replaced table {command.TableToReplace} with {command.ReplacementTable}");
                    }
                    catch (Exception ex)
                    {
                        throw new DataException($"Error occured while replacing table {command.TableToReplace} with {command.ReplacementTable}", ex);
                    }
                }

                // Restoring indexes:
                foreach (var tableIndexes in tablesIndexes)
                {
                    foreach (var indexRestoringScript in tableIndexes.Value)
                    {
                        try
                        {
                            var createCommand = new SqlCommand(indexRestoringScript, _sqlConnection) { CommandTimeout = _commandTimeout };
                            createCommand.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            throw new DataException($"Error occured while restoring index on table {tableIndexes.Key} with script:{Environment.NewLine}{indexRestoringScript}", ex);
                        }
                    }

                    Console.WriteLine($"[{DateTime.Now}] Restored {tableIndexes.Value.Count} indexes on table {tableIndexes.Key}");
                }
            }

            return Array.Empty<IEvent>();
        }
    }
}
