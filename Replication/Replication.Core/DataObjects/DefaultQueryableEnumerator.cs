using System.Collections.Generic;
using System.Linq;

using NuClear.Storage.API.Specifications;

namespace NuClear.Replication.Core.DataObjects
{
    internal sealed class DefaultQueryableEnumerator : IQueryableEnumerator
    {
        public IEnumerable<T> Invoke<T>(IQueryable<T> queryable, FindSpecification<T> specification)
        {
            return queryable.Where(specification);
        }
    }
}