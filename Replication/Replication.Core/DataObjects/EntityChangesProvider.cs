using System.Collections.Generic;

using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;

namespace NuClear.Replication.Core.DataObjects
{
    public class EntityChangesProvider<TDataObject> : IChangesProvider<TDataObject>
        where TDataObject : class
    {
        private readonly IStorageBasedDataObjectAccessor<TDataObject> _storageBasedDataObjectAccessor;
        private readonly TwoPhaseDataChangesDetector<TDataObject> _dataChangesDetector;

        public EntityChangesProvider(IQuery query,
                                     IStorageBasedDataObjectAccessor<TDataObject> storageBasedDataObjectAccessor,
                                     IEqualityComparerFactory equalityComparerFactory)
            : this(query, storageBasedDataObjectAccessor, equalityComparerFactory, new DefaultQueryableEnumerator())
        {
        }

        public EntityChangesProvider(IQuery query,
                                     IStorageBasedDataObjectAccessor<TDataObject> storageBasedDataObjectAccessor,
                                     IEqualityComparerFactory equalityComparerFactory,
                                     IQueryableEnumerator enumerator)
        {
            _storageBasedDataObjectAccessor = storageBasedDataObjectAccessor;
            _dataChangesDetector = new TwoPhaseDataChangesDetector<TDataObject>(
                specification => enumerator.Invoke(_storageBasedDataObjectAccessor.GetSource(), specification),
                specification => enumerator.Invoke(query.For<TDataObject>(), specification),
                equalityComparerFactory.CreateIdentityComparer<TDataObject>(),
                equalityComparerFactory.CreateCompleteComparer<TDataObject>());
        }

        public MergeResult<TDataObject> DetectChanges(IReadOnlyCollection<ICommand> commands)
        {
            var specification = _storageBasedDataObjectAccessor.GetFindSpecification(commands);
            return _dataChangesDetector.DetectChanges(specification);
        }
    }
}