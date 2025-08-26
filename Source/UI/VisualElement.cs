using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

/// <summary>
/// An object that handles something to be drawn onscreen.
/// </summary>
public abstract class VisualElement {
    /// <summary>
    /// <see cref="ValueHandling"/> fields that require knowing the type of value a handler is handling.<br/>
    /// This one's holding a <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type of value this handler is handling.</typeparam>
    public class ValueHandling<T> {
        /// <inheritdoc cref="ValueHandling.ValueGetter"/>
        public Func<T> ValueGetter;
        /// <inheritdoc cref="ValueHandling.ValueSetter"/> 
        public Action<T, T> ValueSetter;
        /// <summary>
        /// The current value.
        /// </summary>
        public T Value;
    }

    /// <summary>
    /// <see cref="ValueHandling"/> fields that can be used without knowing the type of value a handler is handling.
    /// </summary>
    public class ValueHandling {
        /// <summary>
        /// The current value.
        /// </summary>
        public object Value {
            get => ValueGetter?.Invoke();
            set => ValueSetter?.Invoke(Value, value);
        }

        /// <summary>
        /// Returns <see cref="Value"/>. 
        /// </summary>
        public Func<object> ValueGetter;
        /// <summary>
        /// Called with the old and new values of <see cref="Value"/> just before setting it.
        /// </summary>
        public Action<object, object> ValueSetter;

        /// <summary>
        /// Modifies the delegates of the typeless form of a ValueHandling based on the delegates of the typed form.
        /// </summary>
        /// <typeparam name="T">Type of value this handler is handling.</typeparam>
        /// <param name="handler">Typed ValueHandling whose delegates to use.</param>
        protected void Bind<T>(ValueHandling<T> handler = null) {
            handler ??= new();
            ValueGetter = () => handler.Value = handler.ValueGetter == null ? handler.Value : handler.ValueGetter();
            ValueSetter = (oldVal, newVal) => {
                var castNewVal = (T)newVal;
                handler.ValueSetter?.Invoke((T)oldVal, castNewVal);
                handler.Value = castNewVal; 
            };
        }
    }

    /// <summary>
    /// The object that handles continuously getting what is to be displayed in this element.
    /// </summary>
    public ValueHandling ValueHandler { get; set; }
    /// <summary>
    /// Backing field for <see cref="Value"/>. 
    /// </summary>
    public object _value;
    /// <summary>
    /// The value this <see cref="VisualElement"/> displays a visual representation of.
    /// </summary>
    public object Value {
        get {return _value = ValueHandler?.ValueGetter?.Invoke() ?? _value;}
        set {_value = value;}
    }
    /// <summary>
    /// The <see cref="Microsoft.Xna.Framework.Graphics.SpriteBatch"/> in which this element is to be drawn.
    /// </summary>
    public SpriteBatch SpriteBatch = Draw.SpriteBatch;

    /// <summary>
    /// Origin about which the graphic is to be rendered. Units vary depending on the transformation matrix
    /// assigned in the most recent <see cref="SpriteBatch.Begin()"/> call.  
    /// </summary>
    public Vector2 Position = Vector2.Zero;
    /// <summary>
    /// Where the origin is relative to where the graphic is to be rendered. (0, 0) is top left, (1, 1) is bottom right.
    /// </summary>
    public Vector2 Justify = Vector2.Zero;
    /// <summary>
    /// Size multiplier for the graphic.
    /// </summary>
    public Vector2 Scale = Vector2.One;
    /// <summary>
    /// Color of the graphic.
    /// </summary>
    public Color Color = Color.White;
    /// <summary>
    /// Thickness in pixels of the border around the graphic, or null to not draw a border.
    /// </summary>
    public float? BorderThickness = null;
    /// <summary>
    /// Color of the border around the graphic.
    /// </summary>
    public Color BorderColor = Color.Black;
    /// <summary>
    /// Whether to always draw the graphic at the same size, regardless of what the camera's current zoom is.
    /// </summary>
    public bool IgnoreZoom = false;
    
    /// <summary>
    /// Keep the value this graphic is based on updated.
    /// </summary>
    public virtual void Update() {
        //just need to call Value's getter to keep it up to date
        object _ = Value;
    }
    
    /// <summary>
    /// Display a graphic as the other fields of this element specify.
    /// </summary>
    public abstract void Render();
}

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
    public string Text {
        get {
            var newText = _valueHandler?.ValueToString?.Invoke() ?? _value?.ToString() ?? _text;
            NeedsRemeasured |= newText != _text;
            return _text = newText;
        }
        set {
            NeedsRemeasured |= _text != value;
            _text = value;
            _valueHandler?.SetValueFromString?.Invoke(value);
        }
    }

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
    public PixelFontSize Font {
        get => _font;
        set {
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
    public float MaxWidth {
        get => _maxWidth;
        set {
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
    public float MaxHeight {
        get => _maxHeight;
        set {
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
    public float MinScale {
        get => _minScale;
        set {
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
    public string OversizeTruncate {
        get => _oversizeTruncate;
        set {
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
    public Measurement Measurements { get {
        if (NeedsRemeasured) { Measure(); }
        return _measurements;
    } }
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
                    Measurement.Line line = new() {FirstCharIndex = textIndex, Index = i};
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
    /// <item>If the index is exactly the current character count or if <paramref name="after"/> is true and the next character is a newline, return the right edge of the last character.</item>
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
        CharMeasurements chm = new() { Index = idxval, After = after, Offset = Vector2.Zero - new Vector2(_measurements.Width, _measurements.Height) * Justify};
        for (int i = 0; i < _measurements.Lines.Count; i++) {
            var line = _measurements.Lines[i];
            if (line.LastCharIndex < idxval) { continue; }
            chm.Line = line;
            chm.IndexInLine = idxval - line.FirstCharIndex;
            chm.Offset.Y += Font.LineHeight * _measurements.Scale.Y * i;
            chm.Offset.X += Font.Measure(line.Text[..chm.IndexInLine]).X;
            if (after && Font.Characters.TryGetValue(line.Text[chm.IndexInLine], out var ch)) { chm.Offset.X += ch.XAdvance; }
            return chm;
        }
        //should be unreachable -- if not, the last row's LastIndex is less than the given index,
        //but the given index isn't out of range of the original text. this can only mean there's more text that wasn't measured
        throw new System.Diagnostics.UnreachableException($"{nameof(TextElement)}.{nameof(Measure)} seems to have a logical error that prevented some of this element's text from being measured. Please report this to the Macrorouting Tool developer!");
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

/// <summary>
/// An object that handles drawing a still or animated texture onscreen.
/// </summary>
public class TextureElement : VisualElement {
    /// <inheritdoc cref="VisualElement.ValueHandling{T}"/> 
    public new class ValueHandling<T> : VisualElement.ValueHandling<T> {
        public Func<T, Sprite> ValueToSprite;
        public Func<Sprite, T> SetValueFromSprite;
    }

    /// <inheritdoc cref="VisualElement.ValueHandling"/> 
    public new class ValueHandling : VisualElement.ValueHandling {
        public Func<Sprite> ValueToSprite;
        public Action<Sprite> SetValueFromSprite;
        /// <inheritdoc cref="VisualElement.ValueHandling.Bind{T}"/> 
        public void Bind<T>(ValueHandling<T> handler = null) {
            base.Bind(handler);
            ValueToSprite = () => handler.ValueToSprite?.Invoke(handler.Value) ?? default;
            SetValueFromSprite = sprite => handler.Value = handler.SetValueFromSprite == null ? default : handler.SetValueFromSprite(sprite);
        }
    }

    /// <summary>
    /// Backing field for <see cref="ValueHandler"/>. 
    /// </summary>
    public ValueHandling _valueHandler = new();
    public new ValueHandling ValueHandler { get => _valueHandler; set { _valueHandler = value; base.ValueHandler = value; } }

    /// <summary>
    /// The texture this <see cref="TextureElement"/> is currently displaying.
    /// </summary>
    public MTexture Texture {
        get => _sprite.GetFrame(_sprite.CurrentAnimationID, _sprite.CurrentAnimationFrame);
        set {
            var entity = _sprite.Entity;
            _sprite.RemoveSelf();
            //the parameterless constructor doesn't initialize the animations dict, so trying to add an animation would crash with NRE.
            Sprite = new(value.Atlas, ""){Rate = 0f, Visible = false};
            entity.Add(_sprite);
            _sprite.AddLoop("idle", 0f, [value]);
            _sprite.Play("idle");
        }
    }
    /// <summary>
    /// Backing field for <see cref="Sprite"/>. 
    /// </summary>
    public Sprite _sprite = new(GFX.Game, ""){Visible = false};
    /// <summary>
    /// The sprite this <see cref="TextureElement"/> is currently displaying.
    /// </summary>
    public Sprite Sprite {
        get => _sprite = _valueHandler.ValueToSprite?.Invoke() ?? _sprite;
        set { _sprite = value; _valueHandler.SetValueFromSprite?.Invoke(value); }
    }
    
    /// <summary>
    /// Radians by which the texture is to be rotated about its <see cref="VisualElement.Position"/>.
    /// </summary>
    public float Rotation = 0f;
    /// <summary>
    /// Axes about which the texture is to be flipped.
    /// </summary>
    public SpriteEffects Flip = SpriteEffects.None;
    
    /// <summary>
    /// Modify this element's <see cref="Scale"/> so that it will have the specified dimensions.
    /// </summary>
    /// <param name="width">The width this element should have.</param>
    /// <param name="height">The height this element should have.</param>
    public void ScaleToFit(float width, float height) {
        Scale.X = width / Texture.Width;
        Scale.Y = height / Texture.Height;
    }

    /// <summary>
    /// <inheritdoc cref="ScaleToFit(float, float)"/>
    /// </summary>
    /// <param name="dimensions">Vector whose X is the width this element should have and Y is the height this element should have.</param>
    public void ScaleToFit(Vector2 dimensions) => ScaleToFit(dimensions.X, dimensions.Y);
    
    /// <summary>
    /// Modify this element's <see cref="Scale"/>'s horizontal component so that the element will have the specified width.
    /// </summary>
    /// <param name="width">The width this element should have.</param>
    /// <param name="maintainRatio">If true (default), <see cref="Scale"/>'s vertical component will be set to the
    /// same as its new horizontal component in order to maintain the texture's aspect ratio.</param>
    public void ScaleXToFit(float width, bool maintainRatio = true) {
        Scale.X = width / Texture.Width;
        if (maintainRatio) { Scale.Y = Scale.X; }
    }

    /// <summary>
    /// Modify this element's <see cref="Scale"/>'s vertical component so that the element will have the specified height.
    /// </summary>
    /// <param name="height">The height this element should have.</param>
    /// <param name="maintainRatio">If true (default), <see cref="Scale"/>'s horizontal component will be set to the
    /// same as its new vertical component in order to maintain the texture's aspect ratio.</param>
    public void ScaleYToFit(float height, bool maintainRatio = true) {
        Scale.Y = height / Texture.Height;
        if (maintainRatio) { Scale.X = Scale.Y; }
    }
    
    public override void Render() {
        if (Texture == null) {return;}
        //copied from vanilla MTexture.DrawOutlineJustified decomp and adjusted to take outline arguments into account
        float scaleFix = Texture.ScaleFix;
        Vector2 scale = Scale * scaleFix;
        float zoomInverseFactor = 1f;
        if (IgnoreZoom && UIHelpers.SpriteBatches.TryGetValue(Draw.SpriteBatch, out var batchData) && batchData.TryGetValue(UIHelpers.SpriteBatchZoom, out object foundZoom) && foundZoom is float validZoom) {
            zoomInverseFactor /= validZoom;
            scale *= zoomInverseFactor;
        }
        Rectangle clipRect = Texture.ClipRect;
        Vector2 origin = (new Vector2(Texture.Width * Justify.X, Texture.Height * Justify.Y) - Texture.DrawOffset) / scaleFix;
        if (BorderThickness != null) {
            float outlineWidth = (float)BorderThickness * zoomInverseFactor;
            for (float i = -1; i <= 1; i++) {
                for (float j = -1; j <= 1; j ++) {
                    if (i != 0 || j != 0) {
                        SpriteBatch.Draw(Texture.Texture.Texture_Safe, Position + new Vector2(i * outlineWidth, j * outlineWidth), clipRect, BorderColor, Rotation, origin, scale, Flip, 0f);
                    }
                }
            }
        }
        SpriteBatch.Draw(Texture.Texture.Texture_Safe, Position, clipRect, Color, Rotation, origin, scale, Flip, 0f);
    }
    
    /// <inheritdoc cref="TextureElement"/> 
    public TextureElement() {
        //sprites are components, so they have to be added to entities in order to automatically update and render.
        //to avoid having to manually call update/render in classes that use TextureElements, add the sprite to an entity in the constructor.
        if (Engine.Scene is Level level) {
            //adding entities, especially ones with depth 0, can desync TASes, so instead add the component to an entity that will always exist.
            level.strawberriesDisplay.Add([_sprite]);
        } else {
            //other scenes might not have any entities, so we have to risk adding one... suuurely this never matters, right?
            Entity entity = new();
            Engine.Scene.Add(entity);
            entity.Add([_sprite]);
        }
    }
}

/// <summary>
/// <inheritdoc cref="TextElement"/> It can store a state and renders in different colors for each possible state.
/// </summary>
public class UITextElement : TextElement {
    /// <summary>
    /// List of common states used by basic UI elements.
    /// </summary>
    public class States {
        /// <summary>
        /// This UI element is in its default state, not hovered or selected.
        /// </summary>
        public const string Idle = nameof(Idle);
        /// <summary>
        /// This UI element is currently hovered.
        /// </summary>
        public const string Hovered = nameof(Hovered);
        /// <summary>
        /// This UI element is currently selected.
        /// </summary>
        public const string Selected = nameof(Selected);
    }

    /// <summary>
    /// The current state. Should be present as a key in <see cref="ColorsByState"/> -- if not present,
    /// the color for <see cref="States.Idle"/> will be used as a fallback.
    /// </summary>
    public string State;

    /// <summary>
    /// List of possible states and corresponding colors.
    /// </summary>
    public Dictionary<string, Color> ColorsByState = new(){
        {States.Idle, Color.White},
        {States.Hovered, TextMenu.HighlightColorB},
        {States.Selected, TextMenu.HighlightColorA}
    };

    public override void Render() {
        if (!ColorsByState.TryGetValue(State, out Color) && !ColorsByState.TryGetValue(States.Idle, out Color)) {
            Color = Color.White;
        }
        base.Render();
    }
}

/// <summary>
/// <inheritdoc cref="TextureElement"/> It can store a state and renders in different colors for each possible state.
/// </summary>
public class UITextureElement : TextureElement {
    /// <summary>
    /// List of common states used by basic UI elements.
    /// </summary>
    public class States {
        /// <summary>
        /// This UI element is in its default state, not hovered or selected.
        /// </summary>
        public const string Idle = nameof(Idle);
        /// <summary>
        /// This UI element is currently hovered.
        /// </summary>
        public const string Hovered = nameof(Hovered);
        /// <summary>
        /// This UI element is currently selected.
        /// </summary>
        public const string Selected = nameof(Selected);
    }

    /// <summary>
    /// The current state. Should be present as a key in <see cref="ColorsByState"/> -- if not present,
    /// the color for <see cref="States.Idle"/> will be used as a fallback.
    /// </summary>
    public string State = States.Idle;

    /// <summary>
    /// List of possible states and corresponding colors.
    /// </summary>
    public Dictionary<string, Color> ColorsByState = new(){
        {States.Idle, Color.White},
        {States.Hovered, TextMenu.HighlightColorB},
        {States.Selected, TextMenu.HighlightColorA}
    };

    /// <summary>
    /// Texture parts that the current <see cref="State"/> can affect.
    /// </summary>
    public static class StateTargets {
        /// <summary>
        /// The current <see cref="State"/> sets the color of the texture itself.
        /// </summary>
        public const string Texture = nameof(Texture);
        /// <summary>
        /// The current <see cref="State"/> sets the color of the border around the texture.
        /// </summary>
        public const string Border = nameof(Border);
    }
    /// <summary>
    /// The texture part affected by the current <see cref="State"/>. 
    /// </summary>
    public string StateTarget = StateTargets.Border;

    //TODO unhardcode StateTarget effect
    public override void Render() {
        var color = Color;
        var borderColor = BorderColor;
        if (StateTarget == StateTargets.Texture) {
            Color = ColorsByState.EnsureGet(State, ColorsByState[States.Idle]);
        } else if (StateTarget == StateTargets.Border) {
            BorderColor = ColorsByState.EnsureGet(State, ColorsByState[States.Idle]);
        }
        base.Render();
        Color = color;
        BorderColor = borderColor;
    }
}