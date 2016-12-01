using System.Collections.Generic;

using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;

namespace NuClear.Replication.Core.DataObjects
{
    public class IdentityChangesProvider<TDataObject> : IChangesProvider<TDataObject>
        where TDataObject : class
    {
        private readonly IStorageBasedDataObjectAccessor<TDataObject> _storageBasedDataObjectAccessor;
        private readonly MapToObjectsSpecProvider<TDataObject, TDataObject> _mapSpecificationProviderForSource;
        private readonly MapToObjectsSpecProvider<TDataObject, TDataObject> _mapSpecificationProviderForTarget;
        private readonly IEqualityComparerFactory _equalityComparerFactory;

        public IdentityChangesProvider(IQuery query, IStorageBasedDataObjectAccessor<TDataObject> storageBasedDataObjectAccessor, IEqualityComparerFactory equalityComparerFactory)
        {
            _storageBasedDataObjectAccessor = storageBasedDataObjectAccessor;
            _equalityComparerFactory = equalityComparerFactory;

            _mapSpecificationProviderForSource = specification => _storageBasedDataObjectAccessor.GetSource().Where(specification);
            _mapSpecificationProviderForTarget = specification => query.For<TDataObject>().Where(specification);
        }

        public MergeResult<TDataObject> DetectChanges(IReadOnlyCollection<ICommand> commands)
        {
            var equalityComparer = _equalityComparerFactory.CreateIdentityComparer<TDataObject>();
            var dataChangesDetector = new DataChangesDetector<TDataObject>(
                _mapSpecificationProviderForSource,
                _mapSpecificationProviderForTarget,
                equalityComparer);

            return dataChangesDetector.DetectChanges(_storageBasedDataObjectAccessor.GetFindSpecification(commands));
        }
    }
}