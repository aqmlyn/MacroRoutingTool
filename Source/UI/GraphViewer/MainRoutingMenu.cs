namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    public static Menu MainRoutingMenu = new() {
        InitialFor = (int)Modes.Routing,
        Creator = () => {
            TextMenu menu = NewMainMenu();
            return menu;
        }
    };
}