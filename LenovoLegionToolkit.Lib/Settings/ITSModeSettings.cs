using System.Collections.Generic;

namespace LenovoLegionToolkit.Lib.Settings;

public class ITSModeSettings() : AbstractSettings<ITSModeSettings.ITSModeSettingsStore>("itsmode_settings.json")
{
    public class ITSModeSettingsStore
    {
        public List<ITSMode> FnQModeOrder { get; set; } = [];
        public List<ITSMode> DisabledModes { get; set; } = [];
    }
}
