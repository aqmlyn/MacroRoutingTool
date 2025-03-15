using Celeste.Editor;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Cil;
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

    /// <summary>
    /// Whether typing right now will affect the text in a textbox.
    /// </summary>
    public static bool TextEditing;
}