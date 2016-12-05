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
        private readonly Database _database;

        public CreateTableCopyActor(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
            _database = _sqlConnection.GetDatabase();
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var command = commands.OfType<CreateTableCopyCommand>().SingleOrDefault();
            if (command != null)
            {
                var sourceTable = _database.GetTable(command.SourceTable);

                // If table with prefix already exists - drop it:
                var targetTable = _database.GetTable(command.TargetTable);
                if (targetTable != null)
                {
                    targetTable.Drop();
                    Console.WriteLine($"[{DateTime.Now}] [{Environment.CurrentManagedThreadId}] Dropped old shadow copy table {command.TargetTable}");
                }

                CopyTable(sourceTable, command.TargetTable);
                CopyIndexes(sourceTable, command.TargetTable);

                Console.WriteLine($"[{DateTime.Now}] [{Environment.CurrentManagedThreadId}] Created shadow copy of table {command.SourceTable} named {command.TargetTable} with {sourceTable.Indexes.Count} indexes");
            }

            return Array.Empty<IEvent>();
        }

        private void CopyTable(Table sourceTable, TableName targetTable)
        {
            var newTable = string.IsNullOrEmpty(targetTable.Schema)
                ? new Table(_database, targetTable.Table)
                : new Table(_database, targetTable.Table, targetTable.Schema ?? "dbo");

            foreach (Column col in sourceTable.Columns)
            {
                var newCol = new Column(newTable, col.Name, col.DataType, col.IsFileStream)
                {
                    Collation = col.Collation,
                    Computed = col.Computed,
                    ComputedText = col.ComputedText,
                    Identity = col.Identity,
                    IdentityIncrement = col.IdentityIncrement,
                    IdentitySeed = col.IdentitySeed,
                    IsColumnSet = col.IsColumnSet,
                    IsPersisted = col.IsPersisted,
                    IsSparse = col.IsSparse,
                    NotForReplication = col.NotForReplication,
                    Nullable = col.Nullable,
                    RowGuidCol = col.RowGuidCol
                };

                newTable.Columns.Add(newCol);
            }

            newTable.Create();
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
