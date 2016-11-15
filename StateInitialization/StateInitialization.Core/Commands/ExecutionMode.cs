using System.Threading.Tasks;

namespace NuClear.StateInitialization.Core.Commands
{
    public class ExecutionMode
    {
        private const int DefaultDegreeOfParallelizm = -1;
        public static ExecutionMode Sequential = new ExecutionMode(1);
        public static ExecutionMode Parallel = new ExecutionMode(DefaultDegreeOfParallelizm);

        private readonly int _maxDegreeOfParallelizm;

        public ExecutionMode(int maxDegreeOfParallelizm)
        {
            _maxDegreeOfParallelizm = maxDegreeOfParallelizm;
        }

        public virtual ParallelOptions ParallelOptions
            => _maxDegreeOfParallelizm == DefaultDegreeOfParallelizm
                   ? new ParallelOptions()
                   : new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelizm };
    }
}