using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuClear.StateInitialization.Core.Logging
{
    public interface ILogger
    {
        void Append(string message);
    }
}
