using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.MacroRoutingTool.UI;

partial class TableMenu {
    /// <summary>
    /// A table cell in which users can input text. Loosely based on
    /// <seealso href="https://github.com/EverestAPI/Everest/blob/8c5add9e2c4b5478c9b9e559b9af080b24288863/Celeste.Mod.mm/Mod/UI/TextMenuExt.cs#L1481">Everest's TextBox class</seealso>.
    /// </summary>
    public class TextEntry : CellItem {
        /// <summary>
        /// Whether this text input is currently being typed in.
        /// </summary>
        public bool Typing = false;

        /// <summary>
        /// The texture drawn as the background of this text input field.
        /// </summary>
        public TextureElement BackElement = new() { Texture = UIHelpers.Square, Color = Color.DarkSlateGray * 0.8f };
        /// <summary>
        /// The texture drawn over the selected text of this text input field, when there is any selected text.
        /// </summary>
        public TextureElement SelectElement = new() { Texture = UIHelpers.Square, Color = HighlightColorA * 0.4f, Justify = new(0f, 0.5f) };
        /// <summary>
        /// The element that contains and renders this input field's current text.
        /// </summary>
        public TextElement TextElement = new() { Justify = new(0f, 0.5f), BorderThickness = 2f, MaxHeight = ActiveFont.LineHeight };
        /// <summary>
        /// The element that contains and renders this input field's placeholder text, which only appears when the current text is empty.
        /// </summary>
        public TextElement PlaceholderElement = new() { Color = Color.LightGray * 0.75f, Justify = new(0f, 0.5f), BorderThickness = 2f, MaxHeight = ActiveFont.LineHeight };

        /// <summary>
        /// Instructions that control the execution and results of a <see cref="TextEntry"/>'s subscription to <see cref="TextInput.OnInput"/>.
        /// </summary>
        public class InputEventInstructions {
            /// <summary>
            /// Whether to consume the input received this frame. Consuming inputs from <see cref="TextInput.OnInput"/> prevents them from
            /// causing menu-related actions to be performed even if bound to those actions.
            /// </summary>
            /// <remarks>
            /// For example, the default <see cref="Input.MenuCancel"/> keyboard bind is X. With that bind, if the default <see cref="OnReceiveInput"/>
            /// event for Ctrl+X didn't set this field to true, pressing Ctrl+X would close the menu immediately after cutting the selected text.
            /// </remarks>
            public bool ConsumeInput = false;
            /// <summary>
            /// Whether to stop checking for input matches after this event subscriber returns.
            /// </summary>
            public bool Break = false;
        }

        /// <param name="textEntry">The <see cref="TextEntry"/> object of which this event is running.</param>
        /// <param name="evInstr"><inheritdoc cref="InputEventInstructions"/></param>
        public delegate void InputEventSubscriber(TextEntry textEntry, InputEventInstructions evInstr);

        /// <summary>
        /// Each key is a character, and each value is a function to be called when that character is received.<br/>
        /// If a value function performs an action (rather than just checking if it should perform an action), it should set
        /// its received <see cref="InputEventInstructions.ConsumeInput"/> (and usually <see cref="InputEventInstructions.Break"/>) to true.
        /// </summary>
        public Dictionary<char, InputEventSubscriber> OnReceiveChar = [];
        /// <summary>
        /// Each key is a function that returns a Boolean, intended to check whether the user has pressed certain key(s).
        /// Each value is a function to be called whenever the key returns true.<br/>
        /// If a value function performs an action (rather than just checking if it should perform an action), it should set
        /// its received <see cref="InputEventInstructions.ConsumeInput"/> to true. If the action was performed based on
        /// a non-modifier key, it usually should set <see cref="InputEventInstructions.Break"/> to true.
        /// </summary>
        public Dictionary<Func<bool>, InputEventSubscriber> OnReceiveInput = [];

        /// <summary>
        /// A <see cref="TextEntry"/>'s inner margins, which reserve a minimum space between the element's edges and its text's edges.
        /// </summary>
        public class InnerMarginSet {
            /// <summary>
            /// Minimum distance between this element's top edge and its text's top, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.
            /// </summary>
            public float Top = 4f;
            /// <summary>
            /// Minimum distance between this element's bottom edge and its text's bottom edge, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.
            /// </summary>
            public float Bottom = 4f;
            /// <summary>
            /// Minimum distance between this element's left edge and its text's left edge, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.
            /// </summary>
            public float Left = 12f;
            /// <summary>
            /// Minimum distance between this element's right edge and its text's right edge, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.
            /// </summary>
            public float Right = 12f;
        }

        /// <summary>
        /// This <see cref="TextEntry"/>'s inner margins, which reserve a minimum space between this element's edges and its text's edges.
        /// </summary>
        public InnerMarginSet InnerMargins = new();

        /// <summary>
        /// If true, this item's <see cref="BackElement"/> will automatically have its border set to the table's
        /// <see cref="TextMenu.HighlightColor"/> while hovered and hidden while not hovered.
        /// </summary>
        public bool AutoHoverBorder = true;

        /// <summary>
        /// A <see cref="TextEntry"/>'s <seealso href="https://en.wikipedia.org/wiki/Caret_navigation">caret</seealso>. 
        /// </summary>
        public class TextCaret {
            public static Color DefaultColor = HighlightColorB * 0.9f;

            /// <summary>
            /// The element that contains and renders this caret's texture. 
            /// </summary>
            public TextureElement TextureElement = new() { Texture = UIHelpers.Square, Color = DefaultColor, Justify = new(0.5f, 0f), Scale = new(3f / UIHelpers.Square.Width, 1f), BorderThickness = 1f };

            /// <summary>
            /// Time between each change in this caret's visibility.
            /// </summary>
            public float BlinkInterval = 0.6f;

            /// <summary>
            /// Opacity of this caret during blinks. 0 is invisible, 1 is opaque.
            /// </summary>
            public float BlinkAlpha = 0.4f;

            /// <summary>
            /// Index in the original text of the position at which this range was initiated.
            /// </summary>
            public int Anchor = 0;

            /// <summary>
            /// Index in the original text of the caret's current position.
            /// </summary>
            public int Focus = 0;

            /// <summary>
            /// Offset of this caret's <see cref="TextureElement"/> position relative to the text input's <see cref="TextElement"/>'s position. 
            /// </summary>
            public Vector2 Offset = Vector2.Zero;
        }

        /// <summary>
        /// This <see cref="TextEntry"/>'s <seealso href="https://en.wikipedia.org/wiki/Caret_navigation">caret</seealso>. 
        /// </summary>
        public TextCaret Caret = new();

        public override float UnrestrictedWidth() => float.MaxValue;
        public override float UnrestrictedHeight() => float.MaxValue;

        public override void Render(Vector2 position, bool highlighted) {
            base.Render(position, highlighted);

            var width = LeftWidth();
            var height = Height();
            var textWidth = width - InnerMargins.Left - InnerMargins.Right;
            var textHeight = height - InnerMargins.Top - InnerMargins.Bottom;

            BackElement.Position.X = position.X;
            BackElement.Position.Y = position.Y;
            BackElement.Justify.X = JustifyX ?? BackElement.Justify.X;
            BackElement.Justify.Y = JustifyY ?? BackElement.Justify.Y;
            BackElement.ScaleToFit(width, height);
            var ogBackBorderThickness = BackElement.BorderThickness;
            if (AutoHoverBorder) {
                BackElement.BorderColor = Container.HighlightColor;
                if (highlighted) { BackElement.BorderThickness = 0f; }
            }
            BackElement.Render();
            BackElement.BorderThickness = ogBackBorderThickness;
            if (string.IsNullOrEmpty(TextElement.Text)) {
                PlaceholderElement.Position.X = position.X;
                PlaceholderElement.Position.Y = position.Y;
                PlaceholderElement.Justify.X = JustifyX ?? PlaceholderElement.Justify.X;
                PlaceholderElement.Justify.Y = JustifyY ?? PlaceholderElement.Justify.Y;
                PlaceholderElement.MaxWidth = textWidth;
                PlaceholderElement.MaxHeight = textHeight;
                PlaceholderElement.Render();
                if (Typing) {
                    Caret.TextureElement.Position.X = 0f;
                    Caret.TextureElement.Position.Y = TextElement.Font.LineHeight * (JustifyY ?? TextElement.Justify.Y);
                }
            } else {
                TextElement.Position.X = position.X;
                TextElement.Position.Y = position.Y;
                TextElement.Justify.X = JustifyX ?? TextElement.Justify.X;
                TextElement.Justify.Y = JustifyY ?? TextElement.Justify.Y;
                TextElement.MaxWidth = textWidth;
                TextElement.MaxHeight = textHeight;
                TextElement.Render();
                if (Typing) {
                    var focusMeasure = TextElement.MeasureChar(Caret.Focus);
                    Caret.Offset = focusMeasure.Offset;
                    if (Caret.Anchor != Caret.Focus) {
                        //selected text
                        var anchorMeasure = TextElement.MeasureChar(Caret.Anchor);
                        TextElement.CharMeasurements firstMeasure;
                        TextElement.CharMeasurements secondMeasure;
                        if (anchorMeasure.Index < focusMeasure.Index) {
                            firstMeasure = anchorMeasure;
                            secondMeasure = focusMeasure;
                        } else {
                            firstMeasure = focusMeasure;
                            secondMeasure = anchorMeasure;
                        }
                        SelectElement.Position.X = position.X + firstMeasure.Offset.X;
                        SelectElement.Position.Y = position.Y + firstMeasure.Offset.Y;
                        SelectElement.Justify.Y = JustifyY ?? TextElement.Justify.Y;
                        var line = firstMeasure.Line;
                        while (line <= secondMeasure.Line) {
                            
                        }
                        if (anchorMeasure.Line == focusMeasure.Line) {
                            //all on one line
                            SelectElement.Position.X = position.X + firstMeasure.Offset.X;
                            SelectElement.Position.Y = position.Y + firstMeasure.Offset.Y;
                            SelectElement.Justify.Y = JustifyY ?? TextElement.Justify.Y;
                            SelectElement.ScaleToFit(secondMeasure.Offset.X - firstMeasure.Offset.X, TextElement.Font.LineHeight);
                            SelectElement.Render();
                        } else {
                            //multiple lines
                        }
                    }
                }
            }
            if (Typing) {
                var origColor = Caret.TextureElement.Color;
                if (Monocle.Engine.Scene.BetweenInterval(Caret.BlinkInterval)) {
                    Caret.TextureElement.Color *= Caret.BlinkAlpha;
                }
                Caret.TextureElement.Position.X += Caret.Offset.X;
                Caret.TextureElement.Position.Y += Caret.Offset.Y;
                Caret.TextureElement.Justify.Y = JustifyY ?? TextElement.Justify.Y;
                Caret.TextureElement.ScaleYToFit(textHeight);
                Caret.TextureElement.Render();
                Caret.TextureElement.Color = origColor;
            }
        }
    }
}