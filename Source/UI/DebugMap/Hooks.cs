using System;
using System.Collections.Generic;
using Celeste.Editor;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class DebugMapHooks {
    public class DebugRoomArgs {
        public LevelTemplate Room;
        public MapEditor DebugMap;
    }
    /// <summary>
    /// Decides whether a given room should be rendered as if the cursor was hovering over it.
    /// </summary>
    public static GetterEventProperty<bool, DebugRoomArgs> DebugRenderRoomAsHovered = new();

    /// <summary>
    /// Decides whether the debug map's room-related controls are enabled.
    /// </summary>
    public static GetterEventProperty<bool> RoomControlsEnabled = new(){Event = (ref bool val) => val = true};
    /// <summary>
    /// Decides whether ctrl+click is to be interpreted as toggling whether each item under the cursor is selected, and if so, causes that to occur.
    /// </summary>
    public static GetterEventProperty<bool> ToggleSelectionPoint = new(){Event = (ref bool val) => val = false};
    /// <summary>
    /// Decides whether a click without holding ctrl is to be interpreted as replacing the selected item list
    /// with the items under the cursor, and if so, causes that to occur.
    /// </summary>
    public static GetterEventProperty<bool> ReplaceSelectionPoint = new(){Event = (ref bool val) => val = false};
    /// <summary>
    /// Decides whether starting to hold left mouse is to be interpreted as starting to resize selected items, and if so, causes that to occur.<br/>
    /// If <see cref="ReplaceSelectionPoint"/> also returns true this frame, that will occur in addition to this. 
    /// </summary>
    public static GetterEventProperty<bool> StartResize = new(){Event = (ref bool val) => val = false};
    /// <summary>
    /// Decides whether starting to hold left mouse is to be interpreted as starting to move selected items, and if so, causes that to occur.<br/>
    /// If <see cref="ReplaceSelectionPoint"/> also returns true this frame, that will occur in addition to this.<br/>
    /// If <see cref="StartResize"/> also returns true this frame, that will occur instead of this.
    /// </summary>
    public static GetterEventProperty<bool> StartMove = new(){Event = (ref bool val) => val = false};
    /// <summary>
    /// Decides whether releasing left click in <see cref="MapEditor.MouseModes.Select"/> is to be interpreted as
    /// altering the list of selected items, and if so, causes that to occur.
    /// </summary>
    public static GetterEventProperty<bool> CommitSelectionRect = new(){Event = (ref bool val) => val = true};
    /// <summary>
    /// Called each frame in <see cref="MapEditor.MouseModes.Select"/>. 
    /// </summary>
    public static Action WhileSelecting = () => {};
    /// <summary>
    /// Called each frame in <see cref="MapEditor.MouseModes.Move"/>. 
    /// </summary>
    public static Action WhileMoving = () => {};
    /// <summary>
    /// Called each frame in <see cref="MapEditor.MouseModes.Resize"/>. 
    /// </summary>
    public static Action WhileResizing = () => {};
    /// <summary>
    /// Called each frame in <see cref="MapEditor.MouseModes.Hover"/> when no other action is being initiated.
    /// </summary>
    public static Action WhileHovering = () => {};

    public static float DirectionalPanBaseMult = 1f;
    /// <summary>
    /// The speed at which the camera moves when directional inputs are held will be multiplied by this value.
    /// </summary>
    public static GetterEventProperty<float> DirectionalPanSpeedMult = new(){Event = (ref float val) => val = DirectionalPanBaseMult};

    public static bool CancelToTPBackBaseEnabled = true;
    /// <summary>
    /// Pressing cancel or escape will only teleport to the room the player came from if this value is true.
    /// </summary>
    public static GetterEventProperty<bool> CancelToTPBackEnabled = new(){Event = (ref bool val) => val = CancelToTPBackBaseEnabled};

    public static bool ConfirmToTPHoverBaseEnabled = true;
    /// <summary>
    /// Pressing confirm will only teleport to the room the cursor is over if this value is true.
    /// </summary>
    public static GetterEventProperty<bool> ConfirmToTPHoverEnabled = new(){Event = (ref bool val) => val = ConfirmToTPHoverBaseEnabled};

    /// <summary>
    /// Called during <see cref="MapEditor.Render"/> to draw in front of the rooms but behind the cursor.
    /// </summary>
    public static Action<MapEditor, Camera> OnRenderBetweenRoomsAndCursor = (debugMap, camera) => {};

    public static void RenderBetweenRoomsAndCursor(MapEditor debugMap, Camera camera) {
        OnRenderBetweenRoomsAndCursor?.Invoke(debugMap, camera);
    }

    public static Action<MapEditor, Camera> RenderBehindHeadbar = (debugMap, camera) => {};

    public static Action<MapEditor> Update = debugMap => {};

    public class LoadEntityDataArgs {
        public LevelTemplate Room;
        public EntityData EntityData;

        public LoadEntityDataArgs() {
            CurrentLoadEntityDataArgs = this;
        }
    }
    public static LoadEntityDataArgs CurrentLoadEntityDataArgs = new();
    public static Action<LoadEntityDataArgs> LoadEntityData = args => {};

    public static void CallLoadEntityData() {
        LoadEntityData?.Invoke(CurrentLoadEntityDataArgs);
    }

    public static Action BeforeMapCtor = () => {};
    public static Action<MapEditor> AfterMapCtor = debugMap => {};

    public static void EnableOn_MapCtor(On.Celeste.Editor.MapEditor.orig_ctor orig, MapEditor self, AreaKey area, bool reloadMapData) {
        BeforeMapCtor?.Invoke();
        orig(self, area, reloadMapData);
        AfterMapCtor?.Invoke(self);
    }

    public static Dictionary<Type, Action<Scene>> OnExit = [];

    public static void EnableOn_SceneEnd(On.Monocle.Scene.orig_End orig, Scene self) {
        orig(self);
        if (OnExit.TryGetValue(self.GetType(), out var function)) {function?.Invoke(self);}
    }

    //TODO Render hook: probably move to an event instead of hardcoding things into the On hook itself
    //(it's tough to decide how to organize this, this is a weird middle ground between hook and hook enabler)

    /// <summary>
    /// The MRT <see cref="GraphViewer"/>'s menus are <see cref="TextMenu"/>s, which are entities.
    /// The debug map is a <see cref="Scene"/>, so it stores a list of <see cref="Scene.Entities">list of entities</see>, but
    /// <see cref="MapEditor.Render"/> doesn't call <see cref="EntityList.Render"/>, so those entities won't be visible. This hook rectifies that.
    /// </summary>
    /// <remarks>
    /// (<see cref="GraphViewer.RenderGraph"/> should not be called here for layering reasons. The graph should be drawn in front of the rooms,
    /// but behind the headbar/menu/manual.)
    /// </remarks>
    public static void OnMapRender(On.Celeste.Editor.MapEditor.orig_Render orig, MapEditor self) {
        orig(self);
        //begin call is copied from MapEditor.Render 2nd batch.
        //note that the matrix argument doesn't involve the camera matrix, since the menus should always stay on the left side of the screen
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Engine.ScreenMatrix);
        self.Entities.Render();
        Draw.SpriteBatch.End();
    }

    /// <summary>
    /// The <see cref="GraphViewer"/> is not an <see cref="Entity"/>, so a hook is needed to call <see cref="GraphViewer.Update"/>
    /// when the debug map <see cref="Scene"/> updates.<br/>
    /// </summary>
    /// <remarks>
    /// (<see cref="MapEditor.Update"/> <i>does</i> call <see cref="EntityList.Update"/>, so this hook doesn't need to do it manually,
    /// unlike <see cref="OnMapRender">the render hook</see>.)
    /// </remarks>
    public static void OnMapUpdate(On.Celeste.Editor.MapEditor.orig_Update orig, MapEditor self) {
        orig(self);
        Update?.Invoke(self);
    }
}