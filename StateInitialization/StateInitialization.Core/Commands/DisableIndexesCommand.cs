using LinqToDB.Mapping;

using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class DisableIndexesCommand : ICommand
    {
        public DisableIndexesCommand(MappingSchema mappingSchema)
        {
            MappingSchema = mappingSchema;
        }

        public MappingSchema MappingSchema { get; }
    }
}