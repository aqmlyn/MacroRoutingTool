using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Celeste.Editor;
using Celeste.Mod.MacroRoutingTool.Data;
using Monocle;

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
    /// Whether the <see cref="CurrentMenu"/> is currently focused. 
    /// </summary>
    public static bool InMenu => CurrentMenu != null && (CurrentMenu.Focused || CurrentMenu.RenderAsFocused);

    /// <summary>
    /// Whether the user is currently typing in a <see cref="TextMenuExt.TextBox"/> in the <see cref="CurrentMenu"/>.  
    /// </summary>
    public static bool Typing {
        get {
            if (CurrentMenu == null) return false;
            foreach (TextMenu.Item item in CurrentMenu.Items) {
                if (item is ListItem listItem) {
                    foreach (ListItem.Part side in new ListItem.Part[] {listItem.Left, listItem.Right}) {
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
    /// Contains references to each item type the <see cref="Selection"/> could contain. 
    /// </summary>
    public static class SelectionContents {
        /// <summary>
        /// The <see cref="Selection"/> currently contains <see cref="Point"/>s.  
        /// </summary>
        public const string Points = nameof(Points);
        /// <summary>
        /// The <see cref="Selection"/> currently contains <see cref="Connection"/>s. 
        /// </summary>
        public const string Connections = nameof(Connections);
    }

    /// <summary>
    /// List of items currently selected in the graph viewer.
    /// </summary>
    public static List<Traversable> Selection = [];
    /// <summary>
    /// The type of item currently in the <see cref="Selection"/>. 
    /// </summary>
    public static string SelectionHas;

    /// <summary>
    /// Pressing <see cref="MRTSettings.Bind_DebugFocusGraphMenu"/> will only toggle focus if this property's getter returns true.
    /// </summary>
    public static GetterEventProperty<bool> FocusBindEnabled = new();
    /// <summary>
    /// Pressing <see cref="MRTSettings.Bind_DebugGraphViewerMode"/> will only cycle <see cref="Mode"/> if this property's getter returns true. 
    /// </summary>
    public static GetterEventProperty<bool> ModeBindEnabled = new();

    public static void EnableListeners() {
        DebugMapHooks.AfterMapCtor += Load;
        DebugMapHooks.OnRenderBetweenRoomsAndCursor += RenderGraph;
        DebugMapHooks.Update += Update;
        EnableInputListeners();
        var onExitDebugMap = DebugMapHooks.OnExit.EnsureGet(typeof(MapEditor));
        onExitDebugMap += Unload;
    }

    public static void DisableListeners() {
        DebugMapHooks.AfterMapCtor -= Load;
        DebugMapHooks.OnRenderBetweenRoomsAndCursor -= RenderGraph;
        DebugMapHooks.Update -= Update;
        DisableInputListeners();
        var onExitDebugMap = DebugMapHooks.OnExit.EnsureGet(typeof(MapEditor));
        onExitDebugMap -= Unload;
    }

    /// <summary>
    /// Whether this is the first time the graph viewer has been loaded this session.
    /// </summary>
    public static bool FirstLoad = true;

    /// <summary>
    /// Contains delegates used by <see cref="OnFirstLoad"/> to determine which item should be selected for viewing
    /// the first time the graph viewer is opened this session.
    /// </summary>
    public class FirstLoadHandler {
        /// <summary>
        /// Called to determine whether this item was the item most recently viewed in the previous session.
        /// </summary>
        public Func<object, bool> CheckID;
        /// <summary>
        /// Called to determine whether this item should be able to be viewed in the current graph viewer state.
        /// </summary>
        public Func<object, bool> IsSuitable;
        /// <summary>
        /// Called to create a new item if no suitable item was found.
        /// </summary>
        public Func<object> MakeNew;
        /// <summary>
        /// Called to configure the item created by <see cref="MakeNew"/>. 
        /// </summary>
        public Action<object> ConfigureNew;
        /// <summary>
        /// Gets the <see cref="MRTExport{T}.List"/> of the class passed as the type parameter to <see cref="Bind"/>.
        /// </summary>
        public Func<List<MRTExport>> GetList;
        /// <summary>
        /// Sets the <see cref="FirstLoadHandler{T}.Field"/> of the handler passed to <see cref="Bind"/>. 
        /// </summary>
        public Action<object> SetField;
        /// <summary>
        /// Creates a new <see cref="FirstLoadHandler"/> whose delegates will call the given <see cref="FirstLoadHandler{T}"/>'s delegates.
        /// </summary>
        /// <typeparam name="T">Item type that the given handler handles.</typeparam>
        /// <param name="typedHandler">A handler whose delegates can expect <typeparamref name="T"/> values.</param>
        /// <returns>A handler that can be used without knowing <typeparamref name="T"/>.</returns>
        public static FirstLoadHandler Bind<T>(FirstLoadHandler<T> typedHandler) where T : MRTExport<T>, new() {
            return new(){
                CheckID = val => typedHandler.CheckID?.Invoke((T)val) ?? false,
                IsSuitable = val => typedHandler.IsSuitable?.Invoke((T)val) ?? false,
                ConfigureNew = val => typedHandler.ConfigureNew?.Invoke((T)val),
                GetList = () => typedHandler.List.ConvertAll(val => (MRTExport)val),
                MakeNew = typedHandler.MakeNew,
                SetField = val => typedHandler.Field.SetValue(null, val)
            };
        }
    }

    public class FirstLoadHandler<T> where T : MRTExport<T>, new() {
        /// <summary>
        /// The field that <see cref="FirstLoadHandler.SetField"/> sets.
        /// </summary>
        public FieldInfo Field;
        /// <summary>
        /// <inheritdoc cref="FirstLoadHandler.CheckID"/>
        /// </summary>
        public Func<T, bool> CheckID;
        /// <summary>
        /// <inheritdoc cref="FirstLoadHandler.IsSuitable"/> 
        /// </summary>
        public Func<T, bool> IsSuitable;
        /// <summary>
        /// <inheritdoc cref="FirstLoadHandler.ConfigureNew"/> 
        /// </summary>
        public Action<T> ConfigureNew;
        /// <summary>
        /// Gets <typeparamref name="T"/>.List.
        /// </summary>
        public List<T> List => (List<T>)typeof(T)
            .BaseType
            .GetField(nameof(MRTExport<T>.List), BindingFlags.Public | BindingFlags.Static)
            .GetValue(null);
        /// <summary>
        /// <inheritdoc cref="FirstLoadHandler.MakeNew"/> 
        /// </summary>
        public T MakeNew() {
            Guid id = Guid.NewGuid();
            T obj = new(){
                ID = id,
                Name = MRTDialog.GraphDefaultName,
                Path = id.ToString()
            };
            ConfigureNew?.Invoke(obj);
            return obj;
            //do not export the new item or add it to its class's list here.
            //that should wait until an autosave with pending changes or an unsaved changes prompt
        }
    }

    /// <summary>
    /// The <see cref="FirstLoadHandler"/> assigned to each class of which instances can be viewed in the graph viewer.
    /// </summary>
    public static Dictionary<string, FirstLoadHandler> FirstLoadHandlers = new(){
        {nameof(Data.Graph), FirstLoadHandler.Bind<Graph>(new(){
            //TODO AltSidesHelper support
            Field = typeof(GraphViewer).GetField(nameof(Graph), BindingFlags.Public | BindingFlags.Static),
            CheckID = graph => Utils.MapSide.TryParse(UIHelpers.GetAreaKey(DebugMap), out var sideInfo)
                && MRT.Settings.LastGraphID.TryGetValue(sideInfo, out Guid id)
                && graph.ID == id,
            IsSuitable = graph => {
                AreaKey mapInfo = UIHelpers.GetAreaKey(DebugMap);
                return graph.Area.SID == mapInfo.SID && graph.Side == (int)mapInfo.Mode;
            },
            ConfigureNew = graph => {
                AreaKey mapInfo = UIHelpers.GetAreaKey(DebugMap);
                graph.Area = AreaData.Get(mapInfo);
                graph.Side = (int)mapInfo.Mode;
            }
        })},
        {nameof(Data.Route), FirstLoadHandler.Bind<Route>(new(){
            Field = typeof(GraphViewer).GetField(nameof(Route), BindingFlags.Public | BindingFlags.Static),
            CheckID = route => MRT.Settings.LastRouteID.TryGetValue(Graph.ID, out Guid id) && route.ID == id,
            IsSuitable = route => route.GraphID == Graph.ID,
            ConfigureNew = route => {
                route.GraphID = Graph.ID;
            }
        })}
    };

    /// <summary>
    /// Called by <see cref="Load"/> if <see cref="FirstLoad"/> is true.  
    /// </summary>
    public static Action OnFirstLoad = () => {
        IO.Initialize();
        if (IO.Working) {
            if (IO.TryLoadAll()) {
                foreach (var handler in FirstLoadHandlers.Values) {
                    //for each item type viewable in the graph viewer, select an item to have initially open as follows:
                    //1. if the user had an item open in the previous session and an item with that ID still exists, open that.
                    //2. if any currently loaded items should be viewable given the current graph state (checked by IsSuitable), open one of those.
                    //3. if all else fails, just create a new item.
                    var idMatches = handler.GetList().Where(obj => handler.CheckID?.Invoke(obj) ?? false);
                    if (idMatches.Any()) {
                        if (idMatches.Count() > 1) {
                            //TODO prompt to resolve duplicate ID
                        }
                        handler.SetField?.Invoke(idMatches.First());
                    } else {
                        var suitable = handler.GetList()
                            .Where(obj => handler.IsSuitable?.Invoke(obj) ?? false)
                            .MaxBy(obj => {
                                //if there are multiple suitable matches, choose the one whose file was most recently modified
                                try {return File.GetLastWriteTime(obj.Path);}
                                catch {return new DateTime(0);}
                            });
                        handler.SetField?.Invoke(suitable ?? handler.MakeNew?.Invoke());
                    }
                }
            }
        } else {
            Engine.Commands.Open = true;
            Engine.Commands.Log(MRTDialog.IOCantSave, IO.WarnColor);
        }

        //create the menus after selecting a graph and route
        CreateMenus();
    };

    /// <summary>
    /// Enables hooks necessary for graph viewer functionality when the debug map is opened.
    /// </summary>
    public static void Load(MapEditor debugMap) {
        DebugMap = debugMap;

        if (FirstLoad) {
            FirstLoad = false;
            OnFirstLoad?.Invoke();
        }
        SwapMenu(ModeInitialMenu);
    }

    /// <summary>
    /// Disables the graph viewer-related hooks that were enabled by <see cref="Load"/>. 
    /// </summary>
    public static void Unload(Scene _) {
        
    }

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
    }
}