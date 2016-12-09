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
    public sealed class CreateTableCopyActor : IActor
    {
        private readonly Database _database;

        public CreateTableCopyActor(SqlConnection sqlConnection)
        {
            _database = sqlConnection.GetDatabase();
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

                var createdTableCopy = CopyTable(sourceTable, command.TargetTable);
                CopyIndexes(sourceTable, createdTableCopy);

                Console.WriteLine($"[{DateTime.Now}] [{Environment.CurrentManagedThreadId}] Created shadow copy of table {command.SourceTable} named {command.TargetTable} with {sourceTable.Indexes.Count} indexes");
            }

            return Array.Empty<IEvent>();
        }

        private Table CopyTable(Table sourceTable, TableName targetTable)
        {
            var newTable = string.IsNullOrEmpty(targetTable.Schema)
                ? new Table(_database, targetTable.Table)
                : new Table(_database, targetTable.Table, targetTable.Schema);

            foreach (Column col in sourceTable.Columns)
            {
                var newCol = new Column(newTable, col.Name, col.DataType, col.IsFileStream)
                {
                    Collation = col.Collation,
                    Computed = col.Computed,
                    ComputedText = col.ComputedText,
                    DefaultSchema = col.DefaultSchema,
                    Identity = col.Identity,
                    IdentityIncrement = col.IdentityIncrement,
                    IdentitySeed = col.IdentitySeed,
                    IsColumnSet = col.IsColumnSet,
                    IsFileStream = col.IsFileStream,
                    IsPersisted = col.IsPersisted,
                    IsSparse = col.IsSparse,
                    NotForReplication = col.NotForReplication,
                    Nullable = col.Nullable,
                    RowGuidCol = col.RowGuidCol
                };

                newTable.Columns.Add(newCol);
            }

            newTable.Create();

            return newTable;
        }

        private void CopyIndexes(Table sourceTable, Table targetTable)
        {
            foreach (Index index in sourceTable.Indexes)
            {
                try
                {
                    var newIndex = CreateIndexCopy(targetTable, index);
                    newIndex.Create();
                }
                catch (Exception ex)
                {
                    throw new DataException($"Error occured while creating shadow copy of index {index.Name} from table {sourceTable.Name} in table {targetTable.Name}", ex);
                }
            }
        }

        private Index CreateIndexCopy(Table targetTable, Index index)
        {
            var newIndex = new Index(targetTable, CreateTableCopyCommand.Prefix + index.Name)
            {
                BoundingBoxXMax = index.BoundingBoxXMax,
                BoundingBoxXMin = index.BoundingBoxXMin,
                BoundingBoxYMax = index.BoundingBoxYMax,
                BoundingBoxYMin = index.BoundingBoxYMin,
                BucketCount = index.BucketCount,
                CellsPerObject = index.CellsPerObject,
                CompactLargeObjects = index.CompactLargeObjects,
                DisallowPageLocks = index.DisallowPageLocks,
                DisallowRowLocks = index.DisallowRowLocks,
                FileGroup = index.FileGroup,
                FileStreamFileGroup = index.FileStreamFileGroup,
                FileStreamPartitionScheme = index.FileStreamPartitionScheme,
                FillFactor = index.FillFactor,
                FilterDefinition = index.FilterDefinition,
                IgnoreDuplicateKeys = index.IgnoreDuplicateKeys,
                IndexKeyType = index.IndexKeyType,
                IndexType = index.IndexType,
                IndexedXmlPathName = index.IndexedXmlPathName,
                IsClustered = index.IsClustered,
                IsFullTextKey = index.IsFullTextKey,
                IsMemoryOptimized = index.IsMemoryOptimized,
                IsUnique = index.IsUnique,
                Level1Grid = index.Level1Grid,
                Level2Grid = index.Level2Grid,
                Level3Grid = index.Level3Grid,
                Level4Grid = index.Level4Grid,
                LowPriorityAbortAfterWait = index.LowPriorityAbortAfterWait,
                LowPriorityMaxDuration = index.LowPriorityMaxDuration,
                MaximumDegreeOfParallelism = index.MaximumDegreeOfParallelism,
                NoAutomaticRecomputation = index.NoAutomaticRecomputation,
                OnlineIndexOperation = index.OnlineIndexOperation,
                PadIndex = index.PadIndex,
                ParentXmlIndex = index.ParentXmlIndex,
                PartitionScheme = index.PartitionScheme,
                SecondaryXmlIndexType = index.SecondaryXmlIndexType,
                SortInTempdb = index.SortInTempdb,
                SpatialIndexType = index.SpatialIndexType
            };

            foreach (IndexedColumn column in index.IndexedColumns)
            {
                var newColumn = new IndexedColumn(newIndex, column.Name, column.Descending)
                {
                    IsIncluded = column.IsIncluded
                };

                newIndex.IndexedColumns.Add(newColumn);
            }
            return newIndex;
        }
    }
}
