using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste.Editor;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

/// <summary>
/// Miscellaneous tweaks to the debug map that aren't related to displaying MRT's graph viewer.
/// </summary>
public static class DebugMapTweaks {
    public static void EnableAll() {
        DebugMapHooks.DebugRenderRoomAsHovered.Event += RenderCurrentAsHovered;
        DebugMapHooks.OnRenderBetweenRoomsAndCursor += RenderEntities; //order of
        DebugMapHooks.OnRenderBetweenRoomsAndCursor += DrawHoverText;  //these is
        DebugMapHooks.OnRenderBetweenRoomsAndCursor += DrawCursorBack; //important
        DebugMapHooks.LoadEntityData += PopulateEntitiesToRender;
        DebugMapHooks.Update += HoverGetRoomInfo;
        DebugMapHooks.AfterMapCtor += LoadWhiteRect;
        DebugMapHooks.BeforeMapCtor += ResetEntitiesToRender;
    }

    public static void DisableAll() {
        DebugMapHooks.DebugRenderRoomAsHovered.Event -= RenderCurrentAsHovered;
        DebugMapHooks.OnRenderBetweenRoomsAndCursor -= RenderEntities;
        DebugMapHooks.OnRenderBetweenRoomsAndCursor -= DrawHoverText;
        DebugMapHooks.OnRenderBetweenRoomsAndCursor -= DrawCursorBack;
        DebugMapHooks.LoadEntityData -= PopulateEntitiesToRender;
        DebugMapHooks.Update -= HoverGetRoomInfo;
        DebugMapHooks.AfterMapCtor -= LoadWhiteRect;
        DebugMapHooks.BeforeMapCtor -= ResetEntitiesToRender;
    }

    public static FieldInfo CurrentSession => typeof(MapEditor).GetField("CurrentSession", BindingFlags.NonPublic | BindingFlags.Instance);
    public static FieldInfo MousePos => typeof(MapEditor).GetField("mousePosition", BindingFlags.NonPublic | BindingFlags.Instance);
    public static FieldInfo Rooms => typeof(MapEditor).GetField("levels", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void RenderCurrentAsHovered(ref bool renderAsHovered, DebugMapHooks.DebugRoomArgs args) {
        object sessionObj = CurrentSession.GetValue(args.DebugMap);
        if (sessionObj != null) {
            renderAsHovered |= args.Room.Name == ((Session)sessionObj).Level;
        }
    }

    public static void DrawCursorBack(MapEditor debugMap, Camera camera) {
        if (debugMap.mouseMode == MapEditor.MouseModes.Hover) {
            float radius = (CursorRadius + 1f) / camera.Zoom;
            float thickness = (CursorThickness + 2f) / camera.Zoom;
            Draw.Line(
                debugMap.mousePosition.X - radius, debugMap.mousePosition.Y,
                debugMap.mousePosition.X + radius, debugMap.mousePosition.Y,
                Color.Black,
                thickness
            );
            Draw.Line(
                debugMap.mousePosition.X, debugMap.mousePosition.Y - radius,
                debugMap.mousePosition.X, debugMap.mousePosition.Y + radius,
                Color.Black,
                thickness
            );
        }
    }

  #region hover text system
    public static class HoverIDs {
        public const string RoomName = "RoomName";
        public const string CheckpointName = "CheckpointName";
    }
    public static float HoverTextScale = 0.7f;
    public const float CursorRadius = 12f;
    public const float CursorThickness = 3f;
    public static float HoverTextHSpacing = 12f;
    public static float HoverTextVSpacing = 2f;
    public static float HoverTextBorderThickness = 2f;
    public static float HoverBackOpacity = 0.6f;
    
    public class HoverTextItem {
        public UIHelpers.TextElement Element = new();
        public GetterEventProperty<string> Text = new();
    }

    public static GetterEventProperty<bool> HoverTextEnabled = new(){Value = true};

    /// <summary>
    /// Needed to draw a rectangle with non-integer position and dimensions.
    /// Using <see cref="Draw.Rect"/> results in those values being cast to integers,
    /// causing visual discrepancies that become very noticeable as the camera zooms in.
    /// </summary>
    public static MTexture WhiteRect = null;
    public static void LoadWhiteRect(MapEditor debugMap) {
        WhiteRect = GFX.Game["decals/generic/snow_o"]; //this texture carries in map BGs too, if there are no decals/generic/snow_o fans then i am dead
    }

    public static void DrawHoverText(MapEditor debugMap, Camera camera) {
        List<UIHelpers.TextElement> visibleTexts = [];
        foreach (HoverTextItem item in HoverText.Values) {
            item.Element.Text = item.Text.Value;
            if (item.Element.Text != "") {visibleTexts.Add(item.Element);}
        }
        int visibleCount = visibleTexts.Count;
        if (visibleCount == 0) {return;}
        Vector2 mousePos = (Vector2)MousePos.GetValue(debugMap);
        Vector2 drawPos = new(
            mousePos.X + (CursorRadius + HoverTextHSpacing * 2) / camera.Zoom,
            mousePos.Y - ((visibleCount % 2 == 0 ? HoverTextVSpacing / 2 : ActiveFont.LineHeight * HoverTextScale / 2) + ActiveFont.LineHeight * HoverTextScale * (visibleCount / 2) + HoverTextVSpacing * ((visibleCount - 1) / 2)) / camera.Zoom
        );
        WhiteRect.Draw(
            new Vector2(drawPos.X - HoverTextHSpacing / camera.Zoom, drawPos.Y - HoverTextHSpacing / camera.Zoom),
            Vector2.Zero,
            Color.Black * HoverBackOpacity,
            new Vector2(
                (visibleTexts.Max(elem => ActiveFont.Measure(elem.Text).X) * HoverTextScale + HoverTextHSpacing * 2) / camera.Zoom / WhiteRect.Width,
                (ActiveFont.LineHeight * HoverTextScale * visibleCount + HoverTextVSpacing * visibleCount + HoverTextHSpacing * 2) / camera.Zoom / WhiteRect.Width
            )
        );
        foreach (UIHelpers.TextElement elem in visibleTexts) {
            elem.Position = drawPos;
            elem.Scale = Vector2.One * HoverTextScale / camera.Zoom;
            elem.BorderThickness = HoverTextBorderThickness / camera.Zoom;
            elem.Render();
            drawPos.Y += (ActiveFont.LineHeight * HoverTextScale + HoverTextVSpacing) / camera.Zoom;
        }
    }
  #endregion

  #region hover text items
    public static Dictionary<string, HoverTextItem> HoverText = new(){
        {HoverIDs.RoomName, new(){
            Element = new(){Color = Color.Red}
        }},
        {HoverIDs.CheckpointName, new() {
            Element = new(){Color = Color.Lime}
        }}
    };

    public static void HoverGetRoomInfo(MapEditor debugMap) {
        string hoveredName = "";
        string hoveredCheckpoint = "";
        ModeProperties map = UIHelpers.GetAreaData(debugMap).Mode[(int)UIHelpers.GetAreaKey(debugMap).Mode];
        CheckpointData[] checkpoints = map.Checkpoints;
        string firstRoom = map.MapData.StartLevel().Name;
        foreach (LevelTemplate room in (List<LevelTemplate>)Rooms.GetValue(debugMap)) {
            if (room.Check((Vector2)MousePos.GetValue(debugMap))) {
                //if cursor is hovering over any room, show that room's name
                hoveredName = room.Name;
                //if hovered room is first room, show start "checkpoint" name (TODO i think some helpers let u rename it, want to support that later)
                if (firstRoom == hoveredName) {
                    hoveredCheckpoint = MRTDialog.Get("overworld_start");
                    HoverText[HoverIDs.CheckpointName].Element.Color = Color.Aqua;
                }
                //if hovered room is a checkpoint, show that checkpoint's name
                CheckpointData checkpoint = checkpoints.FirstOrDefault(cp => cp.Level == hoveredName, null);
                if (checkpoint != null) {
                    hoveredCheckpoint = MRTDialog.Get(checkpoint.Name);
                    HoverText[HoverIDs.CheckpointName].Element.Color = Color.Lime;
                }
                break;
            }
        }
        HoverText[HoverIDs.RoomName].Text.Value = hoveredName;
        HoverText[HoverIDs.CheckpointName].Text.Value = hoveredCheckpoint;
    }
  #endregion

  #region metadata system
    public class DebugEntityData {
        public string Room;
        public Dictionary<string, object> DebugData;
        public EntityData MapData;
    }

    public class EntityTypeHandler {
        public Func<LevelTemplate, DebugEntityData, Color> ColorGetter;
        public Action<LevelTemplate, EntityData> Reader;
        public Action<LevelTemplate, DebugEntityData> Renderer;
        public Func<LevelTemplate, DebugEntityData, string> DataParser;
        public Action DataInitializer;
    }

    public static Dictionary<string, Dictionary<string, List<DebugEntityData>>> EntitiesToRender = [];

    public static Dictionary<string, ulong> EntityCounts = [];
    public static Action InitCount(string type) => () => EntityCounts.EnsureSet(type, 1u);
    public static Func<LevelTemplate, DebugEntityData, string> Count(string type) => (_, _) => {
        ulong count = EntityCounts.EnsureGet(type, 1u);
        string text = count.ToString();
        EntityCounts.EnsureSet(type, ++count);
        return text;
    };

    public static Func<LevelTemplate, DebugEntityData, Color> ConstantColor(Color color) {
        return (room, entity) => color;
    }
    public static Color DefaultEntityColor = Color.Orange;

    public static Color GetEntityColor(LevelTemplate room, DebugEntityData entity) {
        Color color = DefaultEntityColor;
        if (EntityTypeHandlers.TryGetValue(entity.MapData.Name, out var typeHandler) && typeHandler != null) {
            Color? result = typeHandler.ColorGetter?.Invoke(room, entity);
            if (result != null) {
                color = (Color)result;
            }
        }
        return color;
    }

    public static void RenderEntityBounds(LevelTemplate room, DebugEntityData entity) {
        Draw.HollowRect(room.X + entity.MapData.Position.X / 8, room.Y + entity.MapData.Position.Y / 8, Math.Max(1, entity.MapData.Width / 8), Math.Max(1, entity.MapData.Height / 8), GetEntityColor(room, entity));
    }

    public static void RenderEntitySquare(LevelTemplate room, DebugEntityData entity) {
        Draw.HollowRect(room.X + entity.MapData.Position.X / 8 - 1, room.Y + entity.MapData.Position.Y / 8 - 1, 3, 3, GetEntityColor(room, entity));
    }

    public static Action<LevelTemplate, EntityData> ShouldAlwaysRender(string entityType) {
        return (room, entity) => {
            EntitiesToRender.EnsureGet(entityType, []).EnsureGet(room.Name, []).Add(new() {
                Room = room.Name,
                MapData = entity
            });
        };
    }

    public static GetterEventProperty<bool> MetadataBindEnabled = new(){Event = (ref bool val) => val = true};

    public static void RenderEntities(MapEditor debugMap, Camera camera) {
        foreach (var entityListsByType in EntitiesToRender) {
            foreach (var entitiesByRoom in entityListsByType.Value) {
                foreach (var entity in entitiesByRoom.Value) {
                    LevelTemplate room = debugMap.levels.FirstOrDefault(room => room.Name == entitiesByRoom.Key, null);
                    if (room != null && EntityTypeHandlers.TryGetValue(entityListsByType.Key, out var typeHandler)) {
                        typeHandler.Renderer?.Invoke(room, entity);
                    }
                }
            }
        }
        if (MRT.Settings.Bind_DebugEntityMetadata.Check && MetadataBindEnabled.Value) {
            Draw.Rect((float)Math.Floor(camera.Left), (float)Math.Floor(camera.Top), camera.Right - camera.Left + 2f, camera.Bottom - camera.Top + 2f, Color.Black * 0.25f);
            foreach (var entityListsByType in EntitiesToRender) {
                if (!EntityTypeHandlers.TryGetValue(entityListsByType.Key, out var typeHandler)) {break;}
                typeHandler.DataInitializer?.Invoke();
                foreach (var entitiesByRoom in entityListsByType.Value) {
                    foreach (var entity in entitiesByRoom.Value) {
                        LevelTemplate room = debugMap.levels.FirstOrDefault(room => room.Name == entitiesByRoom.Key, null);
                        if (room != null) {
                            string metadata = typeHandler.DataParser?.Invoke(room, entity);
                            if (metadata != null && metadata != "") {
                                ActiveFont.DrawOutline(
                                    metadata,
                                    new Vector2(room.X + entity.MapData.Position.X / 8f, room.Y + entity.MapData.Position.Y / 8f - 8f / camera.Zoom),
                                    new Vector2(0.5f, 0.5f),
                                    Vector2.One / camera.Zoom,
                                    GetEntityColor(room, entity),
                                    HoverTextBorderThickness / camera.Zoom,
                                    Color.Black
                                );
                            }
                        }
                    }
                }
            }
        }
    }

    public static void ResetEntitiesToRender() {
        EntitiesToRender.Clear();
    }

    public static void PopulateEntitiesToRender(DebugMapHooks.LoadEntityDataArgs args) {
        if (EntityTypeHandlers.TryGetValue(args.EntityData.Name, out var typeHandler)) {
            typeHandler.Reader?.Invoke(args.Room, args.EntityData);
        }
    }
  #endregion

  #region metadata items
    
    /// <summary>
    /// Contains the string that each entity type is saved as in level data (a map's bin). There are two ways to find this string for a given entity type:
    /// <list type="bullet">
    /// <item>
    /// Open a map in Lönn, find an entity of the desired type, and right click on it to open the properties window. The window title will contain the string.
    /// </item>
    /// <item>
    /// Open the source code for the entity type. If it's vanilla, find where the type's constructor is called in <see cref="Level.orig_LoadLevel(Player.IntroTypes, bool)"/>.
    /// If it's modded, find the <see cref="Entities.CustomEntityAttribute"/> attached to the type -- see the <seealso href="https://github.com/EverestAPI/Resources/wiki/Custom-Entities-and-Triggers#customentity">Everest wiki page</seealso>. 
    /// </item>
    /// </list>
    /// </summary>
    public static class LevelDataIDs {
        public const string Strawberry = "strawberry";
        public const string Checkpoint = "checkpoint";
        public const string Key = "key";
        public const string JumpThrough = "jumpThru";
    }

    public static Dictionary<string, EntityTypeHandler> EntityTypeHandlers = new(){
        {LevelDataIDs.Strawberry, new(){
            Reader = ShouldAlwaysRender(LevelDataIDs.Strawberry),
            ColorGetter = ConstantColor(Color.LightPink),
            Renderer = RenderEntitySquare,
            DataParser = ShowStrawberryMetadata
        }},
        {LevelDataIDs.Key, new(){
            Reader = ShouldAlwaysRender(LevelDataIDs.Key),
            ColorGetter = ConstantColor(Color.Gold),
            Renderer = RenderEntitySquare,
            DataInitializer = InitCount(LevelDataIDs.Key),
            DataParser = Count(LevelDataIDs.Key)
        }},
        {LevelDataIDs.Checkpoint, new() {
            Reader = ShouldAlwaysRender(LevelDataIDs.Checkpoint),
            ColorGetter = ConstantColor(Color.Lime),
            Renderer = RenderEntitySquare
        }},
        {LevelDataIDs.JumpThrough, new() {
            Reader = ShouldAlwaysRender(LevelDataIDs.JumpThrough),
            ColorGetter = ConstantColor(Color.Yellow),
            Renderer = RenderEntityBounds
        }}
    };

    public static string ShowStrawberryMetadata(LevelTemplate room, DebugEntityData entity) {
        return $"{entity.MapData.Int("checkpointID", 0)}:{entity.MapData.Int("order", 0)}";
    }
  #endregion
}