using System;
using System.Collections.Generic;
using System.Linq;

namespace NuClear.Replication.Core.Actors
{
    public abstract class EntityActorBase<TDataObject> : IEntityActor
        where TDataObject : class
    {
        private readonly CreateDataObjectsActor<TDataObject> _createDataObjectsActor;
        private readonly SyncDataObjectsActor<TDataObject> _syncDataObjectsActor;
        private readonly DeleteDataObjectsActor<TDataObject> _deleteDataObjectsActor;

        protected EntityActorBase(
            CreateDataObjectsActor<TDataObject> createDataObjectsActor,
            SyncDataObjectsActor<TDataObject> syncDataObjectsActor,
            DeleteDataObjectsActor<TDataObject> deleteDataObjectsActorr)
        {
            _createDataObjectsActor = createDataObjectsActor;
            _syncDataObjectsActor = syncDataObjectsActor;
            _deleteDataObjectsActor = deleteDataObjectsActorr;
        }

        public Type EntityType => typeof(TDataObject);

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            if (!commands.Any())
            {
                return Array.Empty<IEvent>();
            }

            var events = new List<IEvent>();

            events.AddRange(_createDataObjectsActor.ExecuteCommands(commands));
            events.AddRange(_syncDataObjectsActor.ExecuteCommands(commands));
            events.AddRange(_deleteDataObjectsActor.ExecuteCommands(commands));

            return events;
        }

        public abstract IReadOnlyCollection<IActor> GetValueObjectActors();
    }
}