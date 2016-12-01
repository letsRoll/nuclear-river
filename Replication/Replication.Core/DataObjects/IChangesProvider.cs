using System.Collections.Generic;

namespace NuClear.Replication.Core.DataObjects
{
    public interface IChangesProvider<TDataObject>
    {
        MergeResult<TDataObject> DetectChanges(IReadOnlyCollection<ICommand> commands);
    }
}