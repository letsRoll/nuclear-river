using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Data;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class TruncateTableActor : IActor
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
                ExecuteTruncate(command.TableName);
            }

            return Array.Empty<IEvent>();
        }

        private void ExecuteTruncate(string tableName)
        {
            _targetDataConnection.Execute($"TRUNCATE TABLE {tableName}");
        }
    }
}