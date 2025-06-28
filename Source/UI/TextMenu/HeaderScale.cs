using System.Linq;
using Microsoft.Xna.Framework;
using MonoMod.Cil;

namespace Celeste.Mod.MacroRoutingTool.UI;

public partial class HeaderScaleData {
    public float Scale;
    
    public static void EnableAllHooks() {
        IL.Celeste.TextMenu.Header.Render += EnableILTextMenuHeaderRender;
        IL.Celeste.TextMenu.Header.Height += EnableILTextMenuHeaderMeasure;
        IL.Celeste.TextMenu.Header.LeftWidth += EnableILTextMenuHeaderMeasure;
    }

    public static void DisableAllHooks() {
        IL.Celeste.TextMenu.Header.Render -= EnableILTextMenuHeaderRender;
        IL.Celeste.TextMenu.Header.Height -= EnableILTextMenuHeaderMeasure;
        IL.Celeste.TextMenu.Header.LeftWidth -= EnableILTextMenuHeaderMeasure;
    }

    public static Vector2 ApplyScaleToScale(Vector2 initial, TextMenu.Header header) {
        MenuDataContainer dataContainer = (MenuDataContainer)header.Container.Items.FirstOrDefault(item => item is MenuDataContainer, null);
        if (dataContainer != null) {
            if (dataContainer.TryGetItemData(header, out HeaderScaleData scaleData)) {
                initial *= scaleData.Scale;
            }
        }
        return initial;
    }

    public static float ApplyScaleToMeasure(float initial, TextMenu.Header header) {
        MenuDataContainer dataContainer = (MenuDataContainer)header.Container.Items.FirstOrDefault(item => item is MenuDataContainer, null);
        if (dataContainer != null) {
            if (dataContainer.TryGetItemData(header, out HeaderScaleData scaleData)) {
                initial *= scaleData.Scale;
            }
        }
        return initial;
    }

    public static void EnableILTextMenuHeaderRender(ILContext ilctx) {
        ILCursor ilcur = new(ilctx);

        ilcur.GotoNext(MoveType.Before, instr => instr.MatchCall(typeof(ActiveFont).GetMethod(nameof(ActiveFont.DrawEdgeOutline))));
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchCall(typeof(Vector2).GetMethod("get_" + nameof(Vector2.One))));
        ilcur.EmitLdarg0();
        ilcur.EmitDelegate(ApplyScaleToScale);
    }

    public static void EnableILTextMenuHeaderMeasure(ILContext ilctx) {
        ILCursor ilcur = new(ilctx);

        ilcur.GotoNext(MoveType.Before, instr => instr.MatchRet());
        ilcur.EmitLdarg0();
        ilcur.EmitDelegate(ApplyScaleToMeasure);
    }
}