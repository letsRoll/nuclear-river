using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.DataObjects;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.StateInitialization.Core.Actors
{
    internal sealed class BulkInsertDataObjectsActor<TDataObject> : IActor
        where TDataObject : class
    {
        private readonly IQueryable<TDataObject> _dataObjectsSource;
        private readonly DataConnection _targetDataConnection;
        private readonly string _dataObjectAccessorName;

        public BulkInsertDataObjectsActor(IStorageBasedDataObjectAccessor<TDataObject> dataObjectAccessor, DataConnection targetDataConnection)
        {
            _dataObjectAccessorName = dataObjectAccessor.GetType().Name;
            _dataObjectsSource = dataObjectAccessor.GetSource();
            _targetDataConnection = targetDataConnection;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var command = commands.OfType<BulkInsertDataObjectsCommand>().SingleOrDefault();
            if (command != null)
            {
                ExecuteBulkCopy((int)command.BulkCopyTimeout.TotalSeconds, command.TargetTable.Table);
            }

            return Array.Empty<IEvent>();
        }

        private void ExecuteBulkCopy(int timeout, string tableName)
        {
            try
            {
                var options = new BulkCopyOptions { BulkCopyTimeout = timeout };
                var table = _targetDataConnection.GetTable<TDataObject>();
                table.TableName(tableName)
                    .BulkCopy(options, _dataObjectsSource);
            }
            catch (Exception ex)
            {
                string sqlText;
                try
                {
                    var linq2DBQuery = _dataObjectsSource as IExpressionQuery<TDataObject>;
                    sqlText = linq2DBQuery?.SqlText;
                }
                catch (Exception innerException)
                {
                    sqlText = $"can not build sql query: {innerException.Message}";
                }

                throw new DataException($"Error occured while bulk replacing data for dataobject of type {typeof(TDataObject).Name} using {_dataObjectAccessorName}{Environment.NewLine}{sqlText}{Environment.NewLine}", ex);
            }
        }
    }
}