using Celeste.Editor;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.MacroRoutingTool.UI;

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
            return Input.GuiSingleButton(bind.Buttons[^1], "controls/keyboard/oemquestion"); //need to specify fallback to avoid ambiguous call
        } else {
            return Input.GuiMouseButton(MInput.MouseData.MouseButtons.Left);
        }
    }

    public static class AtlasPaths {
        public const string Root = "Graphics/Atlases/";
        public const string Game = Root + "Gameplay/";
        public const string Opening = Root + "Opening/";
        public const string Gui = Root + "Gui/";
        public const string Misc = Root + "Misc/";
        public const string Portraits = Root + "Portraits/";
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
            string atlasPath = "";
            string fullPathCopy = path;
            for (int i = 0; i < 3; i++) {
                int idx = fullPathCopy.IndexOf('/') + 1;
                atlasPath += fullPathCopy[..idx];
                fullPathCopy = fullPathCopy[idx..];
            }
            if (AtlasesByPath.TryGetValue(atlasPath, out Atlas atlas)) {
                return atlas.textures.TryGetValue(fullPathCopy, out texture);
            }
        }
        return false;
    }

    public static float WidestCharWidth => ActiveFont.FontSize.Characters.Max(ch => ch.Value.XAdvance);

    public class SpriteBatchArgs {
        public SpriteSortMode SpriteSortMode;
        public BlendState BlendState;
        public SamplerState SamplerState;
        public DepthStencilState DepthStencilState;
        public RasterizerState RasterizerState;
        public Effect Effect;
        public Matrix Matrix;
    }

    /// <summary>
    /// Emit IL code that calls <c>Monocle.Draw.SpriteBatch.Begin</c> with the given arguments.
    /// </summary>
    /// <param name="args">The arguments to call <c>Monocle.Draw.SpriteBatch.Begin</c> with.</param>
    public static void EmitSpriteBatchBegin(this ILCursor ilcur, SpriteBatchArgs args){
        ilcur.EmitCall(typeof(Draw).GetMethod("get_" + nameof(Draw.SpriteBatch)));
        ilcur.EmitLdcI4((int)args.SpriteSortMode);
        ilcur.EmitLdsfld(typeof(SpriteBatchArgs).GetField(nameof(SpriteBatchArgs.BlendState)));
        ilcur.EmitLdsfld(typeof(SpriteBatchArgs).GetField(nameof(SpriteBatchArgs.SamplerState)));
        ilcur.EmitLdsfld(typeof(SpriteBatchArgs).GetField(nameof(SpriteBatchArgs.DepthStencilState)));
        ilcur.EmitLdsfld(typeof(SpriteBatchArgs).GetField(nameof(SpriteBatchArgs.RasterizerState)));
        if (args.Effect == null) {
            ilcur.EmitLdnull();
        } else {
            ilcur.EmitLdsfld(typeof(SpriteBatchArgs).GetField(nameof(SpriteBatchArgs.Effect)));
        }
        ilcur.EmitLdsfld(typeof(SpriteBatchArgs).GetField(nameof(SpriteBatchArgs.Matrix)));
        ilcur.EmitCallvirt(typeof(SpriteBatch).GetMethod(nameof(SpriteBatch.Begin), [typeof(SpriteSortMode), typeof(BlendState), typeof(SamplerState), typeof(DepthStencilState), typeof(RasterizerState), typeof(Effect), typeof(Matrix)]));
    }

    public static FieldInfo AreaGetter = typeof(MapEditor).GetField("area", BindingFlags.NonPublic | BindingFlags.Static);
    public static AreaKey GetAreaKey(MapEditor debugMap = null) => (AreaKey)AreaGetter.GetValue(debugMap ?? (MapEditor)Engine.Scene);
    public static AreaData GetAreaData(MapEditor debugMap = null) => AreaData.Get(GetAreaKey(debugMap));

    public class TextureElement {
        public MTexture Texture = null;
        public Vector2 Position = Vector2.Zero;
        public Vector2 Justify = Vector2.Zero;
        public Color Color = Color.White;
        public Vector2 Scale = Vector2.One;
        public float Rotation = 0f;
        public SpriteEffects Flip = SpriteEffects.None;
        public float? BorderThickness;
        public Color BorderColor = Color.Black;

        public bool IgnoreCameraZoom = false;
        public Camera Camera = null;

        public void Render() {
            if (Texture == null) {return;}
            //copied from vanilla MTexture.DrawOutlineJustified decomp and adjusted to take outline arguments into account
            float scaleFix = Texture.ScaleFix;
            Vector2 scale = Scale * scaleFix;
            if (IgnoreCameraZoom) {scale /= Camera.Zoom;}
            Rectangle clipRect = Texture.ClipRect;
            Vector2 origin = (new Vector2(Texture.Width * Justify.X, Texture.Height * Justify.Y) - Texture.DrawOffset) / scaleFix;
            if (BorderThickness != null) {
                float outlineWidth = (float)BorderThickness;
                if (IgnoreCameraZoom) {outlineWidth /= Camera.Zoom;}
                for (float i = -1; i <= 1; i++) {
                    for (float j = -1; j <= 1; j ++) {
                        if (i != 0 || j != 0) {
                            Draw.SpriteBatch.Draw(Texture.Texture.Texture_Safe, Position + new Vector2(i * outlineWidth, j * outlineWidth), clipRect, BorderColor, Rotation, origin, scale, Flip, 0f);
                        }
                    }
                }
            }
            Draw.SpriteBatch.Draw(Texture.Texture.Texture_Safe, Position, clipRect, Color, Rotation, origin, scale, Flip, 0f);
        }
    }

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
        float angleFrom1to2 = (p2 - p1).Angle();
        float angleFrom2to1 = (float)(angleFrom1to2 + Math.PI);

        //if angle self,p1,p2 is obtuse, use distance from p1
        float angleFrom1ToSelf = (p1 - self).Angle();
        float angleDiff1 = Math.Abs(angleFrom1ToSelf - angleFrom1to2);
        if (angleDiff1 > Math.PI * 0.5 && angleDiff1 < Math.PI * 1.5) {
            return (p1 - self).Length();
        }

        //if angle self,p2,p1 is obtuse, use distance from p2
        float angleFrom2ToSelf = (p2 - self).Angle();
        float angleDiff2 = Math.Abs(angleFrom2ToSelf - angleFrom2to1);
        if (angleDiff2 > Math.PI * 0.5 && angleDiff2 < Math.PI * 1.5) {
            return (p2 - self).Length();
        }

        //use distance from line
        return (float)Math.Sqrt((self - p1).LengthSquared() - (p2 - p1).LengthSquared());
    }
}