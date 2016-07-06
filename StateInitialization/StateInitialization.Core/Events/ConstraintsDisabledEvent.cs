using System.Collections.Generic;

using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Events
{
    public sealed class ConstraintsDisabledEvent : IEvent
    {
        public ConstraintsDisabledEvent(
            IReadOnlyDictionary<string, IEnumerable<string>> checks,
            IReadOnlyDictionary<string, IEnumerable<string>> foreignKeys)
        {
            Checks = checks;
            ForeignKeys = foreignKeys;
        }

        public IReadOnlyDictionary<string, IEnumerable<string>> Checks { get; }
        public IReadOnlyDictionary<string, IEnumerable<string>> ForeignKeys { get; }
    }
}