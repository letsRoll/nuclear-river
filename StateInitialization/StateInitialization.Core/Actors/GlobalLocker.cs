namespace NuClear.StateInitialization.Core.Actors
{
    internal static class GlobalLocker
    {
        public static readonly object Instance = new object();
    }
}