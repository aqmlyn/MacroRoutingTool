namespace Celeste.Mod.MacroRoutingTool;

public class MRTSettings : EverestModuleSettings {
    /// <summary>
    /// When the debug map is open, this bind toggles whether the graph menu is focused.
    /// </summary>
    [DefaultButtonBinding(0, Microsoft.Xna.Framework.Input.Keys.Tab)]
    public ButtonBinding Bind_DebugFocusGraphMenu {get; set;}

    /// <summary>
    /// When the debug map is open, this bind cycles through the graph views.
    /// </summary>
    [DefaultButtonBinding(0, Microsoft.Xna.Framework.Input.Keys.M)]
    public ButtonBinding Bind_DebugGraphViewerMode {get; set;}

    /// <summary>
    /// When the debug map is open, holding this bind shows metadata over some entities.
    /// </summary>
    [DefaultButtonBinding(0, Microsoft.Xna.Framework.Input.Keys.Q)]
    public ButtonBinding Bind_DebugEntityMetadata {get; set;}

    /// <summary>
    /// User-facing text representing the directory containing the YAML files that graphs and routes are imported from and exported to.
    /// </summary>
    public string MRTDirectory {get; set;} = System.IO.Path.Combine("%CELESTE%", "MacroRoutingTool");

    /// <summary>
    /// Absolute path of the directory containing the YAML files that graphs and routes are imported from and exported to.
    /// </summary>
    public string MRTDirectoryAbsolute => MRTDirectory.Replace("%CELESTE%", Monocle.Engine.AssemblyDirectory, System.StringComparison.OrdinalIgnoreCase);

    public void CreateMRTDirectoryEntry(TextMenu menu, bool inGame) {
        UI.ListItem item = new(false, true) {
            Container = menu,
            LeftWidthPortion = 0.35f
        };
        item.Left.Value = MRTDialog.PathSetting;
        item.Left.Handler.HandleUsing<string>(new());
        item.Right.Value = MRTDirectory;
        item.Right.Handler.HandleUsing<string>(new() {
            ValueParser = value => MRTDirectory = value
        });
        menu.Add(item);
    }
}