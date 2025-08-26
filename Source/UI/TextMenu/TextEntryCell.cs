using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

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
        /// Whether the user is able to press Shift + Enter to insert newline characters into the text.
        /// </summary>
        public bool AllowNewline = false;

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
            /// Whether to stop checking for input matches after this event subscriber returns.
            /// </summary>
            public bool Break = false;
        }

        /// <param name="textEntry">The <see cref="TextEntry"/> object of which this event is running.</param>
        /// <param name="evInstr"><inheritdoc cref="InputEventInstructions"/></param>
        public delegate void InputEventSubscriber(TextEntry textEntry, InputEventInstructions evInstr);

        /// <summary>
        /// Each key is a function that receives the detected character and returns a Boolean.<br/>
        /// Each value is a function to be called if the key returns true. The function receives this <see cref="TextEntry"/> and the detected character.<br/>
        /// If a value function performs an action (rather than just checking if it should perform an action),
        /// it usually should set <see cref="InputEventInstructions.Break"/> to true.
        /// </summary>
        public Dictionary<Func<char, bool>, Action<TextEntry, char, InputEventInstructions>> OnReceiveChar = [];
        /// <summary>
        /// Each key is a function that returns a Boolean, intended to check whether the user has pressed certain key(s).
        /// Each value is a function to be called whenever the key returns true.<br/>
        /// If a value function performs an action (rather than just checking if it should perform an action) based on
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
            /// <summary>
            /// Default color of the caret in every text entry cell.
            /// </summary>
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

        /// <summary>
        /// Text inputs received this frame. Recorded by <see cref="RecordTextInput"/> and processed by <see cref="Update"/>.
        /// </summary>
        public Queue<char> TextInputsThisFrame = [];

        ~TextEntry() {
            if (Typing) { LoseFocus(); }
        }

        /// <summary>
        /// Record the given character into <see cref="TextInputsThisFrame"/>. Characters are recorded, rather than
        /// acted upon immediately, so that <see cref="Update"/> can give inputs a chance to take priority over characters.
        /// </summary>
        public void RecordTextInput(char ch) {
            TextInputsThisFrame.Enqueue(ch);
        }

        /// <summary>
        /// Called when the user starts typing in this text entry.
        /// </summary>
        public void GainFocus() {
            Typing = true;
            if (Container != null) {
                Container.Focused = false;
                Container.RenderAsFocused = true;
            }
            TextInput.OnInput += RecordTextInput;
        }

        /// <summary>
        /// Called when the user finishes typing in this text entry.
        /// </summary>
        public void LoseFocus() {
            TextInput.OnInput -= RecordTextInput;
            Typing = false;
            if (Container != null) {
                Container.Focused = true;
                Container.RenderAsFocused = false;
            }
        }

        /// <summary>
        /// Default check for whether a given character is able to be inserted into the text.
        /// </summary>
        public bool DefaultInsertCheck(char ch) => TextElement.Font.Characters.ContainsKey(ch);

        /// <summary>
        /// If any text is selected, replace it with the given character. Otherwise, insert the given character at the caret's current position.
        /// </summary>
        public void InsertAtCaret(char ch) {
            int insertStart;
            int insertEnd;
            if (Caret.Anchor < Caret.Focus) {
                insertStart = Caret.Anchor;
                insertEnd = Caret.Focus;
            } else {
                insertStart = Caret.Focus;
                insertEnd = Caret.Anchor;
            }
            var existingText = TextElement.Text;
            TextElement.Text = existingText[..insertStart] + ch + existingText[insertEnd..];
        }

        public override void Update() {
            foreach (var inputListener in OnReceiveInput) {
                if (inputListener.Key?.Invoke() ?? false) {
                    var evInstr = new InputEventInstructions();
                    inputListener.Value?.Invoke(this, evInstr);
                    if (evInstr.Break) { break; }
                }
            }

            while (TextInputsThisFrame.TryDequeue(out var ch)) {
                foreach (var charListener in OnReceiveChar) {
                    if (charListener.Key?.Invoke(ch) ?? false) {
                        var evInstr = new InputEventInstructions();
                        charListener.Value?.Invoke(this, ch, evInstr);
                        if (evInstr.Break) {
                            TextInputsThisFrame.Clear();
                            return;
                        }
                    }
                }
            }
        }

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
                        SelectElement.Justify.Y = JustifyY ?? TextElement.Justify.Y;
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
                        if (firstMeasure.Line == secondMeasure.Line) {
                            //all on one line
                            SelectElement.Position.X = position.X + firstMeasure.Offset.X;
                            SelectElement.Position.Y = position.Y + firstMeasure.Offset.Y;
                            SelectElement.ScaleToFit(secondMeasure.Offset.X - firstMeasure.Offset.X, TextElement.Font.LineHeight);
                            SelectElement.Render();
                        } else {
                            //first line
                            SelectElement.Position.X = position.X + firstMeasure.Offset.X;
                            SelectElement.Position.Y = position.Y + firstMeasure.Offset.Y;
                            SelectElement.ScaleToFit(firstMeasure.Line.RightOffset - firstMeasure.Offset.X, TextElement.Font.LineHeight);
                            SelectElement.Render();
                            //lines between first and last (if any)
                            var lineIndex = firstMeasure.Line.Index + 1;
                            while (lineIndex < secondMeasure.Line.Index) {
                                var line = TextElement.Measurements.Lines[lineIndex];
                                SelectElement.Position.X = position.X + line.LeftOffset;
                                SelectElement.Position.Y = position.Y - TextElement.Measurements.Height * TextElement.Justify.Y + line.Index * TextElement.Font.LineHeight;
                                SelectElement.ScaleToFit(line.RightOffset - line.LeftOffset, TextElement.Font.LineHeight);
                                SelectElement.Render();
                            }
                            //last line
                            SelectElement.Position.X = position.X + secondMeasure.Line.LeftOffset;
                            SelectElement.Position.Y = position.Y + secondMeasure.Offset.Y;
                            SelectElement.ScaleToFit(secondMeasure.Offset.X - secondMeasure.Line.LeftOffset, TextElement.Font.LineHeight);
                            SelectElement.Render();
                        }
                    }
                }
            }
            if (Typing) {
                var origColor = Caret.TextureElement.Color;
                if (Engine.Scene.BetweenInterval(Caret.BlinkInterval)) {
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