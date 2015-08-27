﻿using System;

using NuClear.AdvancedSearch.Replication.Model;

namespace NuClear.AdvancedSearch.Replication.CustomerIntelligence.Model.Erm
{
    public abstract class ActivityBase : IErmObject
    {
        public long Id { get; set; }
        public int Status { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset ModifiedOn { get; set; }

        public override bool Equals(object obj)
        {
            return obj != null && obj.GetType() == this.GetType() && IdentifiableObjectEqualityComparer<ActivityBase>.Default.Equals(this, (ActivityBase)obj);
        }

        public override int GetHashCode()
        {
            return IdentifiableObjectEqualityComparer<ActivityBase>.Default.GetHashCode(this);
        }
    }
}