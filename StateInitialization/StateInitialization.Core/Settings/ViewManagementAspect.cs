using NuClear.Settings;
using NuClear.Settings.API;

namespace NuClear.StateInitialization.Core.Settings
{
    public interface IDbSchemaManagementSettings : ISettings
    {
        bool DisableViews { get; }
        bool DisableConstraints { get; }
    }

    public class DbSchemaManagementAspect : ISettingsAspect, IDbSchemaManagementSettings
    {
        private readonly BoolSetting _disableViews = ConfigFileSetting.Bool.Optional("DisableViews", true);
        private readonly BoolSetting _disableConstraints = ConfigFileSetting.Bool.Optional("DisableConstraints", true);

        public bool DisableViews => _disableViews.Value;
        public bool DisableConstraints => _disableConstraints.Value;
    }
}