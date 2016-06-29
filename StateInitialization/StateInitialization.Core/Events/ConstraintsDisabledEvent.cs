using System.Collections.Generic;

using Microsoft.SqlServer.Management.Smo;

using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Events
{
    public sealed class ConstraintsDisabledEvent : IEvent
    {
        public ConstraintsDisabledEvent(IReadOnlyCollection<Check> checks, IReadOnlyCollection<ForeignKey> foreignKeys)
        {
            Checks = checks;
            ForeignKeys = foreignKeys;
        }

        public IReadOnlyCollection<Check> Checks { get; }
        public IReadOnlyCollection<ForeignKey> ForeignKeys { get; }
    }
}