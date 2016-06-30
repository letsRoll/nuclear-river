using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using Microsoft.SqlServer.Management.Smo;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Events;
using NuClear.StateInitialization.Core.Settings;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class ConstraintsManagementActor : IActor
    {
        private readonly SqlConnection _sqlConnection;
        private readonly IDbSchemaManagementSettings _schemaManagementSettings;

        public ConstraintsManagementActor(SqlConnection sqlConnection, IDbSchemaManagementSettings schemaManagementSettings)
        {
            _sqlConnection = sqlConnection;
            _schemaManagementSettings = schemaManagementSettings;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            if (!_schemaManagementSettings.DisableConstraints)
            {
                return Array.Empty<IEvent>();
            }

            if (commands.OfType<DisableContraintsCommand>().Any())
            {
                var database = _sqlConnection.GetDatabase();
                var tables = database.Tables.OfType<Table>().Where(x => !x.IsSystemObject).ToArray();

                var checks = tables.SelectMany(x => x.Checks.Cast<Check>().Where(c => c.IsEnabled)).ToArray();
                var foreignKeys = tables.SelectMany(x => x.ForeignKeys.Cast<ForeignKey>().Where(fk => fk.IsEnabled)).ToArray();

                foreach (var check in checks)
                {
                    check.IsEnabled = false;
                    check.Alter();
                }

                foreach (var foreignKey in foreignKeys)
                {
                    foreignKey.IsEnabled = false;
                    foreignKey.Alter();
                }

                return new[] { new ConstraintsDisabledEvent(checks, foreignKeys) };
            }

            var enableCommand = commands.OfType<EnableConstraintsCommand>().SingleOrDefault();
            if (enableCommand != null)
            {
                foreach (var check in enableCommand.Checks)
                {
                    check.IsEnabled = true;
                    check.Alter();
                }

                foreach (var foreignKey in enableCommand.ForeignKeys)
                {
                    foreignKey.IsEnabled = true;
                    foreignKey.Alter();
                }
            }

            return Array.Empty<IEvent>();
        }
    }
}