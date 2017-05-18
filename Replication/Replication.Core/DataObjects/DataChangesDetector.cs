using System.Collections;
using System.Collections.Generic;
using System.Transactions;

using NuClear.Storage.API.Specifications;

namespace NuClear.Replication.Core.DataObjects
{
    public class DataChangesDetector<T>
    {
        private readonly MapToObjectsSpecProvider<T, T> _sourceProvider;
        private readonly MapToObjectsSpecProvider<T, T> _targetProvider;
        private readonly IEqualityComparer<T> _comparer;

        public DataChangesDetector(
            MapToObjectsSpecProvider<T, T> sourceProvider,
            MapToObjectsSpecProvider<T, T> targetProvider,
            IEqualityComparer<T> comparer)
        {
            _sourceProvider = sourceProvider;
            _targetProvider = targetProvider;
            _comparer = comparer;
        }

        public MergeResult<T> DetectChanges(FindSpecification<T> specification)
        {
            var sourceObjects = new TransactionIsolator(_sourceProvider.Invoke(specification));
            var targetObjects = _targetProvider.Invoke(specification);
            var result = MergeTool.Merge(sourceObjects, targetObjects, _comparer);
            return result;
        }

        private class TransactionIsolator : IEnumerable<T>
        {
            private readonly IEnumerable<T> _queryable;

            public TransactionIsolator(IEnumerable<T> queryable)
            {
                _queryable = queryable;
            }

            public IEnumerator<T> GetEnumerator()
            {
                using (new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    return _queryable.GetEnumerator();
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}