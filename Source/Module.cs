using System;

namespace Celeste.Mod.MacroRoutingTool;

/// <summary>
/// The <see cref="EverestModule"/> for Macrorouting Tool.
/// </summary>
public class MRT : EverestModule {
    /// <inheritdoc cref="MRT"/> 
    public static MRT Instance { get; private set; }

    public override Type SettingsType => typeof(MRTSettings);
    /// <inheritdoc cref="EverestModuleSettings"/> 
    public static MRTSettings Settings => (MRTSettings) Instance._Settings;

    public override Type SessionType => typeof(MRTSession);
    /// <inheritdoc cref="EverestModuleSession"/> 
    public static MRTSession Session => (MRTSession) Instance._Session;

    public override Type SaveDataType => typeof(MRTSaveData);
    /// <inheritdoc cref="EverestModuleSaveData"/>
    public static MRTSaveData SaveData => (MRTSaveData) Instance._SaveData;

    public MRT() {
        Instance = this;
    }

    public override void Load() {
        UI.UIHelperHooks.EnableAll();
        UI.DebugMapHooks.EnableAll();
        UI.HeaderScaleData.EnableAllHooks();
        UI.MultiDisplayData.EnableAllHooks();

        UI.DebugMapTweaks.EnableAll();
        UI.GraphViewer.EnableListeners();
    }

    public override void Unload() {
        UI.UIHelperHooks.EnableAll();
        UI.DebugMapHooks.DisableAll();
        UI.HeaderScaleData.DisableAllHooks();
        UI.MultiDisplayData.DisableAllHooks();

        UI.DebugMapTweaks.DisableAll();
        UI.GraphViewer.DisableListeners();
    }

    public static string LogTag(params string[] subtags) => string.Join("/", "MacroroutingTool", subtags);
    public class LogTags {
        public const string Debug = "Debug";
        public const string Utils = "Utils";
    }
}