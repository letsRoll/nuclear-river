using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;

namespace NuClear.StateInitialization.Core.Actors
{
    internal sealed class CompositeActor : IActor
    {
        private readonly IReadOnlyCollection<IActor> _actors;

        public CompositeActor(IReadOnlyCollection<IActor> actors)
        {
            _actors = actors;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            return _actors.Aggregate(
                new List<IEvent>(),
                (events, actor) =>
                    {
                        events.AddRange(actor.ExecuteCommands(commands));
                        return events;
                    });
        }
    }
}