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
                var constraints = tables.ToDictionary(
                    x => x.Name,
                    x => new
                             {
                                 Checks = x.Checks.Cast<Check>().Where(c => c.IsEnabled).ToArray(),
                                 ForeignKeys = x.ForeignKeys.Cast<ForeignKey>().Where(fk => fk.IsEnabled).ToArray()
                             });

                foreach (var tableConstraints in constraints)
                {
                    foreach (var check in tableConstraints.Value.Checks)
                    {
                        check.IsEnabled = false;

                        Console.WriteLine($"Disabling check constraint [{check.Name}]...");
                        check.Alter();
                    }

                    foreach (var foreignKey in tableConstraints.Value.ForeignKeys)
                    {
                        foreignKey.IsEnabled = false;

                        Console.WriteLine($"Disabling foreign key constraint [{foreignKey.Name}]...");
                        foreignKey.Alter();
                    }
                }

                return new[]
                           {
                               new ConstraintsDisabledEvent(
                                   constraints.ToDictionary(x => x.Key, x => x.Value.Checks.Select(c => c.Name)),
                                   constraints.ToDictionary(x => x.Key, x => x.Value.ForeignKeys.Select(c => c.Name)))
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

                            Console.WriteLine($"Enabling check constraint [{check.Name}]...");
                            check.Alter();
                        }
                    }

                    IEnumerable<string> foreignKeyNames;
                    if (enableCommand.ForeignKeys.TryGetValue(table.Name, out foreignKeyNames))
                    {
                        var foreignKeys = table.ForeignKeys.Cast<ForeignKey>().Where(fk => foreignKeyNames.Contains(fk.Name));
                        foreach (var foreignKey in foreignKeys)
                        {
                            foreignKey.IsEnabled = true;

                            Console.WriteLine($"Enabling foreign key constraint [{foreignKey.Name}]...");
                            foreignKey.Alter();
                        }
                    }
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