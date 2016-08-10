using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class DisableIndexesActor<TDataObject> : IActor
    {
        private readonly IActor _indexesManagementActor;

        public DisableIndexesActor(SqlConnection sqlConnection)
        {
            _indexesManagementActor = new IndexesManagementActor<TDataObject>(sqlConnection);
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var disableCommand = commands.OfType<DisableIndexesCommand>().SingleOrDefault();
            return disableCommand == null ? Array.Empty<IEvent>() : _indexesManagementActor.ExecuteCommands(new[] { disableCommand });
        }
    }
}