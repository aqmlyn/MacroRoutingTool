using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static class ListItemInfo {
    public static bool PrevHoveredRight = false;
}

public class ListItem : TextMenu.Item {
    public class SidePart {
        public ListItem Container;

        public object Element = new();

        //it is best practice to make backing fields private. however! the next time i'm writing an IL hook and i find out some
        //random vanilla field is non-public and it takes me half an hour to realize it bc the publicizer makes it look public,
        //im going to snap! so i'm making every single field in this mod public on principle, no matter the consequences :3
        public bool _editable = false;
        public bool Editable {
            get => _editable;
            set {
                _editable = value;
                Container?.PrepareForEditing();
            }
        }

        public class TypedValueHandler<T> {
            public Func<T> ValueGetter;
            public Func<string, T> ValueParser;
            public T Value;
            public Func<T, string> ValueToString = value => value?.ToString() ?? "";
        }

        public class ValueHandler {
            public Func<object> ValueGetter;
            public Func<string, object> ValueParser;
            public object Value {
                get => GetValue?.Invoke();
                set => SetValue?.Invoke(value);
            }
            public Func<object> GetValue;
            public Action<object> SetValue;
            public Func<object, string> ValueToString;

            public void HandleUsing<T>(TypedValueHandler<T> handler) {
                GetValue = () => handler.Value = handler.ValueGetter == null ? handler.Value : handler.ValueGetter();
                SetValue = val => handler.Value = (T)val;
                ValueParser = str => handler.ValueParser == null ? null : handler.ValueParser(str);
                ValueToString = val => handler.ValueToString?.Invoke((T)val);
            }
        }
        public ValueHandler Handler = new();

        public object _value;
        public object Value {
            get {return _value = Handler.ValueGetter?.Invoke() ?? _value;}
            set {_value = value;}
        }
        public string ValueString => Handler.ValueToString?.Invoke(Value) ?? "";
    }

    public SidePart Left = new();
    public SidePart Right = new();
    public float LeftWidthPortion = 0.5f;
    public Color LeftIdleColor;
    public Color RightIdleColor;

    public TextMenuExt.TextBox HoveredTextbox = null;

    public float MinSeparation = GraphViewer.MarginH;

    public ListItem(bool leftEditable, bool rightEditable) : base() {
        OnEnter += DoOnEnter;
        OnLeave += DoOnLeave;

        Left.Container = Right.Container = this;
        Left.Editable = leftEditable;
        Right.Editable = rightEditable;
    }

    public ListItem() : this(false, false) {}

    public int PrevSelection;

    public void PrepareForEditing() {
        if (Container == null) return; //the Editable setter is called from the ctor, which runs before the item is added to a TextMenu, so this method can be called while the item has no Container

        Selectable = Left.Editable || Right.Editable;

        foreach (SidePart side in new SidePart[] {Left, Right}) {
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
                    side.Value = side.Handler.ValueParser?.Invoke(tb.Text);
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

    public Action<ListItem> OnAdded;

    public override void Added()
    {
        base.Added();
        Container.InnerContent = TextMenu.InnerContentMode.TwoColumn;
        PrevSelection = Container.Selection;
        PrepareForEditing();
        OnAdded?.Invoke(this);
        if (!Left.Editable) {
            LeftIdleColor = ((TextMenuUtils.TextElement)Left.Element).Color;
        }
        if (!Right.Editable) {
            RightIdleColor = ((TextMenuUtils.TextElement)Right.Element).Color;
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
            leftelem.Color = highlighted ? Container.HighlightColor : LeftIdleColor;
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
            rightelem.Color = highlighted ? Container.HighlightColor : RightIdleColor;
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
        foreach (SidePart side in new SidePart[] {Left, Right}) {
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