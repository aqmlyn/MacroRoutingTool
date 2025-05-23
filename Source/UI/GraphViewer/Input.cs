namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    public static void EnableInputListeners() {
        DebugMapHooks.RoomControlsEnabled.Event += DisableRoomControlsWhenViewerEnabled;
        DebugMapTweaks.MetadataBindEnabled.Event += DisableMetadataWhenViewerMenuFocused;
        DebugMapHooks.WhileHovering += UpdateHover;
        DebugMapHooks.WhileSelecting += UpdateHoverRect;
        DebugMapHooks.CommitSelectionRect.Event += ReleaseRect;
        DebugMapHooks.WhileMoving += WhileMovingAny;
        DebugMapHooks.StartMove.Event += DragCheckStartMove;
        DebugMapHooks.ReplaceSelectionPoint.Event += StartReplaceDrag;
        DebugMapHooks.ToggleSelectionPoint.Event += StartToggleDrag;
    }

    public static void DisableInputListeners() {
        DebugMapHooks.RoomControlsEnabled.Event -= DisableRoomControlsWhenViewerEnabled;
        DebugMapTweaks.MetadataBindEnabled.Event -= DisableMetadataWhenViewerMenuFocused;
        DebugMapHooks.WhileHovering -= UpdateHover;
        DebugMapHooks.WhileSelecting -= UpdateHoverRect;
        DebugMapHooks.CommitSelectionRect.Event -= ReleaseRect;
        DebugMapHooks.WhileMoving -= WhileMovingAny;
        DebugMapHooks.StartMove.Event -= DragCheckStartMove;
        DebugMapHooks.ReplaceSelectionPoint.Event -= StartReplaceDrag;
        DebugMapHooks.ToggleSelectionPoint.Event -= StartToggleDrag;
    }

    public static void DisableRoomControlsWhenViewerEnabled(ref bool val) {val &= Mode == (int)Modes.Disabled;}
    public static void DisableMetadataWhenViewerMenuFocused(ref bool val) {val &= CurrentMenu == null || !(CurrentMenu.Focused || CurrentMenu.RenderAsFocused);}
}