using System;
using System.Collections.Generic;
using Celeste.Mod.MacroRoutingTool.Data;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    static GraphViewer() {
        DebugMapHooks.DirectionalPanSpeedMult.Event += (ref float val) => val *= InMenu ? 0f : 1f;
        DebugMapHooks.CancelToTPBackEnabled.Event += (ref bool val) => val &= !InMenu;
        DebugMapHooks.ConfirmToTPHoverEnabled.Event += (ref bool val) => val &= !InMenu;

        FocusBindEnabled.Event += (ref bool val) => val = Mode != (int)Modes.Disabled && !Typing;
        ModeBindEnabled.Event += (ref bool val) => val = !Typing;
    }

    public static bool InMenu => CurrentMenu != null && (CurrentMenu.Focused || CurrentMenu.RenderAsFocused);

    public static bool Typing {
        get {
            if (CurrentMenu == null) return false;
            foreach (TextMenu.Item item in CurrentMenu.Items) {
                if (item is ListItem listItem) {
                    foreach (ListItem.SidePart side in new ListItem.SidePart[] {listItem.Left, listItem.Right}) {
                        if (side.Editable && ((TextMenuExt.TextBox)side.Element).Typing) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    /// <summary>
    /// The set of graph viewer modes supported by MacroRoutingTool.
    /// </summary>
    public enum Modes {
        /// <summary>
        /// The graph viewer functionality is disabled, so the debug map has its vanilla behavior.
        /// </summary>
        Disabled,
        /// <summary>
        /// The graph viewer is in Routing mode.
        /// </summary>
        Routing,
        /// <summary>
        /// The graph viewer is in Editing mode.
        /// </summary>
        Editing
    }

    /// <summary>
    /// The viewer mode that the graph viewer is currently in.
    /// </summary>
    public static int Mode {
        get {return MRTModule.SaveData.GraphViewerMode;}
        set {MRTModule.SaveData.GraphViewerMode = value;}
    }

    /// <summary>
    /// The graph being viewed in the graph viewer.
    /// </summary>
    public static Graph Graph;

    public static class SelectionContents {
        public const string Points = nameof(Points);
        public const string Connections = nameof(Connections);
    }

    public static List<Traversable> Selection;
    public static string SelectionHas;

    public static GetterEventProperty<bool> FocusBindEnabled = new();
    public static GetterEventProperty<bool> ModeBindEnabled = new();

    public static void EnableListeners() {
        DebugMapHooks.AfterMapCtor += Load;
    }

    public static void DisableListeners() {
        DebugMapHooks.AfterMapCtor -= Load;
        Unload();
    }

    public static void Load(Scene scene) {
        CreateMenus(scene);
        SwapMenu(ModeInitialMenu);
        DebugMapHooks.OnRenderBetweenRoomsAndCursor += RenderGraph;
        DebugMapHooks.Update += Update;
        DebugMapHooks.RoomControlsEnabled.Event += DisableRoomControlsWhenViewerEnabled;
    }

    public static void Unload() {
        DebugMapHooks.OnRenderBetweenRoomsAndCursor -= RenderGraph;
        DebugMapHooks.Update -= Update;
        DebugMapHooks.RoomControlsEnabled.Event -= DisableRoomControlsWhenViewerEnabled;
    }

    public static void Update(Editor.MapEditor debugMap) {
        if (MRTModule.Settings.Bind_DebugGraphViewerMode.Pressed && ModeBindEnabled.Value) {
            Mode = (Mode + 1) % Enum.GetValues(typeof(Modes)).Length;
            SwapMenu(ModeInitialMenu);
            if (CurrentMenu == null) return;
            CurrentMenu.Current = CurrentMenu.Items[CurrentMenu.FirstPossibleSelection];
        }
        if (MRTModule.Settings.Bind_DebugFocusGraphMenu.Pressed && FocusBindEnabled.Value) {
            CurrentMenu.Focused = !CurrentMenu.Focused;
        }
    }

    public static void DisableRoomControlsWhenViewerEnabled(ref bool val) {val &= Mode == (int)Modes.Disabled;}
}