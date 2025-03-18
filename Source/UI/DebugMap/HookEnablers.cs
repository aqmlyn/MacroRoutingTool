using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Editor;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.MacroRoutingTool.UI;

/// <summary>
/// Contains all IL hooks used by MacroRoutingTool to alter the debug map's existing methods.
/// </summary>
public static partial class DebugMapHooks {
    public static ILHook ILSceneOrigRender;
    public static ILHook ILSceneOrigUpdate;

    /// <summary>
    /// Modify the IL of <see cref="LevelTemplate(LevelData)"/>, the constructor for non-filler rooms in the debug map. Current modifications are:
    /// <list type="bullet">
    /// <item>a</item>
    /// </list>
    /// </summary>
    /// <param name="ilctx">Context from which an <see cref="ILCursor"><c>ILCursor</c></see> can be created to modify the IL with.</param>
    public static void EnableIL_RoomCtor(ILContext ilctx) {
        ILCursor ilcur = new(ilctx);

        ILLabel loopHeadLabel = null;
        ILLabel loopBodyLabel = null;
        ilcur.GotoNext(MoveType.After, instr => instr.MatchStloc(7));
        ilcur.GotoNext(MoveType.After, instr => instr.MatchBr(out loopHeadLabel));
        ilcur.GotoLabel(loopHeadLabel);
        ilcur.GotoNext(MoveType.After, instr => instr.MatchBrtrue(out loopBodyLabel));
        Instruction loopLeaveInstr = ilcur.Next;

        ilcur.GotoLabel(loopBodyLabel, MoveType.After);
        ilcur.GotoNext(MoveType.After, instr => instr.MatchStloc(8));
      #region load room data
        FieldInfo currentLoadArgs = typeof(DebugMapHooks).GetField(nameof(CurrentLoadEntityDataArgs), BindingFlags.Public | BindingFlags.Static); 

        ilcur.EmitNewobj(typeof(LoadEntityDataArgs).GetConstructors()[0]);
        ilcur.EmitPop();
        ilcur.EmitLdsfld(currentLoadArgs);
        ilcur.EmitLdloc(8);
        ilcur.EmitStfld(typeof(LoadEntityDataArgs).GetField(nameof(LoadEntityDataArgs.EntityData)));
        ilcur.EmitLdsfld(currentLoadArgs);
        ilcur.EmitLdarg(0);
        ilcur.EmitStfld(typeof(LoadEntityDataArgs).GetField(nameof(LoadEntityDataArgs.Room)));
        ilcur.EmitCall(typeof(DebugMapHooks).GetMethod(nameof(CallLoadEntityData)));
        ilcur.EmitBr(loopHeadLabel);
      #endregion
    }

    public class MapOrigUpdateILMouseModeCheck {
        public ILLabel LabelAfterEnd = null;
        public Instruction FirstInstruction = null;
    }

    /// <summary>
    /// Modify the IL of the debug map's original <c>Update</c> method. Current modifications are:
    /// <list type="bullet">
    /// <item>a</item>
    /// </list>
    /// </summary>
    /// <param name="ilctx">Context from which an <see cref="ILCursor"><c>ILCursor</c></see> can be created to modify the IL with.</param>
    public static void EnableIL_MapOrigUpdate(ILContext ilctx) {
        ILCursor ilcur = new(ilctx);

      #region directional pan speed
        //assumptions:
        //the first call to the camera's position setter after the first call to the mousewheel delta's getter is where the camera gets panned by directional input
        //the last two loads of a VirtualIntegerAxis.Value before the position setter are the base values to move the camera by

        //find where directional input is used to pan the camera...
        ilcur.GotoNext(MoveType.After, instr => instr.MatchCallOrCallvirt(typeof(MInput.MouseData).GetMethod("get_" + nameof(MInput.MouseData.WheelDelta))));
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchCallOrCallvirt(typeof(Camera).GetMethod("set_" + nameof(Camera.Position))));
        ilcur.GotoPrev(MoveType.Before, instr => instr.MatchCallOrCallvirt(typeof(Vector2).GetMethod("op_Addition", [typeof(Vector2), typeof(Vector2)])));

        //...and multiply it by a value with an event attached
        //TODO this should probably just replace the vector, not specifically multiply it by a float
        ilcur.EmitLdsfld(typeof(DebugMapHooks).GetField(nameof(DirectionalPanSpeedMult)));
        ilcur.EmitCall(typeof(GetterEventProperty<float>).GetMethod("get_" + nameof(GetterEventProperty<float>.Value)));
        ilcur.EmitCall(typeof(Vector2).GetMethod("op_Multiply", [typeof(Vector2), typeof(float)]));
      #endregion

      #region disable room controls
        //assumptions:
        // - the first instance of setting the value of mouseDragStart is inside the MouseModes.Hover block (all the other mouse modes are for dragging)
        // - before that, the last instance of checking the mouse mode is the start of the MouseModes.Hover block (if that wasn't true, it'd mean someone is checking the mouse mode when they already know it)

        FieldInfo enabledField = typeof(DebugMapHooks).GetField(nameof(RoomControlsEnabled));
        FieldInfo hookArgs = typeof(GetterEventProperty<bool, MapEditor>).GetField(nameof(GetterEventProperty<bool, MapEditor>.Arguments));
        MethodInfo getControlHookValue = typeof(GetterEventProperty<bool, MapEditor>).GetMethod("get_" + nameof(GetterEventProperty<bool, MapEditor>.Value));

        FieldInfo mouseModeField = typeof(MapEditor).GetField(nameof(MapEditor.mouseMode), BindingFlags.NonPublic | BindingFlags.Instance);

        ilcur.GotoNext(MoveType.Before, instr => instr.MatchStfld(typeof(MapEditor).GetField(nameof(MapEditor.mouseDragStart), BindingFlags.NonPublic | BindingFlags.Instance)));
        Instruction setMouseDragStart = ilcur.Next;
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchLdfld(mouseModeField));

        //store the start and end of the block for each mouse mode
        Dictionary<MapEditor.MouseModes, MapOrigUpdateILMouseModeCheck> mouseModeBlocks = [];
        int modeInt = 0;
        //TODO do all compilations have this optimization for mode 0? in case one doesn't, maybe i could limit the range for this brtrue search to be between ldfld mouseModeField and setMouseDragStart?
        mouseModeBlocks.Add(MapEditor.MouseModes.Hover, new());
        ilcur.GotoNext(MoveType.After, instr => instr.MatchBrtrue(out mouseModeBlocks[0].LabelAfterEnd));
        mouseModeBlocks[0].FirstInstruction = ilcur.Next;
        ilcur.GotoLabel(mouseModeBlocks[0].LabelAfterEnd, MoveType.After);
        while (++modeInt < Enum.GetValues(typeof(MapEditor.MouseModes)).Length && ilcur.TryGotoNext(MoveType.After, instr => instr.MatchLdfld(mouseModeField))) {
            MapEditor.MouseModes mode = (MapEditor.MouseModes)modeInt;
            mouseModeBlocks.Add(mode, new());
            ilcur.GotoNext(MoveType.After, instr => instr.MatchBneUn(out mouseModeBlocks[mode].LabelAfterEnd));
            mouseModeBlocks[mode].FirstInstruction = ilcur.Next;
            ilcur.GotoLabel(mouseModeBlocks[mode].LabelAfterEnd, MoveType.After);
        }
        ILLabel afterMouseInputs = mouseModeBlocks[(MapEditor.MouseModes)mouseModeBlocks.Count - 1].LabelAfterEnd;

        void emitAltHook(string evPropName, Action ifTrue, ILLabel ifFalse = null) {
            ifFalse ??= afterMouseInputs;
            FieldInfo evProp = typeof(DebugMapHooks).GetField(evPropName);

            //if RoomControlsEnabled, go to the vanilla code
            emitCheckRoomControlsEnabled();
            //smth about the brtrue that goes here causes problems if after emitting it, more instructions are emitted before the target.
            //so need to save the position, emit the other instructions, then come back and emit this instruction
            Instruction getRoomControlsEnabled = ilcur.Prev;
            //run the hook's getter to decide whether that event happened
            ilcur.EmitLdsfld(evProp);
            ilcur.EmitLdarg(0);
            ilcur.EmitStfld(hookArgs);
            ilcur.EmitLdsfld(evProp);
            ilcur.EmitCall(getControlHookValue);
            ilcur.EmitBrfalse(ifFalse);
            //if so, emit specified instructions
            ifTrue?.Invoke();
            ilcur.EmitBr(ifFalse);
            //now edit that one brtrue from earlier
            ILLabel origCode = ilcur.MarkLabel(ilcur.Next);
            ilcur.Goto(getRoomControlsEnabled, MoveType.After);
            ilcur.EmitBrtrue(origCode);
        }

        Action emitSetMouseMode(MapEditor.MouseModes mode) => () => {
            ilcur.EmitLdarg0();
            ilcur.EmitLdcI4((int)mode);
            ilcur.EmitStfld(mouseModeField);
        };

        void emitCheckRoomControlsEnabled() {
            ilcur.EmitLdsfld(enabledField);
            ilcur.EmitLdarg(0);
            ilcur.EmitStfld(hookArgs);
            ilcur.EmitLdsfld(enabledField);
            ilcur.EmitCall(getControlHookValue);
        }

        //hover mode:
        ilcur.Goto(mouseModeBlocks[MapEditor.MouseModes.Hover].FirstInstruction, MoveType.Before);
        //hook ctrl+lclick (vanilla: toggles whether rooms under the cursor are selected)
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchLdloc2());
        emitAltHook(nameof(ToggleSelectionPoint), emitSetMouseMode(MapEditor.MouseModes.Select));
        //disable F+lclick (vanilla: creates filler room)
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchCall(typeof(MInput).GetMethod("get_" + nameof(MInput.Keyboard))), instr => instr.MatchLdcI4('F'));
        Instruction fClickCheck = ilcur.Next;
        ILLabel notFClick = null;
        ilcur.GotoNext(MoveType.Before, ilcur => ilcur.MatchBrtrue(out notFClick) || ilcur.MatchBrfalse(out notFClick));
        ilcur.Goto(fClickCheck, MoveType.AfterLabel);
        emitCheckRoomControlsEnabled();
        ilcur.EmitBrfalse(notFClick);
        //hook pressing left mouse anywhere over a room (vanilla: replaces selection with rooms under cursor)
        ilcur.GotoNext(MoveType.After, instr => instr.MatchLdloc2());
        Instruction startDragRoomCheck = ilcur.Prev;
        ILLabel dragShouldSelect = null;
        ilcur.GotoNext(MoveType.After, instr => instr.MatchBrtrue(out dragShouldSelect) || instr.MatchBrfalse(out dragShouldSelect));
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchLdloc3());
        Instruction startInterpretDrag = ilcur.Next;
        ilcur.Goto(startDragRoomCheck, MoveType.AfterLabel);
        emitAltHook(nameof(ReplaceSelectionPoint), () => ilcur.EmitBr(startInterpretDrag), dragShouldSelect);
        //hook pressing left mouse elsewhere over a room (vanilla: starts moving room)
        ilcur.Goto(startInterpretDrag, MoveType.AfterLabel);
        emitAltHook(nameof(StartMove), emitSetMouseMode(MapEditor.MouseModes.Move));
        //hook pressing left mouse over resize handle (vanilla: starts resizing room)
        ilcur.Goto(startInterpretDrag, MoveType.AfterLabel);
        emitAltHook(nameof(StartResize), emitSetMouseMode(MapEditor.MouseModes.Resize), ilcur.MarkLabel(ilcur.Next));
        //disable all right click actions (vanilla: F+rclick deletes hovered filler room, normal right click teleports to room)
        ilcur.GotoNext(MoveType.After, instr => instr.MatchCallvirt(typeof(MInput.MouseData).GetMethod("get_" + nameof(MInput.MouseData.PressedRightButton))));
        ilcur.GotoNext(MoveType.After, instr => instr.MatchBrtrue(out _) || instr.MatchBrfalse(out _));
        ilcur.MoveAfterLabels();
        emitCheckRoomControlsEnabled();
        ilcur.EmitBrfalse(afterMouseInputs);
        //select mode:
        ilcur.Goto(mouseModeBlocks[MapEditor.MouseModes.Select].FirstInstruction, MoveType.AfterLabel);
        //hook every frame in select mode (vanilla: makes rooms in the selection rectangle appear hovered)
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchCallvirt(typeof(MInput.MouseData).GetMethod("get_" + nameof(MInput.MouseData.CheckLeftButton))));
        ilcur.GotoPrev(MoveType.AfterLabel, instr => instr.MatchCall(typeof(MInput).GetMethod("get_" + nameof(MInput.Mouse))));
        ILLabel startCheckReleaseSelect = ilcur.MarkLabel(ilcur.Next);
        ilcur.Goto(mouseModeBlocks[MapEditor.MouseModes.Select].FirstInstruction, MoveType.AfterLabel);
        emitCheckRoomControlsEnabled();
        Instruction selectCheckRoomControls = ilcur.Prev;
        ilcur.EmitLdarg0();
        ilcur.EmitDelegate(WhileSelecting);
        ilcur.Goto(selectCheckRoomControls, MoveType.After);
        ilcur.EmitBrfalse(startCheckReleaseSelect);
        //hook releasing left mouse in select mode (vanilla: either merges or replaces the selection list with the rooms in the selection rectangle)
        ilcur.Goto(startCheckReleaseSelect.Target);
        ilcur.GotoNext(MoveType.After, instr => instr.MatchBrtrue(out _) || instr.MatchBrfalse(out _));
        ilcur.MoveAfterLabels();
        emitAltHook(nameof(CommitSelectionRect), emitSetMouseMode(MapEditor.MouseModes.Hover));
        //move mode:
        ilcur.Goto(mouseModeBlocks[MapEditor.MouseModes.Move].FirstInstruction, MoveType.AfterLabel);
        //hook every frame in move mode (vanilla: moves selected rooms)
        emitCheckRoomControlsEnabled();
        Instruction moveCheckRoomControls = ilcur.Prev;
        ilcur.EmitLdarg0();
        ilcur.EmitDelegate(WhileMoving);
        Instruction callMovingHook = ilcur.Prev;
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchCallvirt(typeof(MInput.MouseData).GetMethod("get_" + nameof(MInput.MouseData.CheckLeftButton))));
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchCall(typeof(MInput).GetMethod("get_" + nameof(MInput.Mouse))));
        ILLabel moveCheckReleased = ilcur.MarkLabel(ilcur.Prev);
        ilcur.Goto(callMovingHook, MoveType.After);
        ilcur.EmitBr(moveCheckReleased);
        ILLabel origMoveCode = ilcur.MarkLabel(ilcur.Next);
        ilcur.Goto(moveCheckRoomControls, MoveType.After);
        ilcur.EmitBrtrue(origMoveCode);
        //resize mode:
        ilcur.Goto(mouseModeBlocks[MapEditor.MouseModes.Resize].FirstInstruction, MoveType.AfterLabel);
        //hook every frame in resize mode (vanilla: resizes selected filler rooms)
        emitCheckRoomControlsEnabled();
        Instruction resizeCheckRoomControls = ilcur.Prev;
        ilcur.EmitLdarg0();
        ilcur.EmitDelegate(WhileMoving);
        Instruction callResizingHook = ilcur.Prev;
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchCallvirt(typeof(MInput.MouseData).GetMethod("get_" + nameof(MInput.MouseData.CheckLeftButton))));
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchCall(typeof(MInput).GetMethod("get_" + nameof(MInput.Mouse))));
        ILLabel resizeCheckReleased = ilcur.MarkLabel(ilcur.Prev);
        ilcur.Goto(callResizingHook, MoveType.After);
        ilcur.EmitBr(resizeCheckReleased);
        ILLabel origResizeCode = ilcur.MarkLabel(ilcur.Next);
        ilcur.Goto(resizeCheckRoomControls, MoveType.After);
        ilcur.EmitBrtrue(origResizeCode);
        //mode agnostic:
        ilcur.Goto(afterMouseInputs.Target, MoveType.AfterLabel);
        //disable number key hardcoded actions (vanilla: recolor selected rooms)
        ilcur.GotoNext(MoveType.After, instr => instr.MatchLdcI4('1'));
        ILLabel afterSetColor = null;
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchBr(out afterSetColor));
        ilcur.Goto(afterMouseInputs.Target, MoveType.AfterLabel);
        emitCheckRoomControlsEnabled();
        ilcur.EmitBrfalse(afterSetColor);
      #endregion

      #region input events
        //assumptions:
        //

        ilcur.Index = ilctx.Instrs.Count;
        ilcur.GotoPrev(MoveType.Before, instr => instr.MatchCall(typeof(Scene).GetMethod(nameof(Scene.Update))));
        ilcur.GotoPrev(MoveType.Before, instr => instr.MatchLdarg0());

        ilcur.EmitLdarg0();
        ilcur.EmitDelegate(CallInputEvents);
      #endregion
    }

    /// <summary>
    /// Modify the IL of the debug map's original <c>Render</c> method. Current modifications are:
    /// <list type="bullet">
    /// <item>Skip all the code to render the vanilla headbar, instead call <see cref="GraphViewer.RenderFixed"><c>GraphViewer.Render</c></see> to render a custom headbar and text menu.</item>
    /// <item>Move the entity metadata rendering to before the headbar rendering, so that the headbar appears in front.</item>
    /// </list>
    /// </summary>
    /// <param name="ilctx">Context from which an <see cref="ILCursor"><c>ILCursor</c></see> can be created to modify the IL with.</param>
    public static void EnableIL_MapOrigRender(ILContext ilctx) {
        ILCursor ilcur = new(ilctx);

        //assumptions:
        // - the first LevelTemplate.RenderHighlight call is the one that highlights every selected or hovered room
        // - the first rectangle drawn in the last SpriteBatch is the headbar background
        //   (if someone wants to add another SpriteBatch after the last one, they can do it with an On hook instead, which they'd probably prefer)
        // - the check for Q being held is the condition for the if block where berry metadata is drawn, and this check is exactly 3 instructions long

        //find the hover/selection highlighter
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchCallOrCallvirt(typeof(LevelTemplate).GetMethod(nameof(LevelTemplate.RenderHighlight))));
        Instruction callRenderHighlight = ilcur.Next;
        ilcur.GotoPrev(MoveType.Before, instr => instr.MatchLdfld(typeof(MapEditor).GetField("hovered", BindingFlags.NonPublic | BindingFlags.Instance)));
        ilcur.GotoPrev(MoveType.Before, instr => instr.MatchLdarg0());
      #region room border overrides
        FieldInfo renderAsHoveredProperty = typeof(DebugMapHooks).GetField(nameof(DebugRenderRoomAsHovered));
        MethodInfo propValueSetter = typeof(GetterEventProperty<bool, DebugRoomArgs>).GetMethod("set_" + nameof(GetterEventProperty<bool, DebugRoomArgs>.Value));
        MethodInfo propValueGetter = typeof(GetterEventProperty<bool, DebugRoomArgs>).GetMethod("get_" + nameof(GetterEventProperty<bool, DebugRoomArgs>.Value));
        FieldInfo propEventArgField = typeof(GetterEventProperty<bool, DebugRoomArgs>).GetField(nameof(GetterEventProperty<bool, DebugRoomArgs>.Arguments));
        FieldInfo roomArgsMapField = typeof(DebugRoomArgs).GetField(nameof(DebugRoomArgs.DebugMap));
        FieldInfo roomArgsRoomField = typeof(DebugRoomArgs).GetField(nameof(DebugRoomArgs.Room));

        //set the property's value to the original value, so that'll be the initial value when the getter event runs
        ilcur.EmitLdsfld(renderAsHoveredProperty);
        ilcur.Goto(callRenderHighlight, MoveType.Before);
        ilcur.EmitCall(propValueSetter);

        //event args
        ilcur.EmitLdsfld(renderAsHoveredProperty);
        ilcur.EmitNewobj(typeof(DebugRoomArgs).GetConstructor([]));
        ilcur.EmitStfld(propEventArgField);
        //args.DebugMap
        ilcur.EmitLdsfld(renderAsHoveredProperty);
        ilcur.EmitLdfld(propEventArgField);
        ilcur.EmitLdarg(0);
        ilcur.EmitStfld(roomArgsMapField);
        //args.Room
        ilcur.EmitLdsfld(renderAsHoveredProperty);
        ilcur.EmitLdfld(propEventArgField);
        ilcur.EmitLdloc(8);
        ilcur.EmitStfld(roomArgsRoomField);

        //call the property's getter so the event runs and the result will be passed as the `hovered` argument to LevelTemplate.RenderHighlight
        ilcur.EmitLdsfld(renderAsHoveredProperty);
        ilcur.EmitCall(propValueGetter);
      #endregion

        //find the position just after the foreach block
        ilcur.GotoNext(MoveType.After, instr => instr.MatchEndfinally());
        ilcur.GotoNext(MoveType.After, instr => instr.MatchLdarg0());

      #region between rooms and cursor
        ilcur.EmitLdsfld(typeof(MapEditor).GetField("Camera", BindingFlags.NonPublic | BindingFlags.Static));
        ilcur.EmitCall(typeof(DebugMapHooks).GetMethod(nameof(RenderBetweenRoomsAndCursor)));
        ilcur.EmitLdarg0();
      #endregion

        //find the start of the last SpriteBatch
        ilcur.Index = ilctx.Instrs.Count;
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchCallOrCallvirt(typeof(SpriteBatch).GetMethod(nameof(SpriteBatch.Begin), [typeof(SpriteSortMode), typeof(BlendState), typeof(SamplerState), typeof(DepthStencilState), typeof(RasterizerState), typeof(Effect), typeof(Matrix)])));
        Instruction beginOverlaySpriteBatch = ilcur.Previous;

        //find where the headbar background is drawn
        ilcur.GotoNext(MoveType.After, instr => instr.MatchCallOrCallvirt(typeof(Draw).GetMethod(nameof(Draw.Rect), [typeof(float), typeof(float), typeof(float), typeof(float), typeof(Color)])));
        Instruction afterDrawHeadbarBack = ilcur.Next;

        //find where the berry metadata overlay is drawn
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchCallOrCallvirt(typeof(MInput).GetMethod("get_" + nameof(MInput.Keyboard))), instr => instr.MatchLdcI4((int)Keys.Q));
        Instruction beforeCheckItemMetadataBind = ilcur.Previous;
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchBrtrue(out _) || instr.MatchBrfalse(out _));
        ILLabel labelAfterMetadata = (ILLabel)ilcur.Next.Operand;
        
        //find the end of the last SpriteBatch
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchCallOrCallvirt(typeof(SpriteBatch).GetMethod(nameof(SpriteBatch.End))));
        Instruction endOverlaySpriteBatch = ilcur.Previous;

      #region replace vanilla fixed
      //call an event to draw custom fixed overlay elements, and skip all the vanilla ones
        ilcur.Goto(beginOverlaySpriteBatch, MoveType.After);
        ilcur.EmitDelegate(GraphViewer.RenderFixed);
        ilcur.EmitBr(endOverlaySpriteBatch);
      #endregion
    }

    public static void EnableIL_MapEverestRender(ILContext ilctx) {
        ILCursor ilcur = new(ilctx);

      #region skip Everest rendering
        MethodInfo renderKeys = typeof(MapEditor).GetMethod("RenderKeys", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo renderHighlightCurrent = typeof(MapEditor).GetMethod("RenderHighlightCurrentRoom", BindingFlags.NonPublic | BindingFlags.Instance);

        //branch past RenderHighlightCurrentRoom call
        ilcur.GotoNext(MoveType.After, instr => instr.MatchCallOrCallvirt(renderHighlightCurrent));
        Instruction afterHighlightCurrent = ilcur.Next;
        ilcur.GotoPrev(MoveType.Before, instr => instr.MatchLdarg0());
        ilcur.EmitBr(afterHighlightCurrent);

        //branch past RenderKeys call
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchCallOrCallvirt(renderKeys));
        Instruction afterRenderKeys = ilcur.Next;
        ilcur.GotoPrev(MoveType.Before, instr => instr.MatchLdarg0());
        ilcur.EmitBr(afterRenderKeys);

        //(they should be branched past separately in case another mod adds code in between them)
      #endregion
    }

    public static void EnableIL_MapEverestUpdate(ILContext ilctx) {
        ILCursor ilcur = new(ilctx);

        //assumptions:
        // - the second Input.ESC field access is where the press is consumed, and that's the first thing in the teleport back block
        // - the second Input.MenuConfirm field access is where the press is consumed, and that's the first thing in the teleport-to-hovered block

        //find where ESC/cancel teleports back to the current room...
        ilcur.GotoNext(MoveType.After, instr => instr.MatchLdsfld(typeof(Input).GetField(nameof(Input.ESC))));
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchLdsfld(typeof(Input).GetField(nameof(Input.ESC))));
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchBrtrue(out _) || instr.MatchBrfalse(out _));
        ILLabel afterCancelBlock = (ILLabel)ilcur.Previous.Operand;
        //...and add an event to the check
        ilcur.EmitLdsfld(typeof(DebugMapHooks).GetField(nameof(CancelToTPBackEnabled)));
        ilcur.EmitCall(typeof(GetterEventProperty<bool>).GetMethod("get_" + nameof(GetterEventProperty<bool>.Value)));
        ilcur.EmitBrfalse(afterCancelBlock);

        //find where confirm teleports to the hovered room...
        ilcur.GotoNext(MoveType.After, instr => instr.MatchLdsfld(typeof(Input).GetField(nameof(Input.MenuConfirm))));
        ilcur.GotoNext(MoveType.Before, instr => instr.MatchLdsfld(typeof(Input).GetField(nameof(Input.MenuConfirm))));
        ilcur.GotoPrev(MoveType.After, instr => instr.MatchBrtrue(out _) || instr.MatchBrfalse(out _));
        ILLabel afterConfirmBlock = (ILLabel)ilcur.Previous.Operand;
        //...and add an event to the check
        ilcur.EmitLdsfld(typeof(DebugMapHooks).GetField(nameof(ConfirmToTPHoverEnabled)));
        ilcur.EmitCall(typeof(GetterEventProperty<bool>).GetMethod("get_" + nameof(GetterEventProperty<bool>.Value)));
        ilcur.EmitBrfalse(afterConfirmBlock);
    }

    public static void EnableAll() {
        ILSceneOrigUpdate = new(typeof(MapEditor).GetMethod("orig_Update"), EnableIL_MapOrigUpdate);
        ILSceneOrigRender = new(typeof(MapEditor).GetMethod("orig_Render"), EnableIL_MapOrigRender);
        IL.Celeste.Editor.MapEditor.MakeMapEditorBetter += EnableIL_MapEverestUpdate;
        IL.Celeste.Editor.MapEditor.Render += EnableIL_MapEverestRender;
        IL.Celeste.Editor.LevelTemplate.ctor_LevelData += EnableIL_RoomCtor;
        On.Celeste.Editor.MapEditor.ctor += EnableOn_MapCtor;
        On.Celeste.Editor.MapEditor.Update += OnMapUpdate;
        On.Celeste.Editor.MapEditor.Render += OnMapRender;
    }

    public static void DisableAll() {
        ILSceneOrigUpdate.Dispose();
        ILSceneOrigRender.Dispose();
        IL.Celeste.Editor.MapEditor.MakeMapEditorBetter -= EnableIL_MapEverestUpdate;
        IL.Celeste.Editor.MapEditor.Render -= EnableIL_MapEverestRender;
        IL.Celeste.Editor.LevelTemplate.ctor_LevelData -= EnableIL_RoomCtor;
        On.Celeste.Editor.MapEditor.ctor -= EnableOn_MapCtor;
        On.Celeste.Editor.MapEditor.Update -= OnMapUpdate;
        On.Celeste.Editor.MapEditor.Render -= OnMapRender;
    }
}