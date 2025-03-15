namespace Celeste.Mod.MacroRoutingTool;

public class MRTSaveData : EverestModuleSaveData {
    [SettingIgnore]
    public int GraphViewerMode {get; set;} = (int)UI.GraphViewer.Modes.Disabled;
}