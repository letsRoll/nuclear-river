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
        private readonly IndexManagementService _indexManagementService;

        public EnableIndexesActor(SqlConnection sqlConnection)
        {
            _indexManagementService = new IndexManagementService(sqlConnection);
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var disableCommand = commands.OfType<DisableIndexesCommand>().SingleOrDefault();
            if (disableCommand != null)
            {
                _indexManagementService.EnableIndexes(disableCommand.MappingSchema, typeof(TDataObject));
            }

            return Array.Empty<IEvent>();
        }
    }
}