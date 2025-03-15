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
    /// The directory containing the YAML files that graphs and routes are imported from and exported to.
    /// </summary>
    public string Path {get; set;} = System.IO.Path.Combine(Monocle.Engine.AssemblyDirectory, "MacroRoutingTool");

    public void CreatePathEntry(TextMenu menu, bool inGame) {
        UI.ListItem item = new(false, true) {
            Container = menu,
            LeftWidthPortion = 0.35f
        };
        item.Left.Value = MRTDialog.PathSetting;
        item.Left.Handler.HandleUsing<string>(new());
        item.Right.Value = Path;
        item.Right.Handler.HandleUsing<string>(new() {
            ValueParser = value => Path = value
        });
        menu.Add(item);
    }
}