using System.Collections.Generic;
using System.Collections.Specialized;

using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class EnableConstraintsCommand : ICommand
    {
        public EnableConstraintsCommand(
            IReadOnlyCollection<StringCollection> checksToRestore,
            IReadOnlyCollection<StringCollection> foreignKeysToRestore)
        {
            ChecksToRestore = checksToRestore;
            ForeignKeysToRestore = foreignKeysToRestore;
        }

        public IReadOnlyCollection<StringCollection> ChecksToRestore { get; }
        public IReadOnlyCollection<StringCollection> ForeignKeysToRestore { get; }
    }
}