﻿using NuClear.AdvancedSearch.Common.Metadata.Model;

namespace NuClear.CustomerIntelligence.Domain.Model.Bit
{
    public class FirmCategoryStatistics : IFactStatisticsObject
    {
        public long FirmId { get; set; }

        public long ProjectId { get; set; }

        public long CategoryId { get; set; }

        public long Hits { get; set; }

        public long Shows { get; set; }
    }
}