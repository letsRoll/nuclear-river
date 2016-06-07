namespace NuClear.River.Hosting.Interactive
{
    public sealed class HostParameters
    {
        public HostParameters(string hostName, string hostDisplayName, string updateServerUrl)
        {
            HostName = hostName;
            HostDisplayName = hostDisplayName;
            UpdateServerUrl = updateServerUrl;
        }

        public string HostName { get; }
        public string HostDisplayName { get; }
        public string UpdateServerUrl { get; }
    }
}