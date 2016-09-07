using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.Mapping;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class TruncateTableActor<TDataObject> : IActor
        where TDataObject : class
    {
        private readonly DataConnection _targetDataConnection;

        public TruncateTableActor(DataConnection targetDataConnection)
        {
            _targetDataConnection = targetDataConnection;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var command = commands.OfType<TruncateTableCommand>().SingleOrDefault();
            if (command != null)
            {
                ExecuteTruncate();
            }

            return Array.Empty<IEvent>();
        }

        private void ExecuteTruncate()
        {
            var attributes = _targetDataConnection.MappingSchema.GetAttributes<TableAttribute>(typeof(TDataObject));
            var tableName = attributes.Select(x => x.Name).FirstOrDefault() ?? typeof(TDataObject).Name;

            var schemaName = attributes.Select(x => x.Schema).FirstOrDefault();
            tableName = string.IsNullOrEmpty(schemaName)
                            ? $"[{tableName}]"
                            : $"[{schemaName}].[{tableName}]";

            _targetDataConnection.Execute($"TRUNCATE TABLE {tableName}");
        }
    }
}