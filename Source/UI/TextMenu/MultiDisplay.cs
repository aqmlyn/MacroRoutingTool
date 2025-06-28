using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.MacroRoutingTool.UI;

/// <summary>
/// Intended to be added to a <see cref="MenuDataContainer.MenuData"/> to change the <see cref="TextMenu"/> to be
/// able to be displayed with arbitrary position and dimensions and alongside other <see cref="TextMenu"/>s. 
/// </summary>
public class MultiDisplayData {
    /// <summary>
    /// Height to add to items' positions when rendering them, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.
    /// </summary>
    public float ScrollOffset;

    /// <summary>
    /// Maximum allowed value of ScrollOffset, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.<br/>
    /// Calculated to prevent scrolling past the point where the last item is fully visible.
    /// </summary>
    public float MaxScrollOffset;

    /// <summary>
    /// Amount by which the scale of each item's visuals (text and images) will be multiplied.
    /// </summary>
    public float ItemScaleMult = 1f;

    /// <summary>
    /// Maximum allowed value of ItemScaleMult.
    /// </summary>
    public float ItemScaleMaxMult = 1f;

    /// <summary>
    /// Combined height of each <c>Visible</c> item plus the vertical <c>ItemSpacing</c> added in between each item,
    /// <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.
    /// </summary>
    public float TotalItemHeight;

    public static ILHook ILTextMenuGetScrollTargetY;
    public static ILHook ILTextMenuOptionRender;
    public static ILHook ILTextMenuOptionHeight;

    public static void EnableAllHooks() {
        //hooking methods of generic classes: https://stackoverflow.com/questions/73835919
        //new ILHook(typeof(TextMenu.Option<>).GetMethod..., ...) will NOT work. the constructor won't find the instructions to populate the ILContext with. (i think. the error message is really vague.)
        //it also won't work for finding IL instructions, e.g. instr.MatchCall(typeof(TextMenu.Option<>).GetMethod...)
        //using dummy type arguments like below DOES work, but the hooks somehow get disabled after like a second. i have no idea why, so i gave up and just copied the option class instead

        ILTextMenuGetScrollTargetY = new(typeof(TextMenu).GetMethod("get_" + nameof(TextMenu.ScrollTargetY)), new(EnableILTextMenuGetScrollTargetY));
        //ILTextMenuOptionRender = new(typeof(TextMenu.Option<object>).GetMethod(nameof(TextMenu.Option<object>.Render)), new(EnableILTextMenuOptionRender));
        //ILTextMenuOptionHeight = new(typeof(TextMenu.Option<object>).GetMethod(nameof(TextMenu.Option<object>.Height)), new(EnableILTextMenuItemHeight));
        IL.Celeste.TextMenu.renderItems += EnableILTextMenuRenderItems;
        IL.Celeste.TextMenu.Button.Render += EnableILTextMenuButtonRender;
        IL.Celeste.TextMenu.Header.Render += EnableILTextMenuHeaderRender;
        IL.Celeste.TextMenu.Button.Height += EnableILTextMenuItemHeight;
        IL.Celeste.TextMenu.Header.Height += EnableILTextMenuItemHeight;
        On.Celeste.TextMenu.Update += OnTextMenuUpdate;
        On.Celeste.TextMenu.Added += OnTextMenuAdded;
        On.Celeste.TextMenu.RecalculateSize += OnTextMenuRecalculateSize;
    }

    public static void DisableAllHooks() {
        ILTextMenuGetScrollTargetY.Dispose();
        //ILTextMenuOptionRender.Dispose();
        //ILTextMenuOptionHeight.Dispose();
        IL.Celeste.TextMenu.renderItems -= EnableILTextMenuRenderItems;
        IL.Celeste.TextMenu.Button.Render -= EnableILTextMenuButtonRender;
        IL.Celeste.TextMenu.Header.Render -= EnableILTextMenuHeaderRender;
        IL.Celeste.TextMenu.Button.Height -= EnableILTextMenuItemHeight;
        IL.Celeste.TextMenu.Header.Height -= EnableILTextMenuItemHeight;
        On.Celeste.TextMenu.Update -= OnTextMenuUpdate;
        On.Celeste.TextMenu.Added -= OnTextMenuAdded;
        On.Celeste.TextMenu.RecalculateSize -= OnTextMenuRecalculateSize;
    }

    /// <summary>
    /// Calculates the value that a <see cref="TextMenu"/>'s <see cref="ScrollOffset"/> should approach.<br/>
    /// Returns the original value if the <see cref="TextMenu"/> does not contain a <see cref="MultiDisplayData"/>. 
    /// </summary>
    /// <param name="initial">Value to approach according to vanilla behavior.</param>
    /// <param name="menu"><see cref="TextMenu"/> for which the approach target is being calculated.</param>
    public static float GetScrollTargetY(float initial, TextMenu menu) {
        if (menu.Selection >= 0 && menu.TryGetData(out MultiDisplayData data)) {
            return Math.Max(data.MaxScrollOffset, -menu.GetYOffsetOf(menu.Items[menu.Selection]));
        } else {
            return initial;
        }
    }

    /// <summary>
    /// Calculates the absolute Y position at which each item should be drawn in a <see cref="TextMenu"/> containing a <see cref="MultiDisplayData"/>.<br/>
    /// Returns the original value if the <see cref="TextMenu"/> does not contain a <see cref="MultiDisplayData"/>. 
    /// </summary>
    /// <param name="initial">Absolute Y position at which the item will be drawn according to vanilla behavior.</param>
    /// <param name="menu"><see cref="TextMenu"/> for which the position is being calculated.</param>
    public static float AddScrollOffset(float initial, TextMenu menu) {
        if (menu.TryGetData(out MultiDisplayData data)) {
            return initial + data.ScrollOffset;
        }
        return initial;
    }

    /// <summary>
    /// Calculates the Y position, relative to the top of a <see cref="TextMenu"/> containing a <see cref="MultiDisplayData"/>, which an item should be
    /// considered offscreen if its center is below, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.<br/>
    /// Returns the original value if the <see cref="TextMenu"/> does not contain a <see cref="MultiDisplayData"/>.
    /// </summary>
    /// <param name="initial">Y position below which Everest's <see cref="TextMenu"/>.renderItems considers the item offscreen.</param>
    /// <param name="menu"><see cref="TextMenu"/> for which the position is being calculated.</param>
    public static int ItemInViewBottom(int initial, TextMenu menu) {
        if (menu.DataContains<MultiDisplayData>()) {
            return (int)menu.Height;
        }
        return initial;
    }

    public static Vector2 ApplyItemScaleMultToScale(Vector2 initial, TextMenu menu) {
        if (menu.TryGetData(out MultiDisplayData data)) {
            initial *= data.ItemScaleMult;
        }
        return initial;
    }

    public static float ApplyItemScaleMultToWidth(float initial, TextMenu menu) {
        if (menu.TryGetData(out MultiDisplayData data)) {
            initial *= data.ItemScaleMult;
        }
        return initial;
    }

    /// <summary>
    /// Add code to <see cref="TextMenu.ScrollTargetY"/>'s getter that calls <see cref="GetScrollTargetY"/>
    /// such that its return value will be returned instead of the original getter's return value.
    /// </summary>
    public static void EnableILTextMenuGetScrollTargetY(ILContext ilctx) {
        ILCursor ilcur = new(ilctx);

        ilcur.GotoNext(MoveType.Before, instr => instr.MatchRet());
        ilcur.EmitLdarg0();
        ilcur.EmitDelegate(GetScrollTargetY);
    }

    /// <summary>
    /// Add code to <see cref="TextMenu"/>.renderItems that:
    /// <list type="bullet">
    /// <item>calls <see cref="AddScrollOffset"/> to modify the position at which the item will be drawn, and</item>
    /// <item>calls <see cref="ItemInViewBottom"/> to modify the highest position at which the item will be considered offscreen below.</item>
    /// </list>
    /// </summary>
    public static void EnableILTextMenuRenderItems(ILContext ilctx) {
        ILCursor ilcur = new(ilctx);

        //assumptions:
        // - the first appearance of a TextMenu.Item.SelectWiggler access is where the draw position is being calculated
        // - after that, the first appearance of Engine.Height is the offscreen check

        //find where renderItems calculates drawPosition and add ScrollOffset to its Y value
        FieldInfo wigglerField = typeof(TextMenu.Item).GetField(nameof(TextMenu.Item.SelectWiggler));
        ilcur.GotoNext(MoveType.After, instr => instr.MatchLdfld(wigglerField));
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchNewobj(typeof(Vector2).GetConstructor([typeof(float), typeof(float)])));
        ilcur.EmitLdarg0();
        ilcur.EmitDelegate(AddScrollOffset);

        //find the offscreen check and replace it with an out-of-bounds check
        ilcur.GotoNext(MoveType.After, instr => instr.MatchCall(typeof(Engine).GetMethod("get_" + nameof(Engine.Height))));
        ilcur.EmitLdarg0();
        ilcur.EmitDelegate(ItemInViewBottom);
    }

    public static void EnableILTextMenuItemHeight(ILContext ilctx) {
        ILCursor ilcur = new(ilctx);

        ilcur.GotoNext(MoveType.Before, instr => instr.MatchRet());
        ilcur.EmitLdarg0();
        ilcur.EmitLdfld(typeof(TextMenu.Item).GetField(nameof(TextMenu.Item.Container)));
        ilcur.EmitDelegate(ApplyItemScaleMultToWidth);
    }

    public static void EnableILTextMenuHeaderRender(ILContext ilctx) {
        ILCursor ilcur = new(ilctx);

        ilcur.GotoNext(MoveType.Before, instr => instr.MatchCall(typeof(ActiveFont).GetMethod(nameof(ActiveFont.DrawEdgeOutline))));
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchCall(typeof(Vector2).GetMethod("get_" + nameof(Vector2.One))));
        ilcur.EmitLdarg0();
        ilcur.EmitLdfld(typeof(TextMenu.Item).GetField(nameof(TextMenu.Item.Container)));
        ilcur.EmitDelegate(ApplyItemScaleMultToScale);
    }

    public static void EnableILTextMenuButtonRender(ILContext ilctx) {
        ILCursor ilcur = new(ilctx);

        ilcur.GotoNext(MoveType.Before, instr => instr.MatchCall(typeof(ActiveFont).GetMethod(nameof(ActiveFont.DrawOutline))));
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchCall(typeof(Vector2).GetMethod("get_" + nameof(Vector2.One))));
        ilcur.EmitLdarg0();
        ilcur.EmitLdfld(typeof(TextMenu.Item).GetField(nameof(TextMenu.Item.Container)));
        ilcur.EmitDelegate(ApplyItemScaleMultToScale);
    }

    public static void EnableILTextMenuOptionRender(ILContext ilctx) {
        ILCursor ilcur = new(ilctx);

        //label
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchCall(typeof(ActiveFont).GetMethod(nameof(ActiveFont.DrawOutline))));
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchCall(typeof(Vector2).GetMethod("get_" + nameof(Vector2.One))));
        ilcur.EmitLdarg0();
        ilcur.EmitLdfld(typeof(TextMenu.Item).GetField(nameof(TextMenu.Item.Container)));
        ilcur.EmitDelegate(ApplyItemScaleMultToScale);

        //RightWidth copy used by everything below
        ilcur.GotoNext(MoveType.After, instr => instr.MatchCallvirt(typeof(TextMenu.Item).GetMethod(nameof(TextMenu.Item.RightWidth))));
        ilcur.EmitLdarg0();
        ilcur.EmitLdfld(typeof(TextMenu.Item).GetField(nameof(TextMenu.Item.Container)));
        ilcur.EmitDelegate(ApplyItemScaleMultToWidth);

        //selected option
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchCall(typeof(ActiveFont).GetMethod(nameof(ActiveFont.DrawOutline))));
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchCall(typeof(Vector2).GetMethod("get_" + nameof(Vector2.One))));
        ilcur.EmitLdarg0();
        ilcur.EmitLdfld(typeof(TextMenu.Item).GetField(nameof(TextMenu.Item.Container)));
        ilcur.EmitDelegate(ApplyItemScaleMultToScale);

        //<
        ilcur.GotoNext(MoveType.After, instr => instr.MatchCall(typeof(ActiveFont).GetMethod(nameof(ActiveFont.DrawOutline))));
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchCall(typeof(ActiveFont).GetMethod(nameof(ActiveFont.DrawOutline))));
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchCall(typeof(Vector2).GetMethod("get_" + nameof(Vector2.One))));
        ilcur.EmitLdarg0();
        ilcur.EmitLdfld(typeof(TextMenu.Item).GetField(nameof(TextMenu.Item.Container)));
        ilcur.EmitDelegate(ApplyItemScaleMultToScale);
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchLdcR4(40f));
        ilcur.EmitLdarg0();
        ilcur.EmitLdfld(typeof(TextMenu.Item).GetField(nameof(TextMenu.Item.Container)));
        ilcur.EmitDelegate(ApplyItemScaleMultToWidth);

        //>
        ilcur.GotoNext(MoveType.After, instr => instr.MatchCall(typeof(ActiveFont).GetMethod(nameof(ActiveFont.DrawOutline))));
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchCall(typeof(ActiveFont).GetMethod(nameof(ActiveFont.DrawOutline))));
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchCall(typeof(Vector2).GetMethod("get_" + nameof(Vector2.One))));
        ilcur.EmitLdarg0();
        ilcur.EmitLdfld(typeof(TextMenu.Item).GetField(nameof(TextMenu.Item.Container)));
        ilcur.EmitDelegate(ApplyItemScaleMultToScale);
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchLdcR4(40f));
        ilcur.EmitLdarg0();
        ilcur.EmitLdfld(typeof(TextMenu.Item).GetField(nameof(TextMenu.Item.Container)));
        ilcur.EmitDelegate(ApplyItemScaleMultToWidth);
    }

    public static void OnTextMenuUpdate(On.Celeste.TextMenu.orig_Update orig, TextMenu self) {
        orig(self);
        if (self.TryGetData(out MultiDisplayData data)) {
            //copied from decomp of orig TextMenu.Update
            data.ScrollOffset += (self.ScrollTargetY - data.ScrollOffset) * (1f - (float)Math.Pow(0.01f, Engine.RawDeltaTime));
        }
    }

    public static void OnTextMenuAdded(On.Celeste.TextMenu.orig_Added orig, TextMenu self, Scene scene) {
        if (self.TryGetData(out MultiDisplayData data)) {
            self.AutoScroll = false;
            orig(self, scene);
            data.ScrollOffset = self.Selection == -1 ? 0 : self.GetYOffsetOf(self.Items[self.Selection]);
        } else {
            orig(self, scene);
        }
    }

    public static void OnTextMenuRecalculateSize(On.Celeste.TextMenu.orig_RecalculateSize orig, TextMenu self) {
        if (self.TryGetData(out MultiDisplayData data)) {
            float maxWidth = 0f;
            Stack<float> itemHeights = [];
            bool scrollable = false;

            //measure total item height and text/image scale
            data.TotalItemHeight = 0f;
            foreach (TextMenu.Item item in self.Items) {
                if (item.Visible) {
                    maxWidth = Math.Max(maxWidth, item.LeftWidth() + item.RightWidth());
                    float fullHeight = item.Height() + self.ItemSpacing;
                    data.TotalItemHeight += fullHeight;
                    itemHeights.Push(fullHeight);
                }
            }
            data.ItemScaleMult = Math.Min(data.ItemScaleMaxMult, self.Width / maxWidth);
            data.TotalItemHeight -= self.ItemSpacing; //don't put spacing after the last item

            //find the highest item such that if that item was at the top, the last item would be completely visible
            data.MaxScrollOffset = data.TotalItemHeight;
            while (itemHeights.Any()) {
                float maxScroll = data.MaxScrollOffset - itemHeights.Pop();
                if (maxScroll + self.Height < data.TotalItemHeight) {
                    scrollable = true;
                    break;
                }
                data.MaxScrollOffset = maxScroll - self.ItemSpacing;
            }
            data.MaxScrollOffset = scrollable ? -data.MaxScrollOffset : 0;
        } else {
            //if there isn't a MultiDisplayData, this menu is expecting the vanilla behavior, so call the original method.
            orig(self);
        }
    }

    /// <summary>
    /// A copy of <see cref="TextMenu.Option{T}"/> created due to weird issues I was having when trying to use an IL hook instead.
    /// See <see cref="EnableAllHooks"/> for a description of those issues.
    /// See <see cref="EnableILTextMenuOptionRender"/> for the changes made to the vanilla method. 
    /// </summary>
    public class TextMenuOption<T>(string label) : TextMenu.Option<T>(label) {
        public override float Height()
        {
            return ActiveFont.LineHeight * (Container.TryGetData(out MultiDisplayData data) ? data.ItemScaleMult : 1f);
        }

        public override void Render(Vector2 position, bool highlighted)
        {
            float scale = Container.TryGetData(out MultiDisplayData data) ? data.ItemScaleMult : 1f;
            float alpha = Container.Alpha;
            Color strokeColor = Color.Black * (alpha * alpha * alpha);
            Color color = Disabled ? Color.DarkSlateGray : ((highlighted ? Container.HighlightColor : Color.White) * alpha);
            ActiveFont.DrawOutline(Label, position, new Vector2(0f, 0.5f), Vector2.One * scale, color, 2f, strokeColor);
            if (Values.Count > 0)
            {
                float num = RightWidth() * scale;
                ActiveFont.DrawOutline(Values[Index].Item1, position + new Vector2(Container.Width - num * 0.5f + lastDir * ValueWiggler.Value * 8f, 0f), new Vector2(0.5f, 0.5f), Vector2.One * scale * 0.8f, color, 2f, strokeColor);
                Vector2 vector = Vector2.UnitX * (highlighted ? ((float)Math.Sin((double)(sine * 4f)) * 4f) : 0f);
                bool flag = Index > 0;
                Color color2 = flag ? color : (Color.DarkSlateGray * alpha);
                Vector2 position2 = position + new Vector2(Container.Width - num + 40f * scale + ((lastDir < 0) ? (-ValueWiggler.Value * 8f) : 0f), 0f) - (flag ? vector : Vector2.Zero);
                ActiveFont.DrawOutline("<", position2, new Vector2(0.5f, 0.5f), Vector2.One * scale, color2, 2f, strokeColor);
                bool flag2 = Index < Values.Count - 1;
                Color color3 = flag2 ? color : (Color.DarkSlateGray * alpha);
                Vector2 position3 = position + new Vector2(Container.Width - 40f * scale + ((lastDir > 0) ? (ValueWiggler.Value * 8f) : 0f), 0f) + (flag2 ? vector : Vector2.Zero);
                ActiveFont.DrawOutline(">", position3, new Vector2(0.5f, 0.5f), Vector2.One * scale, color3, 2f, strokeColor);
            }
        }
    }
}