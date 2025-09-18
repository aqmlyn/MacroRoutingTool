using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

partial class TableMenu {
    /// <summary>
    /// A table cell in which users can input text. Loosely based on
    /// <seealso href="https://github.com/EverestAPI/Everest/blob/8c5add9e2c4b5478c9b9e559b9af080b24288863/Celeste.Mod.mm/Mod/UI/TextMenuExt.cs#L1481">Everest's TextBox class</seealso>,
    /// with many additional features such as a caret and clipboard access.
    /// </summary>
    public class TextEntry : CellItem {
        /// <summary>
        /// List of keys that, when held, cause other key presses to be interpreted as sending commands rather than characters.
        /// </summary>
        public static List<Keys> ControlKeys = [Keys.LeftControl, Keys.LeftAlt, Keys.LeftWindows, Keys.RightControl, Keys.RightAlt, Keys.RightWindows];

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
            /// Whether to stop checking for input matches after this function returns.
            /// </summary>
            public bool Break = false;

            /// <summary>
            /// Whether this input is invalid in a way that doesn't demand immediate attention.
            /// </summary>
            public bool InvalidMinor = false;

            /// <summary>
            /// Whether this input is invalid in a way that demands immediate attention.
            /// <see cref="NotifyInvalid"/> will be called to get the user's attention.
            /// </summary>
            public bool InvalidMajor = false;
        }

        /// <param name="textEntry">The <see cref="TextEntry"/> object of which this event is running.</param>
        /// <param name="receivedChar">The character received from the system keyboard input.</param>
        /// <returns>Whether to perform the action associated with this check.</returns>
        public delegate bool CharEventCheck(TextEntry textEntry, char receivedChar);
        /// <param name="textEntry">The <see cref="TextEntry"/> object of which this event is running.</param>
        /// <returns>Whether to perform the action associated with this check.</returns>
        public delegate bool InputEventCheck(TextEntry textEntry);
        /// <param name="textEntry">The <see cref="TextEntry"/> object of which this event is running.</param>
        /// <param name="evInstr"><inheritdoc cref="InputEventInstructions"/></param>
        public delegate void InputEventSubscriber(TextEntry textEntry, InputEventInstructions evInstr);
        /// <param name="textEntry">The <see cref="TextEntry"/> object of which this event is running.</param>
        /// <param name="receivedChar">The character received from the system keyboard input.</param>
        /// <param name="evInstr"><inheritdoc cref="InputEventInstructions"/></param>
        public delegate void CharEventSubscriber(TextEntry textEntry, char receivedChar, InputEventInstructions evInstr);

        /// <summary>
        /// Each key is a function that receives the detected character and returns a Boolean.<br/>
        /// Each value is a function to be called if the key returns true. The function receives this <see cref="TextEntry"/> and the detected character.<br/>
        /// If a value function performs an action (rather than just checking if it should perform an action),
        /// it usually should set <see cref="InputEventInstructions.Break"/> to true.
        /// </summary>
        public Dictionary<CharEventCheck, CharEventSubscriber> OnReceiveChar = new(){
            {EscapeCheck, Exit},
            {CutCheck, Cut},
            {CopyCheck, Copy},
            {PasteCheck, Paste},
            {SelectAllCheck, SelectAll},
            {UpdateCaretMovementOptionCheck, UpdateCaretMovementOptions},
            {MoveCaretUpCheck, MoveCaretUp},
            {MoveCaretDownCheck, MoveCaretDown},
            {MoveCaretLeftCheck, MoveCaretLeft},
            {MoveCaretRightCheck, MoveCaretRight},
            {MoveCaretLineStartCheck, MoveCaretLineStart},
            {MoveCaretLineEndCheck, MoveCaretLineEnd},
            {BackspaceCheck, Backspace},
            {DeleteCheck, Delete},
            {DefaultInsertCheck, TypeAtCaret}
        };

        /// <summary>
        /// Character inputs received this frame. Recorded by <see cref="RecordCharInput"/> (which is subscribed to
        /// <see cref="TextInput.OnInput"/> while <see cref="Typing"/>) and processed by <see cref="Update"/>.
        /// </summary>
        public Queue<char> CharsReceivedThisFrame = [];

        /// <summary>
        /// The function used by this <see cref="TextEntry"/> to determine whether the character at a given index
        /// is part of the separation between two words.
        /// </summary>
        public Func<TextEntry, int, bool> WordSeparationCheck = DefaultWordSeparationCheck;

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
        /// Default color of the caret in every text entry cell.
        /// </summary>
        public static Color CaretDefaultColor = HighlightColorB * 0.9f;

        /// <summary>
        /// The element that contains and renders this caret's texture. 
        /// </summary>
        public TextureElement CaretElement = new() { Texture = UIHelpers.Square, Color = CaretDefaultColor, Justify = new(0.5f, 0f), Scale = new(3f / UIHelpers.Square.Width, 1f), BorderThickness = 1f };

        /// <summary>
        /// Time between each change in this caret's visibility.
        /// </summary>
        public float CaretBlinkInterval = 0.6f;

        /// <summary>
        /// Opacity of this caret during blinks. 0 is invisible, 1 is opaque.
        /// </summary>
        public float CaretBlinkAlpha = 0.4f;

        /// <summary>
        /// Backing field for <see cref="CaretAnchor"/>.
        /// </summary>
        public int _caretAnchor = 0;

        /// <summary>
        /// If any text is selected, this is the index in the original text at which the selection was initiated.
        /// Otherwise, this is equal to <see cref="Focus"/>.
        /// </summary>
        public int CaretAnchor {
            get => _caretAnchor;
            set {
                CaretAnchorNeedsRemeasured |= _caretAnchor != value;
                _caretAnchor = value;
            }
        }

        /// <summary>
        /// Backing field for <see cref="CaretAnchorMeasure"/>.
        /// </summary>
        public TextElement.CharMeasurements _caretAnchorOffset = null;

        /// <summary>
        /// Offset, relative to relative to <see cref="TextElement"/>'s position, of the selection end opposite from the caret.
        /// </summary>
        public TextElement.CharMeasurements CaretAnchorMeasure {
            get {
                if (CaretAnchorNeedsRemeasured) { RemeasureCaretAnchor(); }
                return _caretAnchorOffset;
            }
            set { _caretAnchorOffset = value; }
        }

        /// <summary>
        /// Whether <see cref="CaretAnchorMeasure"/> needs to be remeasured before it's used in rendering.
        /// </summary>
        public bool CaretAnchorNeedsRemeasured = false;

        /// <summary>
        /// Backing field for <see cref="CaretFocus"/>.
        /// </summary>
        public int _caretFocus = 0;

        /// <summary>
        /// Index in the original text of the caret's current position.
        /// </summary>
        public int CaretFocus {
            get => _caretFocus;
            set {
                CaretFocusNeedsRemeasured |= _caretFocus != value;
                _caretFocus = value;
            }
        }
        
        /// <summary>
        /// Backing field for <see cref="CaretFocusMeasure"/>.
        /// </summary>
        public TextElement.CharMeasurements _caretFocusOffset = null;

        /// <summary>
        /// Offset, relative to <see cref="TextElement"/>'s position, of the caret's <see cref="TextureElement"/> position. 
        /// </summary>
        public TextElement.CharMeasurements CaretFocusMeasure {
            get {
                if (CaretFocusNeedsRemeasured) { RemeasureCaretFocus(); }
                return _caretFocusOffset;
            }
            set { _caretFocusOffset = value; }
        }

        /// <summary>
        /// Whether <see cref="CaretFocusMeasure"/> needs to be remeasured before it's used in rendering.
        /// </summary>
        public bool CaretFocusNeedsRemeasured = false;

        /// <summary>
        /// Target horizontal offset to place <see cref="Focus"/> as close as possible to when moving it vertically.
        /// </summary>
        public float CaretTargetX = 0f;

        /// <summary>
        /// Remeasure <see cref="CaretAnchorMeasure"/> so that it will be drawn in the correct position when rendering.
        /// </summary>
        public void RemeasureCaretAnchor() {
            CaretAnchorNeedsRemeasured = false;
            _caretAnchorOffset = TextElement.MeasureChar(CaretAnchor);
        }

        /// <summary>
        /// Remeasure <see cref="CaretFocusMeasure"/> so that it will be drawn in the correct position when rendering.
        /// </summary>
        public void RemeasureCaretFocus() {
            CaretFocusNeedsRemeasured = false;
            _caretFocusOffset = TextElement.MeasureChar(CaretFocus);
        }

        /// <summary>
        /// Assign the indices <see cref="CaretAnchor"/> and <see cref="CaretFocus"/> in their current chronological order to the given out variables.
        /// </summary>
        /// <param name="first">The index that comes first.</param>
        /// <param name="second">The index that comes second.</param>
        public void OrderCaretIndices(out int first, out int second) {
            if (CaretAnchor < CaretFocus) {
                first = CaretAnchor;
                second = CaretFocus;
            } else {
                first = CaretFocus;
                second = CaretAnchor;
            }
        }

        /// <summary>
        /// Assign <see cref="CaretAnchorMeasure"/> and <see cref="CaretFocusMeasure"/> in their current chronological order to the given out variables.
        /// </summary>
        /// <param name="first">The measure that comes first.</param>
        /// <param name="second">The measure that comes second.</param>
        public void OrderCaretMeasurements(out TextElement.CharMeasurements first, out TextElement.CharMeasurements second) {
            if (CaretAnchor < CaretFocus) {
                first = CaretAnchorMeasure;
                second = CaretFocusMeasure;
            } else {
                first = CaretFocusMeasure;
                second = CaretAnchorMeasure;
            }
        }

        ~TextEntry() {
            if (Typing) { LoseFocus(); }
        }

        /// <summary>
        /// Record the given character into <see cref="CharsReceivedThisFrame"/>. Characters are recorded, rather than
        /// acted upon immediately, so that <see cref="Update"/> can give inputs a chance to take priority over characters.
        /// </summary>
        public void RecordCharInput(char ch) {
            CharsReceivedThisFrame.Enqueue(ch);
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
            TextInput.OnInput += RecordCharInput;
        }

        /// <summary>
        /// Called when the user finishes typing in this text entry.
        /// </summary>
        public void LoseFocus() {
            TextInput.OnInput -= RecordCharInput;
            Typing = false;
            if (Container != null) {
                Container.Focused = true;
                Container.RenderAsFocused = false;
            }
        }

        /// <summary>
        /// Consume all <see cref="VirtualButton"/> inputs in <see cref="MInput.VirtualInputs"/>.<br/>
        /// Performed when an event action sets its <see cref="InputEventInstructions.Break"/> to true in order to
        /// prevent globally active input checks, such as CelesteTAS Start/Stop TAS, from conflicting with typing.
        /// </summary>
        public static void ConsumeGameInput() {
            foreach (var input in MInput.VirtualInputs) {
                if (input is VirtualButton button) {
                    button.ConsumePress();
                }
            }
        }

        /// <summary>
        /// Notify the user that their input was not processed as they likely intended.
        /// </summary>
        public static void NotifyInvalid() {
            //TODO also give visual feedback
            Audio.Play(SFX.ui_main_button_invalid);
        }

        /// <summary>
        /// Default check for whether the character at the given index is part of the separation between two words.
        /// </summary>
        public static bool DefaultWordSeparationCheck(TextEntry textEntry, int index) {
            return char.IsWhiteSpace(textEntry.TextElement.Text[index]);
        }

        /// <summary>
        /// Default check for whether a given character is able to be inserted into the text.
        /// </summary>
        public static bool DefaultInsertCheck(TextEntry textEntry, char ch) {
            ArgumentNullException.ThrowIfNull(textEntry);
            return textEntry.TextElement.Font.Characters.ContainsKey(ch) && !ControlKeys.Any(MInput.Keyboard.Check);
        }

        /// <summary>
        /// If any text is selected, replace it with the given string. Otherwise, insert the given string at the caret's current position.
        /// </summary>
        public void InsertAtCaret(string str) {
            OrderCaretIndices(out var insertStart, out var insertEnd);
            var existingText = TextElement.Text;
            var newText = existingText[..insertStart] + str;
            if (insertEnd < existingText.Length) { newText += existingText[insertEnd..]; }
            TextElement.Text = newText;
            CaretAnchor = CaretFocus = insertStart + str.Length;
            CaretTargetX = CaretFocusMeasure.Offset.X;
        }

        /// <summary>
        /// If any text is selected, replace it with the given character. Otherwise, insert the given character at the caret's current position.
        /// </summary>
        public void InsertAtCaret(char str) => InsertAtCaret(str.ToString());

        /// <summary>
        /// If any text is selected, replace it with the given character. Otherwise, insert the given character at the caret's current position.
        /// </summary>
        public static void TypeAtCaret(TextEntry textEntry, char ch, InputEventInstructions evInstr) {
            evInstr.Break = true;
            ArgumentNullException.ThrowIfNull(textEntry);
            textEntry.InsertAtCaret(ch);
        }

        /// <summary>
        /// Default check for cutting text, which checks the keyboard shortcut Ctrl+X.
        /// </summary>
        public static bool CutCheck(TextEntry _, char __) => (MInput.Keyboard.Check(Keys.LeftControl) || MInput.Keyboard.Check(Keys.RightControl)) && MInput.Keyboard.Pressed(Keys.X);
        /// <summary>
        /// Default check for copying text, which checks the keyboard shortcut Ctrl+C.
        /// </summary>
        public static bool CopyCheck(TextEntry _, char __) => (MInput.Keyboard.Check(Keys.LeftControl) || MInput.Keyboard.Check(Keys.RightControl)) && MInput.Keyboard.Pressed(Keys.C);
        /// <summary>
        /// Default check for pasting text, which checks the keyboard shortcut Ctrl+V.
        /// </summary>
        public static bool PasteCheck(TextEntry _, char __) => (MInput.Keyboard.Check(Keys.LeftControl) || MInput.Keyboard.Check(Keys.RightControl)) && MInput.Keyboard.Pressed(Keys.V);
        /// <summary>
        /// Default check for selecting all text, which checks the keyboard shortcut Ctrl+A.
        /// </summary>
        public static bool SelectAllCheck(TextEntry _, char __) => (MInput.Keyboard.Check(Keys.LeftControl) || MInput.Keyboard.Check(Keys.RightControl)) && MInput.Keyboard.Pressed(Keys.A);

        /// <summary>
        /// Copy the given <see cref="TextEntry"/>'s selected text to the system clipboard, then delete that text from the <see cref="TextEntry"/>.<br/>
        /// Copying uses <seealso href="https://github.com/EverestAPI/Everest/blob/fd05ec3e0403f0e0a425ca2e21763b09dedd3cfa/Celeste.Mod.mm/Mod/Everest/TextInput.cs#L75">Everest's TextInput.SetClipboardText</seealso>,
        /// which currently uses <seealso href="https://wiki.libsdl.org/SDL2/FrontPage">SDL2</seealso>.
        /// </summary>
        public static void Cut(TextEntry textEntry, char _, InputEventInstructions evInstr) {
            evInstr.Break = true;

            Copy(textEntry, _, evInstr);
            if (evInstr.InvalidMinor) { return; }
            textEntry.InsertAtCaret("");
        }
        /// <summary>
        /// Copy the given <see cref="TextEntry"/>'s selected text to the system clipboard.<br/>
        /// Uses <seealso href="https://github.com/EverestAPI/Everest/blob/fd05ec3e0403f0e0a425ca2e21763b09dedd3cfa/Celeste.Mod.mm/Mod/Everest/TextInput.cs#L75">Everest's TextInput.SetClipboardText</seealso>,
        /// which currently uses <seealso href="https://wiki.libsdl.org/SDL2/FrontPage">SDL2</seealso>.
        /// </summary>
        public static void Copy(TextEntry textEntry, char _, InputEventInstructions evInstr) {
            evInstr.Break = true;

            if (textEntry.CaretAnchor == textEntry.CaretFocus) {
                evInstr.InvalidMinor = true;
                return;
            }

            textEntry.OrderCaretIndices(out var selStart, out var selEnd);
            var selText = textEntry.TextElement.Text[selStart..(selEnd - selStart)];
            TextInput.SetClipboardText(selText);
        }
        /// <summary>
        /// If the system clipboard contains text, pastes it in the given <see cref="TextEntry"/>
        /// at its caret's current position, replacing selected text if there is any.<br/>
        /// Uses <seealso href="https://github.com/EverestAPI/Everest/blob/fd05ec3e0403f0e0a425ca2e21763b09dedd3cfa/Celeste.Mod.mm/Mod/Everest/TextInput.cs#L74">Everest's TextInput.GetClipboardText</seealso>,
        /// which currently uses <seealso href="https://wiki.libsdl.org/SDL2/FrontPage">SDL2</seealso>.
        /// </summary>
        public static void Paste(TextEntry textEntry, char _, InputEventInstructions evInstr) {
            evInstr.Break = true;

            var pasteText = TextInput.GetClipboardText();
            if (string.IsNullOrEmpty(pasteText)) {
                evInstr.InvalidMajor = true;
                return;
            }
            textEntry.InsertAtCaret(pasteText);
        }
        /// <summary>
        /// Selects all text in the given <see cref="TextEntry"/>.
        /// </summary>
        public static void SelectAll(TextEntry textEntry, char _, InputEventInstructions evInstr) {
            evInstr.Break = true;

            var text = textEntry.TextElement.Text;
            if (string.IsNullOrEmpty(text)) {
                evInstr.InvalidMajor = true;
                return;
            }
            textEntry.CaretAnchor = 0;
            textEntry.CaretFocus = text.Length;
        }

        /// <summary>
        /// Moves the caret to the specified index in the current text. If <paramref name="select"/> is true, leaves the anchor where it is in order to create a selection.
        /// </summary>
        /// <param name="idx">Index in the current text to move the caret to.</param>
        /// <param name="select">Whether to create a selection.</param>
        /// <exception cref="IndexOutOfRangeException">The received index was either negative or greater than the length of the current text.</exception>
        public void MoveCaretIndex(Index idx, bool select = false) {
            if (!idx.IsFromEnd && idx.Value < 0) { throw new IndexOutOfRangeException($"Received negative index: {idx.Value}"); }
            var text = TextElement.Text;
            var idxval = idx.GetOffset(text.Length);
            if (idxval > text.Length) { throw new IndexOutOfRangeException($"Index {idxval} (from {idx}) is greater than current length of text ({text.Length}): {text}"); }
            CaretFocus = idxval;
            if (!select) { CaretAnchor = idxval; }
        }

        /// <summary>
        /// Moves the caret to the specified index in the current text. If <paramref name="select"/> returns true, leaves the anchor where it is in order to create a selection.
        /// </summary>
        /// <param name="idx">Index in the current text to move the caret to.</param>
        /// <param name="select">A function that returns whether to create a selection.</param>
        /// <exception cref="IndexOutOfRangeException">The received index was either negative or greater than the length of the current text.</exception>
        public void MoveCaretIndex(Index idx, Func<bool> select) => MoveCaretIndex(idx, select?.Invoke() ?? false);

        /// <summary>
        /// Returns the closest index before <see cref="CaretFocus"/> at which <see cref="WordSeparationCheck"/>
        /// returns true, or 0 if there is no such index.
        /// </summary>
        public int IndexAtPreviousWord() {
            if (CaretFocus == 0) { return 0; }
            var idx = CaretFocus - 1;
            while (idx > 0 && WordSeparationCheck(this, idx)) { idx--; }
            while (idx > 0 && !WordSeparationCheck(this, idx)) { idx--; }
            return idx;
        }

        /// <summary>
        /// Returns the closest index after <see cref="CaretFocus"/> at which <see cref="WordSeparationCheck"/>
        /// returns true, or the length of the text if there is no such index.
        /// </summary>
        public int IndexAtNextWord() {
            var end = TextElement.Text.Length;
            if (CaretFocus == end) { return end; }
            var idx = CaretFocus + 1;
            while (idx < end && WordSeparationCheck(this, idx)) { idx++; }
            while (idx < end && !WordSeparationCheck(this, idx)) { idx++; }
            return idx;
        }

        /// <summary>
        /// Whether a single action that moves the caret horizontally should move by one word (true) or one character (false).
        /// </summary>
        public bool CaretMovementByWord = false;
        /// <summary>
        /// Whether a single action that moves <see cref="CaretFocus"/> should also move <see cref="CaretAnchor"/>. When those
        /// indices are not the same (allowed by setting this to false), the text in between them is considered selected.
        /// </summary>
        public bool CaretMovementSelect = false;

        /// <summary>
        /// If this <see cref="TextEntry"/> uses the <see cref="UpdateCaretMovementOptions"/> action in its <see cref="OnReceiveChar"/>,
        /// its <see cref="CaretMovementByWord"/> will be set to the return value of this function.
        /// </summary>
        public Func<TextEntry, char, bool> CaretMovementByWordCheck = DefaultCaretMovementByWordCheck;
        /// <summary>
        /// If this <see cref="TextEntry"/> uses the <see cref="UpdateCaretMovementOptions"/> action in its <see cref="OnReceiveChar"/>,
        /// its <see cref="CaretMovementSelect"/> will be set to the return value of this function.
        /// </summary>
        public Func<TextEntry, char, bool> CaretMovementSelectCheck = DefaultCaretMovementSelectCheck;

        /// <summary>
        /// Default check for enabling <see cref="CaretMovementByWord"/>, which checks for either Control key.
        /// </summary>
        public static bool DefaultCaretMovementByWordCheck(TextEntry _, char __) => MInput.Keyboard.Pressed(Keys.LeftControl) || MInput.Keyboard.Pressed(Keys.RightControl);
        /// <summary>
        /// Default check for enabling <see cref="CaretMovementSelect"/>, which checks for either Shift key.
        /// </summary>
        public static bool DefaultCaretMovementSelectCheck(TextEntry _, char __) => MInput.Keyboard.Pressed(Keys.LeftShift) || MInput.Keyboard.Pressed(Keys.RightShift);

        /// <summary>
        /// Default check associated with <see cref="UpdateCaretMovementOptions"/>, which always returns true,
        /// meaning that action will be called each frame.
        /// </summary>
        public static bool UpdateCaretMovementOptionCheck(TextEntry _, char __) => true;
        /// <summary>
        /// Calls <see cref="CaretMovementByWordCheck"/> and <see cref="CaretMovementSelectCheck"/> and updates
        /// <see cref="CaretMovementByWord"/> and <see cref="CaretMovementSelect"/> accordingly.
        /// </summary>
        public static void UpdateCaretMovementOptions(TextEntry textEntry, char ch, InputEventInstructions _) {
            textEntry.CaretMovementByWord = textEntry.CaretMovementByWordCheck?.Invoke(textEntry, ch) ?? false;
            textEntry.CaretMovementSelect = textEntry.CaretMovementSelectCheck?.Invoke(textEntry, ch) ?? false;
        }

        //TODO MoveCaret*Check: check for ANSI escape sequences
        /// <summary>
        /// Default check for moving the caret left, which checks the left arrow key.
        /// </summary>
        public static bool MoveCaretLeftCheck(TextEntry _, char __) => MInput.Keyboard.Pressed(Keys.Left);
        /// <summary>
        /// Default check for moving the caret right, which checks the right arrow key.
        /// </summary>
        public static bool MoveCaretRightCheck(TextEntry _, char __) => MInput.Keyboard.Pressed(Keys.Right);
        /// <summary>
        /// Default check for moving the caret up, which checks the up arrow key.
        /// </summary>
        public static bool MoveCaretUpCheck(TextEntry _, char __) => MInput.Keyboard.Pressed(Keys.Up);
        /// <summary>
        /// Default check for moving the caret down, which checks the down arrow key.
        /// </summary>
        public static bool MoveCaretDownCheck(TextEntry _, char __) => MInput.Keyboard.Pressed(Keys.Down);
        /// <summary>
        /// Default check for moving the caret to the start of the current line, which checks the Home key.
        /// </summary>
        public static bool MoveCaretLineStartCheck(TextEntry _, char __) => MInput.Keyboard.Pressed(Keys.Home);
        /// <summary>
        /// Default check for moving the caret to the end of the current line, which checks the End key.
        /// </summary>
        public static bool MoveCaretLineEndCheck(TextEntry _, char __) => MInput.Keyboard.Pressed(Keys.End);
        /// <summary>
        /// Default action associated with the left arrow key, which:
        /// <list>
        /// <item>If Control is held, moves the caret to the left by a full word, otherwise by a single character.</item>
        /// <item>If Shift is held, leaves <see cref="CaretAnchor"/> where it is to create or continue a selection, otherwise brings the anchor with the focus.</item>
        /// </list>
        /// </summary>
        public static void MoveCaretLeft(TextEntry textEntry, char _, InputEventInstructions evInstr) {
            evInstr.Break = true;
            if (textEntry.CaretFocus == 0) {
                evInstr.InvalidMinor = true;
                return;
            }
            int idx;
            if (textEntry.CaretMovementByWord) {
                idx = textEntry.IndexAtPreviousWord();
            } else {
                idx = textEntry.CaretFocus - 1;
            }
            textEntry.MoveCaretIndex(idx, textEntry.CaretMovementSelect);
            textEntry.CaretTargetX = textEntry.CaretFocusMeasure.Offset.X;
        }
        /// <summary>
        /// Default action associated with the right arrow key, which:
        /// <list>
        /// <item>If Control is held, moves the caret to the right by a full word, otherwise by a single character.</item>
        /// <item>If Shift is held, leaves <see cref="CaretAnchor"/> where it is to create or continue a selection, otherwise brings the anchor with the focus.</item>
        /// </list>
        /// </summary>
        public static void MoveCaretRight(TextEntry textEntry, char _, InputEventInstructions evInstr) {
            evInstr.Break = true;
            var end = textEntry.TextElement.Text.Length;
            if (textEntry.CaretFocus == end) {
                evInstr.InvalidMinor = true;
                return;
            }
            int idx;
            if (textEntry.CaretMovementByWord) {
                idx = textEntry.IndexAtNextWord();
            } else {
                idx = textEntry.CaretFocus + 1;
            }
            textEntry.MoveCaretIndex(idx, textEntry.CaretMovementSelect);
            textEntry.CaretTargetX = textEntry.CaretFocusMeasure.Offset.X;
        }
        /// <summary>
        /// Default action associated with the up arrow key, which:
        /// <list>
        /// <item>Moves the caret up one line.</item>
        /// <item>If Shift is held, leaves <see cref="CaretAnchor"/> where it is to create or continue a selection, otherwise brings the anchor with the focus.</item>
        /// </list>
        /// </summary>
        public static void MoveCaretUp(TextEntry textEntry, char _, InputEventInstructions evInstr) {
            evInstr.Break = true;
            var line = textEntry.TextElement.Measurements.Lines.FindIndex(line => line.LastCharIndex >= textEntry.CaretFocus);
            if (line == 0) {
                evInstr.InvalidMinor = true;
                return;
            }
            textEntry.TextElement.ClosestSpacingX(line - 1, textEntry.CaretTargetX, out int idx, out var _);
            textEntry.MoveCaretIndex(idx, textEntry.CaretMovementSelect);
        }
        /// <summary>
        /// Default action associated with the down arrow key, which:
        /// <list>
        /// <item>Moves the caret down one line.</item>
        /// <item>If Shift is held, leaves <see cref="CaretAnchor"/> where it is to create or continue a selection, otherwise brings the anchor with the focus.</item>
        /// </list>
        /// </summary>
        public static void MoveCaretDown(TextEntry textEntry, char _, InputEventInstructions evInstr) {
            evInstr.Break = true;
            var line = textEntry.TextElement.Measurements.Lines.FindIndex(line => line.LastCharIndex >= textEntry.CaretFocus);
            if (line == textEntry.TextElement.Measurements.Lines.Count) {
                evInstr.InvalidMinor = true;
                return;
            }
            textEntry.TextElement.ClosestSpacingX(line + 1, textEntry.CaretTargetX, out int idx, out var _);
            textEntry.MoveCaretIndex(idx, textEntry.CaretMovementSelect);
        }
        /// <summary>
        /// Default action associated with the Home key, which:
        /// <list>
        /// <item>Moves the caret to the start of the line.</item>
        /// <item>If Shift is held, leaves <see cref="CaretAnchor"/> where it is to create or continue a selection, otherwise brings the anchor with the focus.</item>
        /// </list>
        /// </summary>
        public static void MoveCaretLineStart(TextEntry textEntry, char _, InputEventInstructions evInstr) {
            evInstr.Break = true;
            var line = textEntry.TextElement.Measurements.Lines.First(line => line.LastCharIndex >= textEntry.CaretFocus);
            textEntry.MoveCaretIndex(line.FirstCharIndex, textEntry.CaretMovementSelect);
            textEntry.CaretTargetX = textEntry.CaretFocusMeasure.Offset.X;
        }
        /// <summary>
        /// Default action associated with the End key, which:
        /// <list>
        /// <item>Moves the caret to the end of the line.</item>
        /// <item>If Shift is held, leaves <see cref="CaretAnchor"/> where it is to create or continue a selection, otherwise brings the anchor with the focus.</item>
        /// </list>
        /// </summary>
        public static void MoveCaretLineEnd(TextEntry textEntry, char _, InputEventInstructions evInstr) {
            evInstr.Break = true;
            var line = textEntry.TextElement.Measurements.Lines.First(line => line.LastCharIndex >= textEntry.CaretFocus);
            textEntry.MoveCaretIndex(line.LastCharIndex + 1, textEntry.CaretMovementSelect);
            textEntry.CaretTargetX = textEntry.CaretFocusMeasure.Offset.X;
        }

        /// <summary>
        /// Default check for the Backspace key.
        /// </summary>
        public static bool BackspaceCheck(TextEntry _, char ch) => ch == '\b';
        //TODO the delete key does NOT send the delete character, it sends an ANSI escape sequence -- check for that instead of scancode
        /// <summary>
        /// Default check for the Delete key.
        /// </summary>
        public static bool DeleteCheck(TextEntry _, char __) => MInput.Keyboard.Pressed(Keys.Delete);
        /// <summary>
        /// Default action associated with the Backspace key, which:
        /// <list>
        /// <item>If Control is held, deletes a full word before the caret, otherwise deletes a single character.</item>
        /// </list>
        /// </summary>
        public static void Backspace(TextEntry textEntry, char _, InputEventInstructions evInstr) {
            evInstr.Break = true;
            if (textEntry.CaretAnchor == 0 && textEntry.CaretFocus == 0) {
                evInstr.InvalidMinor = true;
                return;
            }
            if (textEntry.CaretAnchor == textEntry.CaretFocus) {
                if (textEntry.CaretMovementByWord) {
                    textEntry.CaretAnchor = textEntry.IndexAtPreviousWord();
                    textEntry.InsertAtCaret("");
                    textEntry.CaretFocus = textEntry.CaretAnchor;
                } else {
                    textEntry.CaretAnchor = textEntry.CaretFocus--;
                    textEntry.TextElement.Text = textEntry.TextElement.Text.Remove(textEntry.CaretAnchor, 1);
                }
            } else {
                textEntry.InsertAtCaret("");
            }
        }
        /// <summary>
        /// Default action associated with the Delete key, which:
        /// <list>
        /// <item>If Control is held, deletes a full word after the caret, otherwise deletes a single character.</item>
        /// </list>
        /// </summary>
        public static void Delete(TextEntry textEntry, char _, InputEventInstructions evInstr) {
            evInstr.Break = true;
            var text = textEntry.TextElement.Text;
            var end = text.Length;
            if (textEntry.CaretAnchor == end && textEntry.CaretFocus == end) {
                evInstr.InvalidMinor = true;
                return;
            }
            if (textEntry.CaretAnchor == textEntry.CaretFocus) {
                if (textEntry.CaretMovementByWord) {
                    textEntry.CaretAnchor = textEntry.IndexAtNextWord();
                    textEntry.InsertAtCaret("");
                    textEntry.CaretFocus = textEntry.CaretAnchor;
                } else {
                    textEntry.TextElement.Text = textEntry.TextElement.Text.Remove(textEntry.CaretAnchor, 1);
                }
            } else {
                textEntry.InsertAtCaret("");
            }
        }

        /// <summary>
        /// Default check for the Escape key.
        /// </summary>
        public static bool EscapeCheck(TextEntry _, char ch) => ch == '\e';
        /// <summary>
        /// Release focus from the <see cref="TextEntry"/>.
        /// </summary>
        public static void Exit(TextEntry textEntry, char _, InputEventInstructions evInstr) {
            evInstr.Break = true;
            textEntry.LoseFocus();
        }

        public override void ConfirmPressed() {
            base.ConfirmPressed();
            GainFocus();
        }

        public override void Update() {
            while (CharsReceivedThisFrame.TryDequeue(out var ch)) {
                foreach (var charListener in OnReceiveChar) {
                    if (charListener.Key?.Invoke(this, ch) ?? false) {
                        var evInstr = new InputEventInstructions();
                        charListener.Value?.Invoke(this, ch, evInstr);
                        if (evInstr.Break) {
                            ConsumeGameInput();
                            if (evInstr.InvalidMajor) { NotifyInvalid(); }
                            CharsReceivedThisFrame.Clear();
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

            //draw back element
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
                //draw placeholder text when empty
                PlaceholderElement.Position.X = position.X;
                PlaceholderElement.Position.Y = position.Y;
                PlaceholderElement.Justify.X = JustifyX ?? PlaceholderElement.Justify.X;
                PlaceholderElement.Justify.Y = JustifyY ?? PlaceholderElement.Justify.Y;
                PlaceholderElement.MaxWidth = textWidth;
                PlaceholderElement.MaxHeight = textHeight;
                PlaceholderElement.Render();
                if (Typing) {
                    CaretElement.Position.X = 0f;
                    CaretElement.Position.Y = TextElement.Font.LineHeight * (JustifyY ?? TextElement.Justify.Y);
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
                    if (CaretAnchor != CaretFocus) {
                        //draw selected text indicator
                        SelectElement.Justify.Y = JustifyY ?? TextElement.Justify.Y;
                        OrderCaretMeasurements(out var firstMeasure, out var secondMeasure);
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
                //draw caret while typing
                var origCaretColor = CaretElement.Color;
                if (Engine.Scene.BetweenInterval(CaretBlinkInterval)) {
                    CaretElement.Color *= CaretBlinkAlpha;
                }
                CaretElement.Position.X = position.X + CaretFocusMeasure.Offset.X;
                CaretElement.Position.Y = position.Y + CaretFocusMeasure.Offset.Y;
                CaretElement.Justify.Y = JustifyY ?? TextElement.Justify.Y;
                CaretElement.ScaleYToFit(textHeight);
                CaretElement.Render();
                CaretElement.Color = origCaretColor;
            }
        }
    }
}