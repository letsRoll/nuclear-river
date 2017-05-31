using System;
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
            var sourceObjects = new EnumerableDecorator(_sourceProvider.Invoke(specification));
            var targetObjects = _targetProvider.Invoke(specification);
            var result = MergeTool.Merge(sourceObjects, targetObjects, _comparer);
            return result;
        }

        private class EnumerableDecorator : IEnumerable<T>
        {
            private readonly IEnumerable<T> _queryable;

            public EnumerableDecorator(IEnumerable<T> queryable)
            {
                _queryable = queryable;
            }

            public IEnumerator<T> GetEnumerator()
                => new EnumeratorDecorator(_queryable.GetEnumerator());

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            private class EnumeratorDecorator : IEnumerator<T>
            {
                private readonly IEnumerator<T> _enumerator;
                private TransactionScope _transaction;

                public EnumeratorDecorator(IEnumerator<T> enumerator)
                {
                    _enumerator = enumerator;
                }

                public void Dispose()
                {
                    _transaction.Dispose();
                    _enumerator.Dispose();
                }

                public bool MoveNext()
                {
                    if (_transaction == null)
                    {
                        _transaction = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });
                    }

                    return _enumerator.MoveNext();
                }

                public void Reset() => throw new NotSupportedException();

                public T Current => _enumerator.Current;

                object IEnumerator.Current => Current;
            }
        }
    }
}