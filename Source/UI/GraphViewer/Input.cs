namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    public static void EnableInputListeners() {
        DebugMapHooks.RoomControlsEnabled.Event += DisableRoomControlsWhenViewerEnabled;
        DebugMapTweaks.MetadataBindEnabled.Event += DisableMetadataWhenViewerMenuFocused;
    }

    public static void DisableInputListeners() {
        DebugMapHooks.RoomControlsEnabled.Event -= DisableRoomControlsWhenViewerEnabled;
        DebugMapTweaks.MetadataBindEnabled.Event -= DisableMetadataWhenViewerMenuFocused;
    }

    public static void DisableRoomControlsWhenViewerEnabled(ref bool val) {val &= Mode == (int)Modes.Disabled;}
    public static void DisableMetadataWhenViewerMenuFocused(ref bool val) {val &= CurrentMenu == null || !(CurrentMenu.Focused || CurrentMenu.RenderAsFocused);}
}