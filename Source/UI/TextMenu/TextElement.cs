using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

/// <summary>
/// An item that draws text onscreen.
/// </summary>
public class TextElement {
    /// <summary>
    /// <see cref="ValueHandler"/> fields that require knowing the type of value a handler is handling.<br/>
    /// This one's holding a <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type of value this handler is handling.</typeparam>
    public class ValueHandler<T> {
        /// <summary>
        /// Returns <see cref="Value"/>. 
        /// </summary>
        public Func<T> ValueGetter;
        /// <summary>
        /// Called by <see cref="ValueHandler.SetValueFromString"/> to set <see cref="Value"/> based on the given string. 
        /// </summary>
        public Func<string, T> ValueParser;
        /// <summary>
        /// The current value.
        /// </summary>
        public T Value;
        /// <summary>
        /// Called by <see cref="ValueHandler.ValueToString"/> to get a string representation of <see cref="Value"/> for display.
        /// </summary>
        public Func<T, string> ValueToString;
    }

    /// <summary>
    /// <see cref="ValueHandler"/> fields that can be used without knowing the type of value a handler is handling.
    /// </summary>
    public class ValueHandler {
        /// <summary>
        /// Sets <see cref="Value"/> based on the given string.
        /// </summary>
        public Action<string> SetValueFromString;
        /// <summary>
        /// The current value.
        /// </summary>
        public object Value {
            get => GetValue?.Invoke();
            set => SetValue?.Invoke(value);
        }
        /// <summary>
        /// Called when getting <see cref="Value"/>. 
        /// </summary>
        public Func<object> GetValue;
        /// <summary>
        /// Called when setting <see cref="Value"/>. 
        /// </summary>
        public Action<object> SetValue;
        /// <summary>
        /// Gets a string representation of <see cref="Value"/> for display.
        /// </summary>
        public Func<string> ValueToString;

        /// <summary>
        /// Modifies the delegates of the typeless form of a <see cref="Binding"/> based on the delegates of the typed form.<br/>
        /// Makes it easier for menus to contain list items with different value types.
        /// </summary>
        /// <typeparam name="T">Type of value this handler is handling.</typeparam>
        /// <param name="handler"><see cref="ValueHandler{T}"/> whose delegates to use.</param>
        public void Bind<T>(ValueHandler<T> handler) {
            GetValue = () => handler.Value = handler.ValueGetter == null ? handler.Value : handler.ValueGetter();
            SetValue = val => handler.Value = (T)val;
            SetValueFromString = str => Value = handler.ValueParser == null ? default : handler.ValueParser(str);
            ValueToString = () => handler.ValueToString == null ? Value?.ToString() : handler.ValueToString((T)Value);
        }
    }

    /// <summary>
    /// The object that handles continuously getting the text to display in this <see cref="TextElement"/>. 
    /// </summary>
    public ValueHandler Binding = new();
    /// <summary>
    /// Backing field for <see cref="Value"/>. 
    /// </summary>
    public object _value;
    /// <summary>
    /// The value this <see cref="TextElement"/> displays a text representation of.
    /// </summary>
    public object Value {
        get {return _value = Binding.GetValue?.Invoke() ?? _value;}
        set {_value = value;}
    }
    /// <summary>
    /// The text this <see cref="TextElement"/> displays, which is a text representation of <see cref="Value"/>. 
    /// </summary>
    public string Text {
        get => Binding.ValueToString?.Invoke() ?? _value.ToString();
        set {Binding.SetValueFromString?.Invoke(value);}
    }

    /// <summary>
    /// Origin about which the text is to be rendered. Units vary depending on the <see cref="Microsoft.Xna.Framework.Graphics.SpriteBatch.spriteMatrixTransform"/>
    /// assigned in the most recent <see cref="Microsoft.Xna.Framework.Graphics.SpriteBatch.Begin()"/> call.  
    /// </summary>
    public Vector2 Position = Vector2.Zero;
    /// <summary>
    /// Where the origin is relative to where the text is to be rendered. (0, 0) is top left, (1, 1) is bottom right.
    /// </summary>
    public Vector2 Justify = Vector2.Zero;
    /// <summary>
    /// Size multiplier for the text.
    /// </summary>
    public Vector2 Scale = Vector2.One;
    /// <summary>
    /// Color of the text.
    /// </summary>
    public Color Color = Color.White;
    /// <summary>
    /// Thickness in pixels of the border around the text, or null to not draw a border.
    /// </summary>
    public float? BorderThickness = null;
    /// <summary>
    /// Color of the border around the text.
    /// </summary>
    public Color BorderColor = Color.Black;
    /// <summary>
    /// Thickness in pixels of the drop shadow under the text, or null to not draw a drop shadow.
    /// </summary>
    public float? DropShadowOffset = null;
    /// <summary>
    /// Color of the drop shadow around the text.
    /// </summary>
    public Color DropShadowColor = Color.DarkSlateBlue;
    /// <summary>
    /// Font of the text. Must be present in <see cref="Fonts.loadedFonts"/>. See
    /// <see href="https://github.com/EverestAPI/Resources/wiki/Adding-Custom-Dialogue#adding-a-completely-custom-font-for-the-game">this guide</see>
    /// to get Everest to load custom fonts.
    /// </summary>
    public PixelFontSize Font = ActiveFont.FontSize;

    /// <summary>
    /// Whether to always draw the text at the same size, regardless of what <see cref="Camera"/>'s <see cref="Camera.Zoom"/> is.<br/>
    /// To have any effect, this requires <see cref="Camera"/> to be set to the camera from which the current
    /// <see cref="Microsoft.Xna.Framework.Graphics.SpriteBatch.spriteMatrixTransform"/> got its value.
    /// </summary>
    public bool IgnoreCameraZoom = false;
    /// <summary>
    /// The camera from which the current <see cref="Microsoft.Xna.Framework.Graphics.SpriteBatch.spriteMatrixTransform"/> got its value.
    /// </summary>
    public Camera Camera = null;

    /// <summary>
    /// Render the text as the other fields of this element specify.
    /// </summary>
    public virtual void Render() {
        Vector2 scale = new(Scale.X, Scale.Y);
        if (IgnoreCameraZoom) {scale /= Camera.Zoom;}
        if (DropShadowOffset != null) {
            Font.DrawEdgeOutline(Text, Position, Justify, scale, Color, (float)DropShadowOffset, DropShadowColor, (BorderThickness ?? 0) / (IgnoreCameraZoom ? Camera.Zoom : 1f), BorderColor);
        } else if (BorderThickness != null) {
            Font.DrawOutline(Text, Position, Justify, scale, Color, (float)BorderThickness / (IgnoreCameraZoom ? Camera.Zoom : 1f), BorderColor);
        } else {
            Font.Draw(Text, Position, Justify, scale, Color);
        }
    }

    /// <summary>
    /// Keep the value this text is based on updated.
    /// </summary>
    public void Update() {
        //just need to call Value's getter to keep it up to date
        object _ = Value;
    }
}

/// <summary>
/// An item that draws text onscreen. It can store a state and renders in different colors for each possible state.
/// </summary>
public class UITextElement : TextElement
{
    /// <summary>
    /// List of common states used by basic UI elements.
    /// </summary>
    public class States
    {
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
        {States.Hovered, TextMenu.HighlightColorA},
        {States.Selected, TextMenu.HighlightColorB}
    };

    public override void Render()
    {
        Color = ColorsByState.EnsureGet(State, ColorsByState[States.Idle]);
        base.Render();
    }
}