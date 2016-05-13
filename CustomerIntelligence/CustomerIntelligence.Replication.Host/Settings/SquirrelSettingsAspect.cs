using NuClear.Settings;
using NuClear.Settings.API;

namespace NuClear.CustomerIntelligence.Replication.Host.Settings
{
    public interface ISquirrelSettings : ISettings
    {
        string ApplicationReleasesPath { get; }
    }

    public class SquirrelSettings : ISquirrelSettings, ISettingsAspect
    {
        private readonly StringSetting _applicationInstallationPath = ConfigFileSetting.String.Required("ApplicationReleasesPath");

        public string ApplicationReleasesPath => _applicationInstallationPath.Value;
    }
}