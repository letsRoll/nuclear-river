using System.Collections.Generic;
using System.Linq;

using NuClear.Storage.API.Specifications;

namespace NuClear.Replication.Core.DataObjects
{
    public interface IQueryableEnumerator
    {
        IEnumerable<T> Invoke<T>(IQueryable<T> queryable, FindSpecification<T> specification);
    }
}