using NuClear.Settings;
using NuClear.Settings.API;

namespace NuClear.CustomerIntelligence.Replication.Host.Settings
{
    public interface ISquirrelSettings : ISettings
    {
        string UpdateServerUrl { get; }
    }

    public class SquirrelSettings : ISquirrelSettings, ISettingsAspect
    {
        private readonly StringSetting _updateServerUrl = ConfigFileSetting.String.Required("UpdateServerUrl");

        public string UpdateServerUrl => _updateServerUrl.Value;
    }
}