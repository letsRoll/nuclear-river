using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.Mapping;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.DataObjects;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class ReplaceDataObjectsInBulkActor<TDataObject> : IActor
        where TDataObject : class
    {
        private readonly IQueryable<TDataObject> _dataObjectsSource;
        private readonly DataConnection _targetDataConnection;

        public ReplaceDataObjectsInBulkActor(IStorageBasedDataObjectAccessor<TDataObject> dataObjectAccessor, DataConnection targetDataConnection)
        {
            _dataObjectsSource = dataObjectAccessor.GetSource();
            _targetDataConnection = targetDataConnection;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var command = commands.OfType<ReplaceDataObjectsInBulkCommand>().SingleOrDefault();
            if (command == null)
            {
                return Array.Empty<IEvent>();
            }

            var attributes = _targetDataConnection.MappingSchema.GetAttributes<TableAttribute>(typeof(TDataObject));
            var tableName = attributes.Select(x => x.Name).FirstOrDefault() ?? typeof(TDataObject).Name;

            var builder = new SqlCommandBuilder();
            tableName = builder.QuoteIdentifier(tableName);

            var schemaName = attributes.Select(x => x.Schema).FirstOrDefault();
            if (!string.IsNullOrEmpty(schemaName))
            {
                schemaName = builder.QuoteIdentifier(schemaName);
                tableName = $"{schemaName}.{tableName}";
            }

            try
            {
                _targetDataConnection.Execute($"TRUNCATE TABLE {tableName}");

                var options = new BulkCopyOptions { BulkCopyTimeout = (int)TimeSpan.FromMinutes(command.BulkCopyTimeout).TotalSeconds };
                _targetDataConnection.BulkCopy(options, _dataObjectsSource);

                return Array.Empty<IEvent>();
            }
            catch (Exception ex)
            {
                throw new DataException($"Error occured while bulk replacing data for dataobject of type {typeof(TDataObject).Name}{Environment.NewLine}{_targetDataConnection.LastQuery}", ex);
            }
        }
    }
}