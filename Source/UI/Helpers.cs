using Celeste.Editor;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static class UIHelperHooks {
    public static ILHook ILSpriteBatchBegin;
    public static ILHook ILSpriteBatchEnd;
    
    public static void EnableAll() {
        ILSpriteBatchBegin = new(typeof(SpriteBatch).GetMethod(nameof(SpriteBatch.Begin), [typeof(SpriteSortMode), typeof(BlendState), typeof(SamplerState), typeof(DepthStencilState), typeof(RasterizerState), typeof(Effect), typeof(Matrix)]), EnableIL_SpriteBatchBegin);
        ILSpriteBatchEnd = new(typeof(SpriteBatch).GetMethod(nameof(SpriteBatch.End)), EnableIL_SpriteBatchEnd);
    }

    public static void DisableAll() {
        ILSpriteBatchBegin.Dispose();
        ILSpriteBatchEnd.Dispose();
    }
    
    public static void EnableIL_SpriteBatchBegin(ILContext ilctx) {
        ILCursor ilcur = new(ilctx);
        ilcur.EmitLdarg0(); //EmitLdarga(0) is invalid btw
        ilcur.EmitLdarga(7);
        ilcur.EmitLdfld(typeof(Matrix).GetField(nameof(Matrix.M11)));
        ilcur.EmitCall(typeof(UIHelpers).GetMethod(nameof(UIHelpers.OnSpriteBatchBegin)));
    }
    
    public static void EnableIL_SpriteBatchEnd(ILContext ilctx) {
        ILCursor ilcur = new(ilctx);
        ilcur.EmitLdarg0();
        ilcur.EmitCall(typeof(UIHelpers).GetMethod(nameof(UIHelpers.OnSpriteBatchEnd)));
    }
}

public static class UIHelpers {
    /// <summary>
    /// Finds a texture for a given bind using prioritization designed for the debug map.<br/>
    /// <list type="number">
    /// <item>If there is any key bound to the action, display that.</item>
    /// <item>If a controller is plugged in and there is any controller input bound to the action, display that.</item>
    /// <item>Otherwise, display left click to indicate that a mouse can interact with the UI.</item>
    /// </list>
    /// </summary>
    /// <param name="bind">The <c>ButtonBinding</c> to find a texture for.</param>
    public static MTexture DebugBindTexture(ButtonBinding bind) {
        if (bind.Keys.Count > 0) {
            return Input.GuiKey(bind.Keys[^1]); //in both bind.Keys and bind.Buttons, the last element is the one most recently added
        } else if (MInput.GamePads.Any(gp => gp.Attached) && bind.Buttons.Count > 0) {
            return Input.GuiSingleButton(bind.Buttons[^1], "controls/keyboard/oemquestion"); //fallback won't be used, but must be specified to avoid ambiguous call
        } else {
            return Input.GuiMouseButton(MInput.MouseData.MouseButtons.Left);
        }
    }

    public static class AtlasPaths {
        public const string Root = "Graphics/Atlases/";
        public const string Game = "Gameplay/";
        public const string Opening = "Opening/";
        public const string Gui = "Gui/";
        public const string Misc = "Misc/";
        public const string Portraits = "Portraits/";
    }
    public static Dictionary<string, Atlas> AtlasesByPath = new(){
        {AtlasPaths.Game, GFX.Game},
        {AtlasPaths.Opening, GFX.Opening},
        {AtlasPaths.Gui, GFX.Gui},
        {AtlasPaths.Misc, GFX.Misc},
        {AtlasPaths.Portraits, GFX.Portraits}
    };
    public static bool TryGetTexture(string path, out MTexture texture) {
        texture = null;
        if (path.StartsWith(AtlasPaths.Root)) {
            path = path[AtlasPaths.Root.Length..];
        }
        int idx = path.IndexOf('/') + 1;
        if (idx != 0 && AtlasesByPath.TryGetValue(path[..idx], out Atlas atlas)) {
            return atlas.textures.TryGetValue(path[idx..], out texture);
        }
        return false;
    }

    public static float WidestCharWidth => ActiveFont.FontSize.Characters.Max(ch => ch.Value.XAdvance);

    public const string SpriteBatchZoom = nameof(SpriteBatchZoom);
    /// <summary>
    /// Stores data about all <see cref="SpriteBatch"/>es on which <see cref="SpriteBatch.Begin"/> have been called but <see cref="SpriteBatch.End"/> haven't. 
    /// </summary>
    public static Dictionary<SpriteBatch, Dictionary<string, object>> SpriteBatches = [];
    /// <remarks>
    /// The current method of finding <see cref="SpriteBatchZoom"/> only considers multiplications to the current <see cref="SpriteBatch"/>'s
    /// transformation matrix. It's probably possible to take <i>some</i> more effects into account, but a completely effective method would need
    ///  to implement code analysis at an extreme scope to determine which <see cref="SpriteBatch"/> is being rendered to and which
    /// <see cref="Camera"/>'s zoom is currently affecting it.
    /// </remarks>
    public static void OnSpriteBatchBegin(SpriteBatch batch, float curZoom) {
        SpriteBatches.EnsureGet(batch, [])[SpriteBatchZoom] = curZoom * Engine.Width / Engine.ViewWidth;
    }
    public static void OnSpriteBatchEnd(SpriteBatch batch) {
        SpriteBatches.Remove(batch);
    }

    public static FieldInfo AreaGetter = typeof(MapEditor).GetField("area", BindingFlags.NonPublic | BindingFlags.Static);
    public static AreaKey GetAreaKey(MapEditor debugMap = null) => (AreaKey)AreaGetter.GetValue(debugMap ?? (MapEditor)Engine.Scene);
    public static AreaData GetAreaData(MapEditor debugMap = null) => AreaData.Get(GetAreaKey(debugMap));

    //all remaining uses of this are related to ListItems, which are being replaced
    public class TextElement {
        public TextMenu.Item Container = null;
        public string Text = "";
        public Vector2 Position = Vector2.Zero;
        public Vector2 Justify = Vector2.Zero;
        public Vector2 Scale = Vector2.One;
        public Color Color = Color.White;
        public float? BorderThickness = null;
        public Color BorderColor = Color.Black;
        public float? DropShadowOffset = null;
        public Color DropShadowColor = Color.DarkSlateBlue;

        public bool IgnoreCameraZoom = false;
        public Camera Camera = null;

        public void Render() {
            Vector2 scale = new(Scale.X, Scale.Y);
            if (IgnoreCameraZoom) {scale /= Camera.Zoom;}
            if (DropShadowOffset != null) {
                ActiveFont.DrawEdgeOutline(Text, Position, Justify, scale, Color, (float)DropShadowOffset, DropShadowColor, (BorderThickness ?? 0) / (IgnoreCameraZoom ? Camera.Zoom : 1f), BorderColor);
            } else if (BorderThickness != null) {
                ActiveFont.DrawOutline(Text, Position, Justify, scale, Color, (float)BorderThickness / (IgnoreCameraZoom ? Camera.Zoom : 1f), BorderColor);
            } else {
                ActiveFont.Draw(Text, Position, Justify, scale, Color);
            }
        }
    }

    public static Vector2 OnCamera(this Vector2 self, Camera camera) {
        return new(camera.Left + (camera.Origin.X + self.X) / camera.Zoom, camera.Top + (camera.Origin.Y + self.Y) / camera.Zoom);
    }

    public static float DistanceToLineSegment(this Vector2 self, Vector2 p1, Vector2 p2) {
        //if angle self,p1,p2 is obtuse, p1 is the closest point
        float p1angle = Math.Abs((p2 - p1).Angle() - (self - p1).Angle());
        if (p1angle > Math.PI * 0.5 && p1angle < Math.PI * 1.5) {
            return (p1 - self).Length();
        }

        //if angle self,p2,p1 is obtuse, p2 is the closest point
        float p2angle = Math.Abs((p1 - p2).Angle() - (self - p2).Angle());
        if (p2angle > Math.PI * 0.5 && p2angle < Math.PI * 1.5) {
            return (p2 - self).Length();
        }

        //if both are acute, closest point is on the line that passes through p1 and p2
        return (float)(Math.Abs(Math.Sin((p2 - p1).Angle() - (self - p1).Angle())) * (self - p1).Length());
    }
}