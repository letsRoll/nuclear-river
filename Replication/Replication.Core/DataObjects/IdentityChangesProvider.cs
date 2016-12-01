using System.Collections.Generic;

using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;

namespace NuClear.Replication.Core.DataObjects
{
    public class IdentityChangesProvider<TDataObject> : IChangesProvider<TDataObject>
        where TDataObject : class
    {
        private readonly IStorageBasedDataObjectAccessor<TDataObject> _storageBasedDataObjectAccessor;
        private readonly DataChangesDetector<TDataObject> _dataChangesDetector;

        public IdentityChangesProvider(IQuery query,
                                       IStorageBasedDataObjectAccessor<TDataObject> storageBasedDataObjectAccessor,
                                       IEqualityComparerFactory equalityComparerFactory)
            : this(query, storageBasedDataObjectAccessor, equalityComparerFactory, new DefaultQueryableEnumerator())
        {
        }

        public IdentityChangesProvider(IQuery query,
                                       IStorageBasedDataObjectAccessor<TDataObject> storageBasedDataObjectAccessor,
                                       IEqualityComparerFactory equalityComparerFactory,
                                       IQueryableEnumerator enumerator)
        {
            _storageBasedDataObjectAccessor = storageBasedDataObjectAccessor;
            _dataChangesDetector = new DataChangesDetector<TDataObject>(
                specification => enumerator.Invoke(storageBasedDataObjectAccessor.GetSource(), specification),
                specification => enumerator.Invoke(query.For<TDataObject>(), specification),
                equalityComparerFactory.CreateIdentityComparer<TDataObject>());
        }

        public MergeResult<TDataObject> DetectChanges(IReadOnlyCollection<ICommand> commands)
        {
            var specification = _storageBasedDataObjectAccessor.GetFindSpecification(commands);
            return _dataChangesDetector.DetectChanges(specification);
        }
    }
}