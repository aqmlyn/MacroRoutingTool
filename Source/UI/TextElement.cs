using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

/// <summary>
/// An object that handles drawing text onscreen.
/// </summary>
public class TextElement : VisualElement {
    /// <inheritdoc cref="VisualElement.ValueHandling{T}"/> 
    public new class ValueHandling<T> : VisualElement.ValueHandling<T> {
        /// <summary>
        /// Called by <see cref="ValueHandling.SetValueFromString"/> to set <see cref="Value"/> based on the given string. 
        /// </summary>
        public Func<string, T> ValueParser;
        /// <summary>
        /// Called by <see cref="ValueHandling.ValueToString"/> to get a string representation of <see cref="Value"/> for display.
        /// </summary>
        public Func<T, string> ValueToString;
    }

    /// <inheritdoc cref="VisualElement.ValueHandling"/> 
    public new class ValueHandling : VisualElement.ValueHandling {
        /// <summary>
        /// Sets <see cref="Value"/> based on the given string.
        /// </summary>
        public Action<string> SetValueFromString;
        /// <summary>
        /// Gets a string representation of <see cref="Value"/> for display.
        /// </summary>
        public Func<string> ValueToString;

        /// <inheritdoc cref="VisualElement.ValueHandling.Bind{T}"/> 
        public void Bind<T>(ValueHandling<T> handler = null) {
            base.Bind(handler);
            SetValueFromString = str => Value = handler.ValueParser == null ? default : handler.ValueParser(str);
            ValueToString = () => handler.ValueToString == null ? Value?.ToString() : handler.ValueToString((T)Value);
        }
    }

    /// <summary>
    /// Backing field for <see cref="ValueHandler"/>. 
    /// </summary>
    public ValueHandling _valueHandler = new();
    public new ValueHandling ValueHandler { get => _valueHandler; set { _valueHandler = value; base.ValueHandler = value; } }

    /// <summary>
    /// Backing field for <see cref="Text"/>.
    /// </summary>
    public string _text = "";
    /// <summary>
    /// The text this <see cref="TextElement"/> displays. If this element uses a <see cref="ValueHandler"/>,
    /// this property will raise its associated events -- getting will raise <see cref="ValueHandling.ValueToString"/>
    /// and setting will raise <see cref="ValueHandling.SetValueFromString"/>.
    /// </summary>
    public string Text
    {
        get
        {
            var newText = _valueHandler?.ValueToString?.Invoke() ?? _value?.ToString() ?? _text;
            NeedsRemeasured |= newText != _text;
            return _text = newText;
        }
        set
        {
            NeedsRemeasured |= _text != value;
            _text = value;
            _valueHandler?.SetValueFromString?.Invoke(value);
        }
    }

    /// <summary>
    /// Default value of <see cref="LineSpacing"/>, which is the default value used in vanilla for <see cref="TextMenu.ItemSpacing"/>.
    /// </summary>
    public static float DefaultLineSpacing = 4f;

    /// <summary>
    /// Vertical space between the bottom of one line and the top of the next, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.
    /// </summary>
    public float LineSpacing = DefaultLineSpacing;

    /// <summary>
    /// Thickness in pixels of the drop shadow under the text, or null to not draw a drop shadow.
    /// </summary>
    public float? DropShadowOffset = null;
    /// <summary>
    /// Color of the drop shadow around the text.
    /// </summary>
    public Color DropShadowColor = Color.DarkSlateBlue;

    /// <summary>
    /// Backing field for <see cref="Font"/>. 
    /// </summary>
    public PixelFontSize _font = ActiveFont.FontSize;
    /// <summary>
    /// Font of the text. Must be present in <see cref="Fonts.loadedFonts"/>. See
    /// <see href="https://github.com/EverestAPI/Resources/wiki/Adding-Custom-Dialogue#adding-a-completely-custom-font-for-the-game">this guide</see>
    /// to get Everest to load custom fonts.
    /// </summary>
    public PixelFontSize Font
    {
        get => _font;
        set
        {
            NeedsRemeasured |= _font != value;
            _font = value;
        }
    }

    /// <summary>
    /// Backing field for <see cref="MaxWidth"/>. 
    /// </summary>
    public float _maxWidth = float.MaxValue;
    /// <summary>
    /// Maximum width the text is allowed to occupy.
    /// </summary>
    public float MaxWidth
    {
        get => _maxWidth;
        set
        {
            NeedsRemeasured |= _maxWidth != value;
            _maxWidth = value;
        }
    }
    /// <summary>
    /// Backing field for <see cref="MaxHeight"/>. 
    /// </summary>
    public float _maxHeight = float.MaxValue;
    /// <summary>
    /// Maximum height the text is allowed to occupy.
    /// </summary>
    public float MaxHeight
    {
        get => _maxHeight;
        set
        {
            NeedsRemeasured |= _maxHeight != value;
            _maxHeight = value;
        }
    }
    /// <summary>
    /// Backing field for <see cref="MinScale"/>. 
    /// </summary>
    public float _minScale = 1f;
    /// <summary>
    /// Minimum factor by which <see cref="VisualElement.Scale"/> may be multiplied for oversize text. Must be between 0 and 1 to have any effect.
    /// </summary>
    public float MinScale
    {
        get => _minScale;
        set
        {
            NeedsRemeasured |= _minScale != value;
            _minScale = value;
        }
    }
    /// <summary>
    /// Backing field for <see cref="OversizeTruncate"/>. 
    /// </summary>
    public string _oversizeTruncate = "...";
    /// <summary>
    /// Text that appears when truncated text is shown due to the full text being too long to fit.
    /// </summary>
    public string OversizeTruncate
    {
        get => _oversizeTruncate;
        set
        {
            NeedsRemeasured |= _oversizeTruncate != value;
            _oversizeTruncate = value;
        }
    }

    /// <summary>
    /// Actual values that will be used to render as much of a <see cref="TextElement"/>'s text as possible
    /// in the space provided by the element's <see cref="MaxWidth"/>, <see cref="MaxHeight"/>, and <see cref="MinScale"/>. 
    /// </summary>
    public class Measurement {
        /// <summary>
        /// The scale at which the text will be rendered. Each axis of the original <see cref="VisualElement.Scale"/> might have
        /// been multiplied by a value to fit as much of the original text as possible in the space provided.
        /// </summary>
        public Vector2 Scale = Vector2.One;
        /// <summary>
        /// This object's <see cref="Scale"/> is the original <see cref="VisualElement.Scale"/> multiplied by this factor.
        /// </summary>
        /// <remarks>
        /// The measurement <see cref="Scale"/> field is just for convenience and very minor optimization. This is the important field --
        /// some things other than the main graphic also need scaled to be rendered accordingly, e.g. <see cref="VisualElement.BorderThickness"/>
        /// and <see cref="DropShadowOffset"/>, and that's done by passing this factor to the corresponding arguments of
        /// <see cref="PixelFontSize.DrawEdgeOutline"/>.
        /// </remarks>
        public float ScaleFactor = 1f;
        /// <summary>
        /// The width that is actually needed to render the text in the space provided.
        /// </summary>
        public float Width = 0f;
        /// <summary>
        /// The height that is actually needed to render the text in the space provided.
        /// </summary>
        public float Height = 0f;
        /// <summary>
        /// Measurements for a line of an element's text.
        /// </summary>
        public class Line {
            /// <summary>
            /// Index (0-indexed) of this line in the element's to-be-rendered text. 
            /// </summary>
            public int Index = 0;
            /// <summary>
            /// Index (0-indexed) at which the first character in this line appears in the element's original text.
            /// </summary>
            public int FirstCharIndex = 0;
            /// <summary>
            /// Index (0-indexed) at which the last character in this line from the original text appears in the full original text.
            /// </summary>
            public int LastCharIndex = 0;
            /// <summary>
            /// The text which appears on this line.
            /// </summary>
            public string Text = "";
            /// <summary>
            /// The left edge of the first character on this line.
            /// </summary>
            public float LeftOffset = 0f;
            /// <summary>
            /// The right edge of the last character on this line.
            /// </summary>
            public float RightOffset = 0f;
        }
        /// <summary>
        /// List of measurements for each line of this object's text.
        /// </summary>
        public List<Line> Lines = [];
    }
    /// <summary>
    /// Indicates to the <see cref="Measurements"/> getter whether <see cref="Measure"/> needs to be called before returning.
    /// </summary>
    public bool NeedsRemeasured = true;
    /// <summary>
    /// Backing field for <see cref="Measurements"/>.
    /// </summary>
    public Measurement _measurements = new();
    /// <summary>
    /// Actual values that will be used to render this <see cref="TextElement"/>. 
    /// </summary>
    public Measurement Measurements
    {
        get
        {
            if (NeedsRemeasured) { Measure(); }
            return _measurements;
        }
    }
    public void Measure() {
        NeedsRemeasured = false;
        _measurements = new();

        var text = Text;

        //IgnoreZoom
        _measurements.Scale.X = Scale.X;
        _measurements.Scale.Y = Scale.Y;
        _measurements.ScaleFactor = 1f;
        if (IgnoreZoom && UIHelpers.SpriteBatches.TryGetValue(Draw.SpriteBatch, out var batchData) && batchData.TryGetValue(UIHelpers.SpriteBatchZoom, out object foundZoom) && foundZoom is float validZoom) {
            _measurements.ScaleFactor /= validZoom;
            _measurements.Scale *= _measurements.ScaleFactor;
        }

        //MinScale, MaxWidth, MaxHeight
        bool canShrink = MinScale > 0f && MinScale < 1f;
        int maxLineCount = (int)Math.Floor(_maxHeight / Font.LineHeight);
        bool canWrap = maxLineCount > 1;
        if (canShrink && canWrap) {
            //TODO
        } else {
            Measurement.Line lastLine = new();
            if (canWrap) {
                int textIndex = 0;
                for (int i = 0; i < maxLineCount - 1 && textIndex < text.Length; i++) {
                    Measurement.Line line = new() { FirstCharIndex = textIndex, Index = i };
                    _measurements.Lines.Add(line);
                    float width = 0f;
                    while (width < _maxWidth && textIndex < text.Length) {
                        var ch = text[textIndex];
                        if (ch == '\n') {
                            line.LastCharIndex = textIndex - 1;
                            line.Text = text[line.FirstCharIndex..(line.LastCharIndex - line.FirstCharIndex)];
                            line.LeftOffset = -width * Justify.X;
                            line.RightOffset = line.LeftOffset + width;
                            textIndex++;
                            break;
                        }
                        //width check adapted from Monocle.PixelFontSize.Measure(string)
                        if (Font.Characters.TryGetValue(ch, out var chm)) {
                            var chWidth = chm.XAdvance;
                            if (textIndex != line.FirstCharIndex) { chWidth += Font.KerningBetween(text[textIndex - 1], ch); }
                            if (width + chWidth > _maxWidth) {
                                line.LastCharIndex = textIndex - 1;
                                line.Text = text[line.FirstCharIndex..(line.LastCharIndex - line.FirstCharIndex)];
                                line.LeftOffset = -width * Justify.X;
                                line.RightOffset = line.LeftOffset + width;
                                break;
                            }
                            width += chWidth;
                            textIndex++;
                        }
                    }
                    if (textIndex >= text.Length) {
                        line.LastCharIndex = text.Length - 1;
                        line.Text = text[line.FirstCharIndex..(line.LastCharIndex - line.FirstCharIndex)];
                        line.LeftOffset = -width * Justify.X;
                        line.RightOffset = line.LeftOffset + width;
                        goto FinishedLines;
                    }
                }
                lastLine.Index = maxLineCount;
                lastLine.FirstCharIndex = textIndex;
            }
            _measurements.Lines.Add(lastLine);
            float lastWidth = 0f;
            float maxWidth = _maxWidth * (canShrink ? MinScale : 1f);
            bool needsTruncated = false;
            Stack<float> charWidths = new();
            for (int i = lastLine.FirstCharIndex; i < text.Length; i++) {
                var ch = text[i];
                if (ch == '\n') {
                    lastLine.Text = text[..(i - 1)];
                    lastLine.LastCharIndex = i - 1;
                    needsTruncated = true;
                    break;
                }
                //adapted from Monocle.PixelFontSize.Measure(string)
                if (Font.Characters.TryGetValue(ch, out var chm)) {
                    var chmWidth = chm.XAdvance;
                    if (i != lastLine.FirstCharIndex) { chmWidth += Font.KerningBetween(text[i - 1], ch); }
                    if (lastWidth + chmWidth > maxWidth) {
                        lastLine.Text = text[..(i - 1)];
                        lastLine.LastCharIndex = i - 1;
                        needsTruncated = true;
                        break;
                    } else {
                        lastWidth += chmWidth;
                        charWidths.Push(chmWidth);
                    }
                }
            }
            if (needsTruncated) {
                float truncWidth = Font.Measure(OversizeTruncate).X;
                while (lastWidth + truncWidth > maxWidth && charWidths.TryPop(out var charWidth)) {
                    lastLine.Text = lastLine.Text[..^1];
                    lastLine.LastCharIndex--;
                    lastWidth -= charWidth;
                }
                lastLine.Text += OversizeTruncate;
                //LastCharIndex will be the index of the last character *before* OversizeTruncate's first character
            } else {
                lastLine.Text = text[lastLine.FirstCharIndex..];
                lastLine.LastCharIndex = text.Length - 1 - lastLine.FirstCharIndex;
            }
            lastLine.LeftOffset = -lastWidth * Justify.X;
            lastLine.RightOffset = lastLine.LeftOffset + lastWidth;
        }

        FinishedLines:
        _measurements.Width = _measurements.Lines.Select(line => line.RightOffset - line.LeftOffset).Max();
        if (canShrink) {
            var spaceFactor = _maxWidth / _measurements.Width;
            _measurements.ScaleFactor *= spaceFactor;
            _measurements.Scale *= spaceFactor;
        }
        _measurements.Height = Font.LineHeight * _measurements.Lines.Count;
    }

    /// <summary>
    /// Finds the horizontal center of the spacing between two letters which is closest to the given horizontal position on the given line.
    /// </summary>
    /// <param name="lineIndex">Index (0-indexed) of the desired line in <see cref="Measurements"/>' <see cref="Lines"/>.</param>
    /// <param name="targetXOffset">Target horizontal offset <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.</param>
    /// <param name="index">If the closest space is to the right of the last character, this will be one more than the line's <see cref="Measurement.Line.LastCharIndex"/>.
    /// Otherwise, this will be the index of the character of which this space is to the left.</param>
    /// <param name="closestXOffset">Horizontal center of the spacing between two letters which is closest to the given horizontal position on the given line.</param>
    /// <exception cref="IndexOutOfRangeException">The given index was out of range of the <see cref="Measurements"/>' <see cref="Lines"/> list.</exception>
    /// <exception cref="UnreachableException">The line's RightOffset and the actual right edge of its last character don't match, indicating a mistake in <see cref="Measure"/>.</exception>
    public void ClosestSpacingX(Index lineIndex, float targetXOffset, out int index, out bool after, out float closestXOffset) {
        if (!lineIndex.IsFromEnd && lineIndex.Value < 0) {
            throw new IndexOutOfRangeException($"Received negative index {lineIndex.Value}");
        }
        var msrmts = Measurements;
        var lineIdxVal = lineIndex.GetOffset(msrmts.Lines.Count);
        var text = Text;
        if (lineIdxVal >= msrmts.Lines.Count) {
            throw new IndexOutOfRangeException($"Received index {lineIdxVal} (from '{lineIndex}') which is out-of-range for text with line count {msrmts.Lines.Count}: {text}");
        }
        var line = msrmts.Lines[lineIndex];
        if (targetXOffset < line.LeftOffset) {
            index = line.FirstCharIndex;
            after = false;
            closestXOffset = line.LeftOffset;
            return;
        }
        if (targetXOffset > line.RightOffset) {
            index = line.LastCharIndex;
            after = true;
            closestXOffset = line.RightOffset;
            return;
        }

        var checkX = line.LeftOffset;
        for (int i = line.FirstCharIndex; i <= line.LastCharIndex; i++) {
            if (Font.Characters.TryGetValue(text[i], out var ch)) {
                var width = 0f;
                var kerningBefore = 0f;
                if (i > line.FirstCharIndex) { kerningBefore = Font.KerningBetween(text[i - 1], text[i]); }
                var kerningAfter = 0f;
                if (i < line.LastCharIndex) { kerningAfter = Font.KerningBetween(text[i], text[i + 1]); }
                width = kerningBefore + ch.XAdvance + kerningAfter;
                if (checkX + width > targetXOffset) {
                    var rem = targetXOffset - checkX;
                    index = i;
                    closestXOffset = checkX;
                    if (rem >= width / 2f) {
                        after = true;
                        closestXOffset += width - kerningAfter / 2f;
                    } else {
                        after = false;
                        closestXOffset -= kerningBefore / 2f;
                    }
                    return;
                }
                checkX += width - kerningAfter;
            }
        }
        throw new UnreachableException($"The line's {nameof(Measurement.Line.RightOffset)} ({line.RightOffset}) and the actual right edge of its last character ({checkX}) don't match, which indicates a mistake in {nameof(TextElement)}.{nameof(Measure)}(). Please report this to the Macrorouting Tool developer(s)!");
    }

    /// <summary>
    /// Finds the horizontal center of the spacing between two letters which is closest to the given position.
    /// </summary>
    /// <param name="targetOffset">Target offset <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.</param>
    /// <param name="index">If the closest space is to the right of the closest line's last character, this will be one more than the line's <see cref="Measurement.Line.LastCharIndex"/>.
    /// Otherwise, this will be the index of the character of which this space is to the left.</param>
    /// <param name="closestXOffset">Horizontal center of the spacing between two letters which is closest to the given position.</param>
    /// <exception cref="UnreachableException">The closest line's RightOffset and the actual right edge of its last character don't match, indicating a mistake in <see cref="Measure"/>.</exception>
    public void ClosestSpacingX(Vector2 targetOffset, out int index, out bool after, out float closestXOffset) {
        int lineIndex;
        var msrmts = Measurements;
        var top = Position.Y - msrmts.Height * Justify.Y;
        if (targetOffset.Y < top) {
            lineIndex = 0;
        } else if (targetOffset.Y > top + msrmts.Height) {
            lineIndex = msrmts.Lines.Count - 1;
        } else {
            lineIndex = (int)Math.Round(msrmts.Height / Font.LineHeight * targetOffset.Y);
        }
        ClosestSpacingX(lineIndex, targetOffset.X, out index, out after, out closestXOffset);
        return;
    }

    /// <summary>
    /// Measurements related to a character at a specific index in a <see cref="TextElement"/> based on
    /// that element's <see cref="Measurements"/> at the time this object was created.
    /// </summary>
    public class CharMeasurements {
        /// <summary>
        /// Whether this measurement was created before or after this character.
        /// </summary>
        public bool After = false;
        /// <summary>
        /// A position relative to this <see cref="TextElement"/>'s <see cref="VisualElement.Position"/> based on this character's position.
        /// <list type="bullet">
        /// <item>If the index is exactly the current character count or if <see cref="After"/> is true and the next character is a newline, return the right edge of the last character.</item>
        /// <item>If the index is 0 or if <see cref="After"/> is false and the previous character is a newline, return the left edge of the character at the given index.</item>
        /// <item>Otherwise, return the center of the <seealso href="https://en.wikipedia.org/wiki/Kerning#Kerning_values">kerning</seealso> to the left of the character at the given index.</item>
        /// </list>
        /// </summary>
        public Vector2 Offset = Vector2.Zero;
        /// <summary>
        /// Index (0-indexed) of this character in this <see cref="TextElement"/>'s <see cref="Measurements"/>' text copy.
        /// </summary>
        public int Index = 0;
        /// <summary>
        /// Index (0-indexed) of this character in the line in which it appears in this <see cref="TextElement"/>'s <see cref="Measurements"/>' text copy.
        /// </summary>
        public int IndexInLine = 0;
        /// <summary>
        /// Measurements for the line in which this character appears in this <see cref="TextElement"/>'s <see cref="Measurements"/>' text copy.
        /// </summary>
        public Measurement.Line Line = null;
    }

    /// <summary>
    /// Return a position, relative to this element's current <see cref="VisualElement.Position"/>, based on the given index (0-indexed) of this element's current <see cref="Text"/>.
    /// <list type="bullet">
    /// <item>If <paramref name="after"/> is true and the next character is a newline, return the right edge of the last character.</item>
    /// <item>If the index is 0 or if <paramref name="after"/> is false and the previous character is a newline, return the left edge of the character at the given index.</item>
    /// <item>Otherwise, return the center of the <seealso href="https://en.wikipedia.org/wiki/Kerning#Kerning_values">kerning</seealso> to the left of the character at the given index.</item>
    /// </list>
    /// </summary>
    /// <param name="index">Index (0-indexed) in the current text of the character to return a position in relation to.</param>
    /// <param name="after">If the character at the given index is a newline, false will return the right edge of the line it ends, and true
    /// will return the left edge of the line it begins. Otherwise, true effectively just moves given index one character forward.</param>
    /// <exception cref="NullReferenceException"><see cref="Text"/> currently returns null.</exception>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> was negative or greater than <see cref="Text"/>'s current <see cref="string.Length"/>.</exception>
    public CharMeasurements MeasureChar(Index index, bool after = false) {
        var text = Text ?? throw new NullReferenceException("This element's text is currently null.");
        var idxval = index.GetOffset(text.Length);
        if (idxval < 0 || idxval >= text.Length) { throw new IndexOutOfRangeException($"Received index {idxval} (from '{index}') which is out-of-range for text with length {text.Length}: {text}"); }
        if (NeedsRemeasured) { Measure(); }
        if (text[idxval] == '\n') {
            if (after) { idxval++; } else { idxval--; }
            idxval = Calc.Clamp(idxval, 0, text.Length - 1);
            after = !after;
        }
        Measurement.Line line = null;
        CharMeasurements chm = new() { Index = idxval, After = after, Offset = Vector2.Zero - new Vector2(_measurements.Width, _measurements.Height) * Justify };
        for (int i = 0; i < _measurements.Lines.Count; i++) {
            line = _measurements.Lines[i];
            if (line.LastCharIndex < idxval) { continue; }
            chm.Line = line;
            chm.IndexInLine = idxval - line.FirstCharIndex;
            chm.Offset.Y += Font.LineHeight * _measurements.Scale.Y * i;
            chm.Offset.X += Font.Measure(line.Text[..chm.IndexInLine]).X;
            if (after && Font.Characters.TryGetValue(line.Text[chm.IndexInLine], out var ch)) { chm.Offset.X += ch.XAdvance; }
            return chm;
        }
        //if the given index is more than the last row's LastIndex, then there's more text that wasn't measured.
        //this should occur when text is truncated, otherwise it suggests a logical error in TextElement.Measure
        if (line != null) {
            Logger.Warn(MRT.LogTags.UI, $"Index {idxval} is past the last measured character ({line.Text[^1]} at index {line.LastCharIndex}) -- returning null.");
        }
        return null;
    }

    public override void Render() {
        var ogSpriteBatch = Draw.SpriteBatch;
        Draw.SpriteBatch = SpriteBatch;

        if (NeedsRemeasured) { Measure(); }

        var position = new Vector2(0f, Position.Y - _measurements.Height * Justify.Y);
        if (DropShadowOffset != null) {
            var borderThickness = (BorderThickness ?? 0f) * _measurements.ScaleFactor;
            foreach (var line in _measurements.Lines) {
                position.X = Position.X + line.LeftOffset;
                Font.DrawEdgeOutline(line.Text, position, Vector2.Zero, _measurements.Scale, Color, (float)DropShadowOffset, DropShadowColor, borderThickness, BorderColor);
                position.Y += Font.LineHeight;
            }
        } else if (BorderThickness != null) {
            var borderThickness = (float)BorderThickness * _measurements.ScaleFactor;
            foreach (var line in _measurements.Lines) {
                position.X = Position.X + line.LeftOffset;
                Font.DrawOutline(line.Text, position, Vector2.Zero, _measurements.Scale, Color, borderThickness, BorderColor);
                position.Y += Font.LineHeight;
            }
        } else {
            foreach (var line in _measurements.Lines) {
                position.X = Position.X + line.LeftOffset;
                Font.Draw(line.Text, position, Vector2.Zero, _measurements.Scale, Color);
                position.Y += Font.LineHeight;
            }
        }

        Draw.SpriteBatch = ogSpriteBatch;
    }
}