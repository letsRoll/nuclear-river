using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using LinqToDB.Mapping;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class UpdateTableStatisticsActor<TDataObject> : IActor
    {
        private readonly SqlConnection _sqlConnection;

        public UpdateTableStatisticsActor(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var command = commands.OfType<UpdateTableStatisticsCommand>().SingleOrDefault();
            if (command == null)
            {
                return Array.Empty<IEvent>();
            }

            var attributes = command.MappingSchema.GetAttributes<TableAttribute>(typeof(TDataObject));
            var tableName = attributes.Select(x => x.Name).FirstOrDefault() ?? typeof(TDataObject).Name;
            var schemaName = attributes.Select(x => x.Schema).FirstOrDefault();
            try
            {
                var database = _sqlConnection.GetDatabase();
                var table = !string.IsNullOrEmpty(schemaName) ? database.Tables[tableName, schemaName] : database.Tables[tableName];
                table.UpdateStatistics();

                return Array.Empty<IEvent>();
            }
            catch (Exception ex)
            {
                throw new DataException($"Error occured while statistics updating for table {tableName}", ex);
            }
        }
    }
}