namespace NuClear.StateInitialization.Core.Logging
{
    public sealed class SilentLogger : ILogger
    {
        public static readonly ILogger Instance = new SilentLogger();

        public void Append(string message)
        {
        }
    }
}