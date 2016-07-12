using LinqToDB.Mapping;

using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class EnableIndexesCommand : ICommand
    {
        public EnableIndexesCommand(MappingSchema mappingSchema)
        {
            MappingSchema = mappingSchema;
        }

        public MappingSchema MappingSchema { get; }
    }
}