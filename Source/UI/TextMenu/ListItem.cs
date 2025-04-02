using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static class ListItemInfo {
    public static bool PrevHoveredRight = false;
}

/// <summary>
/// A two-column text menu item. Each column displays a value that updates in real time and optionally can be edited using a <see cref="TextMenuExt.TextBox"/>. 
/// </summary>
public class ListItem : TextMenu.Item {
    /// <summary>
    /// An item which is part of a <see cref="ListItem"/>. 
    /// </summary>
    public class Part {
        /// <summary>
        /// The <see cref="ListItem"/> of which this is a part.
        /// </summary>
        public ListItem Container;

        /// <summary>
        /// The object used to display this part. Currently, it may be:
        /// <list type="bullet">
        /// <item>a <see cref="TextMenuExt.TextBox"/> if this part is <see cref="Editable"/>.</item>  
        /// <item>a <see cref="TextMenuUtils.TextElement"/> if this part is not <see cref="Editable"/>.</item>
        /// </list>
        /// </summary>
        public object Element = new();

        //it is best practice to make backing fields private. however! the next time i'm writing an IL hook and i find out some
        //random vanilla field is non-public and it takes me half an hour to realize it bc the publicizer makes it look public,
        //im going to snap! so i'm making every single field in this mod public on principle, no matter the consequences :3
        public bool _editable = false;
        /// <summary>
        /// Whether this part can be edited by the user. If so, <see cref="Element"/> will be a <see cref="TextMenuExt.TextBox"/>
        /// which the user can type into, and this part's <see cref="Handler"/>'s <see cref="ValueHandler.SetValueFromString"/>
        /// will be used to modify this part's value based on what the user typed.
        /// </summary>
        public bool Editable {
            get => _editable;
            set {
                _editable = value;
                Container?.PrepareForEditing();
            }
        }

        /// <summary>
        /// Stores the color that a part which is not <see cref="Editable"/> should be while it's not hovered.
        /// </summary>
        public Color IdleColor = Color.White;

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
            public Func<object, string> ValueToString;

            /// <summary>
            /// Modifies the delegates of the typeless form of a <see cref="ValueHandler"/> based on the delegates of the typed form.<br/>
            /// Makes it easier for menus to contain list items with different value types.
            /// </summary>
            /// <typeparam name="T">Type of value this handler is handling.</typeparam>
            /// <param name="handler"><see cref="ValueHandler{T}"/> whose delegates to use.</param>
            public void Bind<T>(ValueHandler<T> handler) {
                GetValue = () => handler.Value = handler.ValueGetter == null ? handler.Value : handler.ValueGetter();
                SetValue = val => handler.Value = (T)val;
                SetValueFromString = str => Value = handler.ValueParser == null ? default : handler.ValueParser(str);
                ValueToString = val => handler.ValueToString == null ? val?.ToString() : handler.ValueToString((T)val);
            }
        }
        /// <summary>
        /// The <see cref="ValueHandler"/> being used to handle this part's value.
        /// </summary>
        public ValueHandler Handler = new();

        public object _value;
        /// <summary>
        /// This part's current value.
        /// </summary>
        public object Value {
            get {return _value = Handler.GetValue?.Invoke() ?? _value;}
            set {_value = value;}
        }
        /// <summary>
        /// Gets a string representation of this part's <see cref="Value"/> for display.
        /// </summary>
        public string ValueString => Handler.ValueToString?.Invoke(Value) ?? "";
    }

    //TODO after MVP release, consider having a list of Parts rather than only a Left and Right
    //(will require refactoring LeftWidthPortion, the non-default constructor, and rendering)

    /// <summary>
    /// <see cref="Part"/> in the left column of this <see cref="ListItem"/>. 
    /// </summary>
    public Part Left = new();
    /// <summary>
    /// <see cref="Part"/> in the right column of this <see cref="ListItem"/>.  
    /// </summary>
    public Part Right = new();

    /// <summary>
    /// The maximum width of <see cref="Left"/> will be the containing <see cref="TextMenu"/>'s width multiplied by this value.<br/>
    /// The maximum width of <see cref="Right"/> will be the remaining width in the <see cref="TextMenu"/>. 
    /// </summary>
    public float LeftWidthPortion = 0.5f;

    /// <summary>
    /// The <see cref="TextMenuExt.TextBox"/> in this <see cref="ListItem"/> that is currently hovered, if any.
    /// </summary>
    public TextMenuExt.TextBox HoveredTextbox = null;

    /// <summary>
    /// Minimum horizontal separation between each part of this <see cref="ListItem"/>, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.  
    /// </summary>
    public float MinSeparation = GraphViewer.MarginH;

    /// <summary>
    /// This constructor allows specifying whether each <see cref="Part"/> of this item is to be <see cref="Part.Editable"/>.
    /// </summary>
    /// <param name="leftEditable">Whether the <see cref="Left"/> <see cref="Part"/> of this item is to be <see cref="Part.Editable"/>.</param>
    /// <param name="rightEditable">Whether the <see cref="Right"/> <see cref="Part"/> of this item is to be <see cref="Part.Editable"/>.</param>
    public ListItem(bool leftEditable, bool rightEditable) : base() {
        OnEnter += DoOnEnter;
        OnLeave += DoOnLeave;

        Left.Container = Right.Container = this;
        Left.Editable = leftEditable;
        Right.Editable = rightEditable;
    }

    /// <summary>
    /// The default constructor assumes no <see cref="Part"/>s of this item should be <see cref="Part.Editable"/>. 
    /// </summary>
    public ListItem() : this(false, false) {}

    /// <summary>
    /// Initializes each <see cref="Part"/>'s <see cref="Part.Element"/> using new objects based on whether each side is <see cref="Part.Editable"/>.
    /// </summary>
    public void PrepareForEditing() {
        if (Container == null) return; //the Editable setter is called from the ctor, which runs before the item is added to a TextMenu, so this method can be called while the item has no Container

        Selectable = Left.Editable || Right.Editable;

        foreach (Part side in new Part[] {Left, Right}) {
            if (side.Editable) {
                float textScale = Container.TryGetData(out MultiDisplayData data) ? data.ItemScaleMult : 1f;
                TextMenuExt.TextBox textbox = new()
                {
                    Container = Container,
                    Selectable = false,
                    WidthScale = (side == Right ? 1f - LeftWidthPortion : LeftWidthPortion) - MinSeparation / Container.Width / 2f,
                    TextScale = Vector2.One * 0.9f * textScale,
                    TextPadding = Vector2.One * 0.1f * textScale,
                    TextJustify = new Vector2(0f, 0.5f)
                };
                textbox.OnTextInputCharActions['\r' /*enter*/] = tb => {
                    tb.StopTyping();
                    side.Handler.SetValueFromString?.Invoke(tb.Text);
                };
                side.Element = textbox;
            } else {
                TextMenuUtils.TextElement text = new() {
                    Container = this,
                    BorderThickness = 2f,
                    Justify = new Vector2(side == Right ? 1f : 0f, 0.5f)
                };
                side.Element = text;
            }
        }
    }

    /// <summary>
    /// Called when adding this <see cref="ListItem"/> to a <see cref="TextMenu"/>. 
    /// </summary>
    public Action<ListItem> OnAdded;

    public override void Added()
    {
        base.Added();
        Container.InnerContent = TextMenu.InnerContentMode.TwoColumn;
        PrepareForEditing();
        OnAdded?.Invoke(this);
        foreach (Part part in new Part[]{Left, Right}) {
            if (!part.Editable) {
                part.IdleColor = ((TextMenuUtils.TextElement)part.Element).Color;
            }
        }
    }

    public override void ConfirmPressed()
    {
        base.ConfirmPressed();
        HoveredTextbox?.StartTyping();
    }

    public override void LeftPressed()
    {
        base.LeftPressed();
        if (Left.Editable) {
            HoveredTextbox = (TextMenuExt.TextBox)Left.Element;
        }
    }

    public override void RightPressed()
    {
        base.RightPressed();
        if (Right.Editable) {
            HoveredTextbox = (TextMenuExt.TextBox)Right.Element;
        }
    }

    //TODO UneditableScale ignores LeftWidthPortion
    /// <summary>
    /// Returns the scale applied to each <see cref="Part"/> in this <see cref="ListItem"/> if no parts are <see cref="Part.Editable"/>. 
    /// </summary>
    public float UneditableScale() {
        return Container.TryGetData(out MultiDisplayData data) ? Math.Min(data.ItemScaleMult, Container.Width / (ActiveFont.Measure(((TextMenuUtils.TextElement)Left.Element).Text).X + ActiveFont.Measure(((TextMenuUtils.TextElement)Right.Element).Text).X + MinSeparation)) : 1f;
    }

    public override float Height()
    {
        //don't use Measure().Scale here -- want to maintain a consistent height for this item even when the text is downscaled
        return ActiveFont.LineHeight * (Container.TryGetData(out MultiDisplayData data) ? data.ItemScaleMult : 1f);
    }

    public override void Render(Vector2 position, bool highlighted)
    {
        base.Render(position, highlighted);

        Vector2 dimensions = new(Container.Width, Height());
        Vector2 justifyOffset = new(0f, -0.5f);
        //the Y position passed to an item's Render method is intended to be its vertical center. to allow setting a different justify,
        //i think it's simpler to offset the justify value to account for this, not the position

        if (!Left.Editable && !Right.Editable) {
            TextMenuUtils.TextElement leftelem = (TextMenuUtils.TextElement)Left.Element;
            TextMenuUtils.TextElement rightelem = (TextMenuUtils.TextElement)Right.Element;
            leftelem.Scale = rightelem.Scale = Vector2.One * UneditableScale();
            leftelem.Position = position + (dimensions * (leftelem.Justify + justifyOffset));
            leftelem.Render();
            rightelem.Position = position + (dimensions * (rightelem.Justify + justifyOffset));
            rightelem.Render();
            return;
        }

        Container.TryGetData(out MultiDisplayData data, null);
        float scale = data?.ItemScaleMult ?? 1f;
        if (Left.Editable) {
            TextMenuExt.TextBox leftelem = (TextMenuExt.TextBox)Left.Element;
            if (highlighted && HoveredTextbox == leftelem) {Draw.Rect(position.X - Container.ItemSpacing / 2, position.Y - leftelem.Height() / 2 - Container.ItemSpacing / 2, Container.Width * leftelem.WidthScale + Container.ItemSpacing, leftelem.Height() + Container.ItemSpacing, Container.HighlightColor);}
            float unscaledWidth = ActiveFont.Measure(leftelem.Text + "_").X + UIHelpers.WidestCharWidth;
            float adjustedScale = leftelem.Width / unscaledWidth;
            leftelem.TextScale = Vector2.One * Math.Min(scale * 0.9f, adjustedScale);
            leftelem.Render(new Vector2(position.X, position.Y - leftelem.Height() / 2f), false);
        } else {
            TextMenuUtils.TextElement leftelem = (TextMenuUtils.TextElement)Left.Element;
            leftelem.Scale = Vector2.One * Math.Min(scale, Container.Width * LeftWidthPortion - MinSeparation / Container.Width / 2);
            leftelem.Position = position + (dimensions * (leftelem.Justify + justifyOffset));
            leftelem.Color = highlighted ? Container.HighlightColor : Left.IdleColor;
            leftelem.Render();
        }
        if (Right.Editable) {
            TextMenuExt.TextBox rightelem = (TextMenuExt.TextBox)Right.Element;
            if (highlighted && HoveredTextbox == rightelem) {Draw.Rect(position.X + Container.Width * LeftWidthPortion + MinSeparation / 2 - Container.ItemSpacing / 2, position.Y - rightelem.Height() / 2 - Container.ItemSpacing / 2, Container.Width * rightelem.WidthScale + Container.ItemSpacing, rightelem.Height() + Container.ItemSpacing, Container.HighlightColor);}
            float unscaledWidth = ActiveFont.Measure(rightelem.Text + "_").X + UIHelpers.WidestCharWidth;
            float adjustedScale = rightelem.Width / unscaledWidth;
            rightelem.TextScale = Vector2.One * Math.Min(scale * 0.9f, adjustedScale);
            rightelem.Render(new Vector2(position.X + Container.Width * LeftWidthPortion + MinSeparation / 2, position.Y - rightelem.Height() / 2f), false);
        } else {
            TextMenuUtils.TextElement rightelem = (TextMenuUtils.TextElement)Right.Element;
            rightelem.Scale = Vector2.One * Math.Min(scale, Container.Width * (1f - LeftWidthPortion) - MinSeparation / Container.Width / 2);
            rightelem.Position = position + (dimensions * (rightelem.Justify + justifyOffset));
            rightelem.Color = highlighted ? Container.HighlightColor : Right.IdleColor;
            rightelem.Render();
        }
    }

    public void DoOnEnter() {
        HoveredTextbox = 
            Left.Editable && Right.Editable ? (ListItemInfo.PrevHoveredRight ? (TextMenuExt.TextBox)Right.Element : (TextMenuExt.TextBox)Left.Element)
          : Left.Editable ? (TextMenuExt.TextBox)Left.Element
          : Right.Editable ? (TextMenuExt.TextBox)Right.Element
          : null;
    }

    public void DoOnLeave() {
        if (Left.Editable && Right.Editable) {
            ListItemInfo.PrevHoveredRight = HoveredTextbox == (TextMenuExt.TextBox)Right.Element;
        }
    }

    public override void Update()
    {
        base.Update();
        if (Input.ESC.Pressed && (HoveredTextbox?.Typing ?? false)) {
            if (HoveredTextbox.Text.Length > 0) {
                HoveredTextbox.ClearText();
            }
            HoveredTextbox.StopTyping();
        }
        foreach (Part side in new Part[] {Left, Right}) {
            if (side.Editable) {
                TextMenuExt.TextBox elem = (TextMenuExt.TextBox)side.Element;
                elem.Update();
                if (!elem.Typing) {
                    elem.SetText(side.ValueString);
                }
            } else {
                TextMenuUtils.TextElement elem = (TextMenuUtils.TextElement)side.Element;
                elem.Text = side.ValueString;
            }
        }
    }
}