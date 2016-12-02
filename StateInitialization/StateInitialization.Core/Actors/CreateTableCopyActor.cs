using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.SqlServer.Management.Smo;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Storage;

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
                var sourceTable = database.GetTable(command.SourceTable);

                // If table with prefix already exists - drop it:
                var targetTable = database.GetTable(command.TargetTable);
                if (targetTable != null)
                {
                    targetTable.Drop();
                    Console.WriteLine($"[{DateTime.Now}] [{Environment.CurrentManagedThreadId}] Dropped old shadow copy table {command.TargetTable}");
                }

                CopyTable(sourceTable);
                CopyIndexes(sourceTable, command.TargetTable);

                Console.WriteLine($"[{DateTime.Now}] [{Environment.CurrentManagedThreadId}] Created shadow copy of table {command.SourceTable} named {command.TargetTable} with {sourceTable.Indexes.Count} indexes");
            }

            return Array.Empty<IEvent>();
        }

        private void CopyTable(Table sourceTable)
        {
            var scripts = sourceTable.Script();
            foreach (var script in scripts)
            {
                if (script.IndexOf("CREATE TABLE", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    var index = script.IndexOf(sourceTable.Name, StringComparison.OrdinalIgnoreCase);
                    var newScript = script.Insert(index, CreateTableCopyCommand.Prefix);
                    Debug.WriteLine(newScript);
                    var createTableCommand = new SqlCommand(newScript, _sqlConnection);
                    createTableCommand.ExecuteNonQuery();
                }
            }
        }

        private void CopyIndexes(Table sourceTable, TableName targetTable)
        {
            foreach (Index index in sourceTable.Indexes)
            {
                foreach (var script in index.Script())
                {
                    var updatedScript = GetIndexCopyCreationScript(script, sourceTable.Name, targetTable.Table, index.Name);
                    try
                    {
                        Debug.WriteLine(updatedScript);
                        var createIndexCommand = new SqlCommand(updatedScript, _sqlConnection);
                        createIndexCommand.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        throw new DataException(
                                  $"Error occured while creating shadow copy of index {index.Name} from table {sourceTable.Name} in table {targetTable} with script:{Environment.NewLine}{updatedScript}",
                                  ex);
                    }
                }
            }
        }

        private string GetIndexCopyCreationScript(string createIndexScript, string sourceTableName, string targetTableName, string indexName)
        {
            var tableNamePosition = createIndexScript.IndexOf($"[{sourceTableName}]", StringComparison.OrdinalIgnoreCase);
            var indexNamePosition = createIndexScript.IndexOf($"[{indexName}]", StringComparison.OrdinalIgnoreCase);

            if (tableNamePosition < 0 || indexNamePosition < 0)
            {
                return createIndexScript;
            }

            var sb = new StringBuilder(createIndexScript);
            if (tableNamePosition > indexNamePosition)
            {
                sb.Remove(tableNamePosition, sourceTableName.Length + 2)
                  .Insert(tableNamePosition, $"[{targetTableName}]")
                  .Insert(indexNamePosition + 1, CreateTableCopyCommand.Prefix);    // Change index name at the end
            }
            else
            {
                sb.Insert(indexNamePosition + 1, CreateTableCopyCommand.Prefix) // Change index name at the beginning
                  .Remove(tableNamePosition, sourceTableName.Length + 2)
                  .Insert(tableNamePosition, $"[{targetTableName}]");
            }

            return sb.ToString();
        }
    }
}
