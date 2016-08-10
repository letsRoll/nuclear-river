using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using LinqToDB.Mapping;

using Microsoft.SqlServer.Management.Smo;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class IndexesManagementActor<TDataObject> : IActor
    {
        private enum IndexesManagementMode
        {
            Enable,
            Disable
        }

        private readonly SqlConnection _sqlConnection;

        public IndexesManagementActor(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var disableCommand = commands.OfType<DisableIndexesCommand>().SingleOrDefault();
            if (disableCommand != null)
            {
                return ExecuteCommand(disableCommand.MappingSchema, IndexesManagementMode.Disable);
            }

            var enableCommand = commands.OfType<EnableIndexesCommand>().SingleOrDefault();
            if (enableCommand != null)
            {
                return ExecuteCommand(enableCommand.MappingSchema, IndexesManagementMode.Enable);
            }

            return Array.Empty<IEvent>();
        }

        private IReadOnlyCollection<IEvent> ExecuteCommand(MappingSchema mappingSchema, IndexesManagementMode indexesManagementMode)
        {
            var attributes = mappingSchema.GetAttributes<TableAttribute>(typeof(TDataObject));
            var tableName = attributes.Select(x => x.Name).FirstOrDefault() ?? typeof(TDataObject).Name;
            var schemaName = attributes.Select(x => x.Schema).FirstOrDefault();

            try
            {
                var database = _sqlConnection.GetDatabase();
                var table = !string.IsNullOrEmpty(schemaName) ? database.Tables[tableName, schemaName] : database.Tables[tableName];

                if (indexesManagementMode == IndexesManagementMode.Disable)
                {
                    foreach (var index in table.Indexes.Cast<Index>().Where(x => x != null && !x.IsClustered && !x.IsDisabled))
                    {
                        index.Alter(IndexOperation.Disable);
                    }
                }
                else
                {
                    table.EnableAllIndexes(IndexEnableAction.Rebuild);
                }

                return Array.Empty<IEvent>();

            }
            catch (Exception ex)
            {
                throw new DataException($"Error occured while enabling or disabling indexes for table {tableName}", ex);
            }
        }
    }
}