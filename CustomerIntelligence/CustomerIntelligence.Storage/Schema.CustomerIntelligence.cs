using LinqToDB;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NuClear.CustomerIntelligence.Storage.Model.CI;
using NuClear.CustomerIntelligence.Storage.Model.Statistics;

namespace NuClear.CustomerIntelligence.Storage
{
    public static partial class Schema
    {
        private const string AggregatesSchema = "Aggregates";

        public static MappingSchema CustomerIntelligence
        {
            get
            {
                var schema = new MappingSchema(nameof(CustomerIntelligence), new SqlServerMappingSchema());
                var config = schema.GetFluentMappingBuilder();

                config.Entity<CategoryGroup>()
                    .HasSchemaName(AggregatesSchema)
                    .HasPrimaryKey(x => x.Id);

                config.Entity<Client>()
                    .HasSchemaName(AggregatesSchema)
                    .HasPrimaryKey(x => x.Id);

                config.Entity<ClientContact>()
                    .HasSchemaName(AggregatesSchema);

                config.Entity<Firm>()
                    .HasSchemaName(AggregatesSchema)
                    .HasPrimaryKey(x => x.Id);

                config.Entity<FirmLead>()
                      .HasSchemaName(AggregatesSchema);

                config.Entity<FirmActivity>()
                    .HasSchemaName(AggregatesSchema);

                config.Entity<FirmBalance>()
                    .HasSchemaName(AggregatesSchema);

                config.Entity<FirmCategory1>()
                    .HasSchemaName(AggregatesSchema);

                config.Entity<FirmCategory2>()
                    .HasSchemaName(AggregatesSchema);

                config.Entity<FirmTerritory>()
                    .HasSchemaName(AggregatesSchema);

                config.Entity<Project>()
                    .HasSchemaName(AggregatesSchema)
                    .HasPrimaryKey(x => x.Id);

                config.Entity<ProjectCategory>()
                    .HasSchemaName(AggregatesSchema);

                config.Entity<Territory>()
                    .HasSchemaName(AggregatesSchema)
                    .HasPrimaryKey(x => x.Id);

                config.Entity<ProjectStatistics>()
                    .HasSchemaName(AggregatesSchema)
                    .HasPrimaryKey(x => x.Id);

                config.Entity<ProjectCategoryStatistics>()
                    .HasSchemaName(AggregatesSchema);

                config.Entity<FirmForecast>()
                    .HasSchemaName(AggregatesSchema)
                    .HasPrimaryKey(x => x.FirmId);

                config.Entity<FirmCategory3>()
                    .HasSchemaName(AggregatesSchema);

                schema.SetDataType(typeof(decimal), new SqlDataType(DataType.Decimal, 19, 4));
                schema.SetDataType(typeof(decimal?), new SqlDataType(DataType.Decimal, 19, 4));
                schema.SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, int.MaxValue));

                return schema;
            }
        }
    }
}