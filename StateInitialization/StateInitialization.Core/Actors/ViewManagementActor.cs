using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Linq;

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Events;
using NuClear.StateInitialization.Core.Settings;

namespace NuClear.StateInitialization.Core.Actors
{
    public class ViewManagementActor : IActor
    {
        private readonly SqlConnection _sqlConnection;
        private readonly IDbSchemaManagementSettings _schemaManagementSettings;

        public ViewManagementActor(SqlConnection sqlConnection, IDbSchemaManagementSettings schemaManagementSettings)
        {
            _sqlConnection = sqlConnection;
            _schemaManagementSettings = schemaManagementSettings;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            if (!_schemaManagementSettings.DisableViews)
            {
                return Array.Empty<IEvent>();
            }

            if (commands.OfType<DropViewsCommand>().Any())
            {
                var database = GetDatabase(_sqlConnection);
                var views = database.Views.Cast<View>().Where(v => !v.IsSystemObject).ToArray();

                var viewsToRestore = new List<StringCollection>();
                foreach (var view in views)
                {
                    viewsToRestore.Add(view.Script());
                    view.Drop();
                }

                return new[] { new ViewsDroppedEvent(viewsToRestore) };
            }

            var restoreViewsCommand = commands.OfType<RestoreViewsCommand>().SingleOrDefault();
            if (restoreViewsCommand != null)
            {
                foreach (var view in restoreViewsCommand.ViewsToRestore)
                {
                    foreach (var script in view)
                    {
                        var command = new SqlCommand(script, _sqlConnection);
                        command.ExecuteNonQuery();
                    }
                }
            }

            return Array.Empty<IEvent>();
        }

        private static Database GetDatabase(SqlConnection sqlConnection)
        {
            var connection = new ServerConnection(sqlConnection);
            var server = new Server(connection);

            var connectionStringBuilder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
            return server.Databases[connectionStringBuilder.InitialCatalog];
        }
    }
}