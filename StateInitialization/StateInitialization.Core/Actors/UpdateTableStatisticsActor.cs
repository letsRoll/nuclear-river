using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class UpdateTableStatisticsActor : IActor
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

            try
            {
                var database = _sqlConnection.GetDatabase();
                var table = string.IsNullOrEmpty(command.Table.SchemaName) ? database.Tables[command.Table.TableName] : database.Tables[command.Table.TableName, command.Table.SchemaName];
                table.UpdateStatistics();

                return Array.Empty<IEvent>();
            }
            catch (Exception ex)
            {
                throw new DataException($"Error occured while statistics updating for table {command.Table}", ex);
            }
        }
    }
}