using LinqToDB.Mapping;

using NuClear.Storage.API.ConnectionStrings;

namespace NuClear.StateInitialization.Core.Storage
{
    public sealed class StorageDescriptor
    {
        public StorageDescriptor(IConnectionStringIdentity connectionStringIdentity, MappingSchema mappingSchema, int commandTimeout = 30)
        {
            ConnectionStringIdentity = connectionStringIdentity;
            MappingSchema = mappingSchema;
            CommandTimeout = commandTimeout;
        }

        public IConnectionStringIdentity ConnectionStringIdentity { get; }
        public MappingSchema MappingSchema { get; }
        public int CommandTimeout { get; set; }
    }
}