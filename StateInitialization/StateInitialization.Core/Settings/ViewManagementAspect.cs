using NuClear.Settings;
using NuClear.Settings.API;

namespace NuClear.StateInitialization.Core.Settings
{
    public interface IViewManagementSettings : ISettings
    {
        bool TemporaryDropViews { get; }
    }

    public class ViewManagementAspect : ISettingsAspect, IViewManagementSettings
    {
        private readonly BoolSetting _temporaryDropViews = ConfigFileSetting.Bool.Optional("TemporaryDropViews", true);

        public bool TemporaryDropViews => _temporaryDropViews.Value;
    }
}