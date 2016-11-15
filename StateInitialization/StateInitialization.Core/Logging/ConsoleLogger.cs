using System;

namespace NuClear.StateInitialization.Core.Logging
{
    internal sealed class ConsoleLogger : ILogger
    {
        public static readonly ILogger Instance = new ConsoleLogger();

        public void Append(string message)
            => Console.WriteLine($"[{DateTime.Now}] [{Environment.CurrentManagedThreadId}] {message}");
    }
}