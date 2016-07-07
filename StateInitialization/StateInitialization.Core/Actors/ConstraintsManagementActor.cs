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
                var tables = GetTables();
                var tableConstraints = tables.ToDictionary(
                    x => x,
                    x => new
                             {
                                 Checks = x.Checks.Cast<Check>().Where(c => c.IsEnabled),
                                 ForeignKeys = x.ForeignKeys.Cast<ForeignKey>().Where(fk => fk.IsEnabled)
                             });

                foreach (var table in tableConstraints)
                {
                    foreach (var check in table.Value.Checks)
                    {
                        check.IsEnabled = false;
                    }

                    foreach (var foreignKey in table.Value.ForeignKeys)
                    {
                        foreignKey.IsEnabled = false;
                    }

                    table.Key.Alter();
                }

                return new[]
                           {
                               new ConstraintsDisabledEvent(
                                   tableConstraints.ToDictionary(x => x.Key.Name, x => x.Value.Checks.Select(c => c.Name)),
                                   tableConstraints.ToDictionary(x => x.Key.Name, x => x.Value.ForeignKeys.Select(c => c.Name)))
                           };
            }

            var enableCommand = commands.OfType<EnableConstraintsCommand>().SingleOrDefault();
            if (enableCommand != null)
            {
                var tables = GetTables();
                foreach (var table in tables)
                {
                    IEnumerable<string> checkNames;
                    if (enableCommand.Checks.TryGetValue(table.Name, out checkNames))
                    {
                        var checks = table.Checks.Cast<Check>().Where(c => checkNames.Contains(c.Name)).ToArray();
                        foreach (var check in checks)
                        {
                            check.IsEnabled = true;
                        }
                    }

                    IEnumerable<string> foreignKeyNames;
                    if (enableCommand.ForeignKeys.TryGetValue(table.Name, out foreignKeyNames))
                    {
                        var foreignKeys = table.ForeignKeys.Cast<ForeignKey>().Where(fk => foreignKeyNames.Contains(fk.Name));
                        foreach (var foreignKey in foreignKeys)
                        {
                            foreignKey.IsEnabled = true;
                        }
                    }

                    table.Alter();
                }
            }

            return Array.Empty<IEvent>();
        }

        private IEnumerable<Table> GetTables()
        {
            var database = _sqlConnection.GetDatabase();
            return database.Tables.OfType<Table>().Where(x => !x.IsSystemObject).ToArray();
        }
    }
}