using System;

namespace Celeste.Mod.MacroRoutingTool;

public class MRTModule : EverestModule {
    public static MRTModule Instance { get; private set; }

    public override Type SettingsType => typeof(MRTSettings);
    public static MRTSettings Settings => (MRTSettings) Instance._Settings;

    public override Type SessionType => typeof(MRTSession);
    public static MRTSession Session => (MRTSession) Instance._Session;

    public override Type SaveDataType => typeof(MRTSaveData);
    public static MRTSaveData SaveData => (MRTSaveData) Instance._SaveData;

    public MRTModule() {
        Instance = this;
    }

    public override void Load() {
        UI.DebugMapHooks.EnableAll();
        UI.HeaderScaleData.EnableAllHooks();
        UI.MultiDisplayData.EnableAllHooks();

        UI.DebugMapTweaks.EnableAll();
        UI.GraphViewer.EnableListeners();
    }

    public override void Unload() {
        UI.DebugMapHooks.DisableAll();
        UI.HeaderScaleData.DisableAllHooks();
        UI.MultiDisplayData.DisableAllHooks();

        UI.DebugMapTweaks.DisableAll();
        UI.GraphViewer.DisableListeners();
    }
}