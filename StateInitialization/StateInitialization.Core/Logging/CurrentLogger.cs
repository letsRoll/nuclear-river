namespace NuClear.StateInitialization.Core.Logging
{
    internal static class CurrentLogger
    {
        public static ILogger Instance = ConsoleLogger.Instance;
    }
}