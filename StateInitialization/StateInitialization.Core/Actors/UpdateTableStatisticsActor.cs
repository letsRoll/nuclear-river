using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.Mapping;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class UpdateTableStatisticsActor<TDataObject> : IActor
    {
        private readonly DataConnection _dataConnection;

        public UpdateTableStatisticsActor(DataConnection dataConnection)
        {
            _dataConnection = dataConnection;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {


            var attributes = _dataConnection.MappingSchema.GetAttributes<TableAttribute>(typeof(TDataObject));
            var tableName = attributes.Select(x => x.Name).FirstOrDefault() ?? typeof(TDataObject).Name;
            try
            {
                var schemaName = attributes.Select(x => x.Schema).FirstOrDefault();
                var builder = new SqlCommandBuilder();
                if (!string.IsNullOrEmpty(schemaName))
                {
                    tableName = builder.QuoteIdentifier(tableName);
                    schemaName = builder.QuoteIdentifier(schemaName);
                    _dataConnection.Execute($"UPDATE STATISTICS {schemaName}.{tableName}");
                }
                else
                {
                    tableName = builder.QuoteIdentifier(tableName);
                    _dataConnection.Execute($"UPDATE STATISTICS {tableName}");
                }

                return Array.Empty<IEvent>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occured while statistics updating for table {tableName}{Environment.NewLine}{_dataConnection.LastQuery}", ex); ;
            }
        }
    }
}