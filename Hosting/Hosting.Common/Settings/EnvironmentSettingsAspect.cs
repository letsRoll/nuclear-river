using NuClear.Settings;
using NuClear.Settings.API;

namespace NuClear.River.Hosting.Common.Settings
{
    public sealed class EnvironmentSettingsAspect : ISettingsAspect, IEnvironmentSettings
    {
        private readonly StringSetting _environmentName = ConfigFileSetting.String.Required("EnvironmentName");
        private readonly StringSetting _hostName = ConfigFileSetting.String.Required("HostName");
        private readonly StringSetting _hostDisplayName;

        public EnvironmentSettingsAspect()
        {
            _hostDisplayName = ConfigFileSetting.String.Optional("HostDisplayName", _hostName.Value);
        }

        public string EnvironmentName => _environmentName.Value;
        public string HostName => _hostName.Value;
        public string HostDisplayName => _hostDisplayName.Value;
    }
}
