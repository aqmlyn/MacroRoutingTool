using System;
using Celeste.Editor;
using Celeste.Mod.MacroRoutingTool.Data;

namespace Celeste.Mod.MacroRoutingTool.UI;

/// <summary>
/// Macrorouting Tool's graph viewer.
/// </summary>
public static partial class GraphViewer {
    static GraphViewer() {
        DebugMapHooks.DirectionalPanSpeedMult.Event += (ref float val) => val *= InMenu ? 0f : 1f;
        DebugMapHooks.CancelToTPBackEnabled.Event += (ref bool val) => val &= !InMenu;
        DebugMapHooks.ConfirmToTPHoverEnabled.Event += (ref bool val) => val &= !InMenu;

        FocusBindEnabled.Event += (ref bool val) => val = Mode != (int)Modes.Disabled && !Typing;
        ModeBindEnabled.Event += (ref bool val) => val = !Typing;
    }

    /// <summary>
    /// The debug map in which the graph viewer is currently open.
    /// </summary>
    public static MapEditor DebugMap = null;

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
        get {return MRT.Settings.GraphViewerMode;}
        set {MRT.Settings.GraphViewerMode = value;}
    }

    /// <summary>
    /// The graph being viewed in the graph viewer.
    /// </summary>
    public static Graph Graph;

    /// <summary>
    /// The route being viewed in the graph viewer.
    /// </summary>
    public static Route Route;

    /// <summary>
    /// Pressing <see cref="MRTSettings.Bind_DebugFocusGraphMenu"/> will only toggle focus if this property's getter returns true.
    /// </summary>
    public static GetterEventProperty<bool> FocusBindEnabled = new();
    /// <summary>
    /// Pressing <see cref="MRTSettings.Bind_DebugGraphViewerMode"/> will only cycle <see cref="Mode"/> if this property's getter returns true. 
    /// </summary>
    public static GetterEventProperty<bool> ModeBindEnabled = new();

    public static void Update(MapEditor debugMap) {
        if (MRT.Settings.Bind_DebugGraphViewerMode.Pressed && ModeBindEnabled.Value) {
            Mode = (Mode + 1) % Enum.GetValues(typeof(Modes)).Length;
            SwapMenu(ModeInitialMenu);
            if (CurrentMenu == null) return;
            CurrentMenu.Current = CurrentMenu.Items[CurrentMenu.FirstPossibleSelection];
        }
        if (MRT.Settings.Bind_DebugFocusGraphMenu.Pressed && FocusBindEnabled.Value) {
            CurrentMenu.Focused = !CurrentMenu.Focused;
        }
        if (Mode != (int)Modes.Disabled) {
            foreach (var point in Graph.Points) {
                point.TextElement.State = point.TextureElement.State = Hovers.Contains(point) ? UITextElement.States.Hovered : Selection.Contains(point) ? UITextElement.States.Selected : UITextElement.States.Idle;
            }
        }
    }
}