using LinqToDB.Mapping;

using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class UpdateTableStatisticsCommand : ICommand
    {
        public UpdateTableStatisticsCommand(MappingSchema mappingSchema)
        {
            MappingSchema = mappingSchema;
        }

        public MappingSchema MappingSchema { get; }
    }
}