using System.Collections.Generic;

using Microsoft.SqlServer.Management.Smo;

using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class EnableConstraintsCommand : ICommand
    {
        public EnableConstraintsCommand(IReadOnlyCollection<Check> checks, IReadOnlyCollection<ForeignKey> foreignKeys)
        {
            Checks = checks;
            ForeignKeys = foreignKeys;
        }

        public IReadOnlyCollection<Check> Checks { get; }

        public IReadOnlyCollection<ForeignKey> ForeignKeys { get; }
    }
}