using System.IO;

namespace Celeste.Mod.MacroRoutingTool;

public class MRTSaveData : EverestModuleSaveData {
    /// <summary>
    /// <inheritdoc cref="UI.GraphViewer.Mode"/>
    /// </summary>
    [SettingIgnore]
    public int GraphViewerMode {get; set;} = (int)UI.GraphViewer.Modes.Disabled;

    /// <summary>
    /// Path to the YAML file containing the graph being worked on, relative to <see cref="MRTSettings.MRTDirectory"/>. 
    /// </summary>
    [SettingIgnore]
    public string GraphPath {get; set;} = "";

    /// <summary>
    /// Absolute path to the YAML file containing the graph being worked on.
    /// </summary>
    [SettingIgnore]
    [YamlDotNet.Serialization.YamlIgnore]
    public string GraphPathAbsolute => Path.Join(MRTModule.Settings.MRTDirectory, GraphPath);

    /// <summary>
    /// Path to the YAML file containing the route being worked on, relative to <see cref="MRTSettings.MRTDirectory"/>. 
    /// </summary>
    [SettingIgnore]
    public string RoutePath {get; set;} = "";

    /// <summary>
    /// Absolute path to the YAML file containing the route being worked on.
    /// </summary>
    [SettingIgnore]
    [YamlDotNet.Serialization.YamlIgnore]
    public string RoutePathAbsolute => Path.Join(MRTModule.Settings.MRTDirectory, RoutePath);
}