using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class EnableIndexesActor<TDataObject> : IActor
    {
        private readonly IActor _indexesManagementActor;

        public EnableIndexesActor(SqlConnection sqlConnection)
        {
            _indexesManagementActor = new IndexesManagementActor<TDataObject>(sqlConnection);
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var enableCommand = commands.OfType<EnableIndexesCommand>().SingleOrDefault();
            return enableCommand == null ? Array.Empty<IEvent>() : _indexesManagementActor.ExecuteCommands(new[] { enableCommand });
        }
    }
}