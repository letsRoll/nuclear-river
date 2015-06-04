﻿using System.Collections.Generic;
using System.Linq;

using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Data.Context;
using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Data.Context.Implementation;
using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Transforming.Operations;
using NuClear.AdvancedSearch.Replication.Data;

namespace NuClear.AdvancedSearch.Replication.CustomerIntelligence.Transforming
{
    using CI = CustomerIntelligence.Model;

    public sealed class BitFactsTransformation
    {
        private readonly IDataMapper _mapper;
        private readonly IBitFactsContext _bitFactsContext;

        public BitFactsTransformation(IBitFactsContext bitFactsContext, IDataMapper mapper)
        {
            _mapper = mapper;
            _bitFactsContext = bitFactsContext;
        }

        public IEnumerable<AggregateOperation> Transform(FirmStatisticsDto dto)
        {
            var transformationContext = new BitFactsTransformationContext(dto);

            var firmsBefore = _bitFactsContext.FirmStatistics.Where(stat => stat.ProjectId == dto.ProjectId).Select(stat => stat.FirmId).Distinct().ToArray();
            
            _mapper.DeleteAll(_bitFactsContext.FirmStatistics);
            _mapper.InsertAll(transformationContext.FirmStatistics);

            var firmsAfter = transformationContext.FirmStatistics.Where(stat => stat.ProjectId == dto.ProjectId).Select(stat => stat.FirmId).Distinct().ToArray();

            return firmsBefore.Union(firmsAfter).Select(id => new RecalculateAggregate(typeof(CI.Firm), id));
        }

        public IEnumerable<AggregateOperation> Transform(CategoryStatisticsDto dto)
        {
            var transformationContext = new BitFactsTransformationContext(dto);

            _mapper.DeleteAll(_bitFactsContext.CategoryStatistics);
            _mapper.InsertAll(transformationContext.CategoryStatistics);

            return new [] { new RecalculateAggregate(typeof(CI.Project), dto.ProjectId) };
        }
    }

    public class FirmStatisticsDto
    {
        public long ProjectId { get; set; }

        public IEnumerable<FirmDto> Firms { get; set; }

        public class FirmDto
        {
            public long FirmId { get; set; }
            public long CategoryId { get; set; }
            public int Hits { get; set; }
            public int Shows { get; set; }
        }
    }

    public class CategoryStatisticsDto
    {
        public long ProjectId { get; set; }
        public IEnumerable<CategoryDto> Categories { get; set; }

        public class CategoryDto
        {
            public long CategoryId { get; set; }

            public int AdvertisersCount { get; set; }
        }
    }

}
