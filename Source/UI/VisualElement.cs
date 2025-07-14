using System;
using System.Collections.Generic;
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
        /// The text that will be rendered. Newlines might have been inserted into the original text to fit as much of it as
        /// possible in the space provided. 
        /// </summary>
        public string Text = "";
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

        _measurements.Text = Text;

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
        bool canWrap = MaxHeight >= Font.LineHeight * 2f;
        if (canShrink && canWrap) {
            
        } else {
            //adapted from Monocle.PixelFontSize.Measure(string)
            int lineCount = 1;
            if (canWrap) {

            }
            _measurements.Width = 0f;
            float maxWidth = MaxWidth * (canShrink ? MinScale : 1f);
            bool needsTruncated = false;
            Stack<float> charWidths = new();
            for (int i = 0; i < _measurements.Text.Length; i++) {
                if (_measurements.Text[i] == '\n') {
                    _measurements.Text = _measurements.Text[..(i - 1)];
                    needsTruncated = true;
                    break;
                }
                if (Font.Characters.TryGetValue(_measurements.Text[i], out var ch)) {
                    var ogWidth = _measurements.Width;
                    _measurements.Width += ch.XAdvance;
                    if (i < _measurements.Text.Length - 1 && ch.Kerning.TryGetValue(_measurements.Text[i + 1], out var kerning)) {
                        _measurements.Width += kerning;
                    }
                    if (_measurements.Width > maxWidth) {
                        _measurements.Text = _measurements.Text[..(i - 1)];
                        _measurements.Width = ogWidth;
                        needsTruncated = true;
                        break;
                    } else {
                        charWidths.Push(_measurements.Width - ogWidth);
                    }
                }
            }
            if (needsTruncated) {
                float truncWidth = Font.Measure(OversizeTruncate).X;
                while (_measurements.Width + truncWidth > maxWidth && charWidths.TryPop(out var charWidth)) {
                    _measurements.Text = _measurements.Text[..^1];
                    _measurements.Width -= charWidth;
                }
                _measurements.Text += OversizeTruncate;
            }
            if (canShrink) {
                var spaceFactor = MaxWidth / _measurements.Width;
                _measurements.ScaleFactor *= spaceFactor;
                _measurements.Scale *= spaceFactor;
            }
            _measurements.Height = Font.LineHeight * lineCount;
        }
    }

    public override void Render() {
        var ogSpriteBatch = Draw.SpriteBatch;
        Draw.SpriteBatch = SpriteBatch;

        var msrmts = Measurements;
        if (DropShadowOffset != null) {
            Font.DrawEdgeOutline(msrmts.Text, Position, Justify, msrmts.Scale, Color, (float)DropShadowOffset, DropShadowColor, (BorderThickness ?? 0f) * msrmts.ScaleFactor, BorderColor);
        } else if (BorderThickness != null) {
            Font.DrawOutline(msrmts.Text, Position, Justify, msrmts.Scale, Color, (float)BorderThickness * msrmts.ScaleFactor, BorderColor);
        } else {
            Font.Draw(msrmts.Text, Position, Justify, msrmts.Scale, Color);
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