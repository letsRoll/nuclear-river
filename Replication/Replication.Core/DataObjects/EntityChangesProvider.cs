using System.Collections.Generic;

using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;

namespace NuClear.Replication.Core.DataObjects
{
    public class EntityChangesProvider<TDataObject> : IChangesProvider<TDataObject>
        where TDataObject : class
    {
        private readonly IStorageBasedDataObjectAccessor<TDataObject> _storageBasedDataObjectAccessor;
        private readonly MapToObjectsSpecProvider<TDataObject, TDataObject> _mapSpecificationProviderForSource;
        private readonly MapToObjectsSpecProvider<TDataObject, TDataObject> _mapSpecificationProviderForTarget;
        private readonly IEqualityComparerFactory _equalityComparerFactory;

        public EntityChangesProvider(IQuery query, IStorageBasedDataObjectAccessor<TDataObject> storageBasedDataObjectAccessor, IEqualityComparerFactory equalityComparerFactory, IQueryableEnumerator enumerator)
        {
            _storageBasedDataObjectAccessor = storageBasedDataObjectAccessor;
            _equalityComparerFactory = equalityComparerFactory;

            _mapSpecificationProviderForSource = specification => enumerator.Invoke(_storageBasedDataObjectAccessor.GetSource(), specification);
            _mapSpecificationProviderForTarget = specification => enumerator.Invoke(query.For<TDataObject>(), specification);
        }

        public MergeResult<TDataObject> DetectChanges(IReadOnlyCollection<ICommand> commands)
        {
            var identityEqualityComparer = _equalityComparerFactory.CreateIdentityComparer<TDataObject>();
            var completeEqualityComparer = _equalityComparerFactory.CreateCompleteComparer<TDataObject>();
            var dataChangesDetector = new TwoPhaseDataChangesDetector<TDataObject>(
                _mapSpecificationProviderForSource,
                _mapSpecificationProviderForTarget,
                identityEqualityComparer,
                completeEqualityComparer);

            return dataChangesDetector.DetectChanges(_storageBasedDataObjectAccessor.GetFindSpecification(commands));
        }
    }
}