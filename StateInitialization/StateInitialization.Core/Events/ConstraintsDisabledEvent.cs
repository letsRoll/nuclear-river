using System.Collections.Generic;
using System.Collections.Specialized;

using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Events
{
    public sealed class ConstraintsDisabledEvent : IEvent
    {
        public ConstraintsDisabledEvent(IReadOnlyCollection<StringCollection> checks, IReadOnlyCollection<StringCollection> foreignKeys)
        {
            Checks = checks;
            ForeignKeys = foreignKeys;
        }

        public IReadOnlyCollection<StringCollection> Checks { get; }
        public IReadOnlyCollection<StringCollection> ForeignKeys { get; }
    }
}