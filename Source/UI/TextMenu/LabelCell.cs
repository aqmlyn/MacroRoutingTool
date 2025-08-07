using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.MacroRoutingTool.UI;

public partial class TableMenu {
    /// <summary>
    /// Menu item that displays a <see cref="TextElement"/> in a cell of a table.
    /// </summary>
    public class Label : CellItem {
        /// <summary>
        /// The <see cref="TextElement"/> being displayed by this item.
        /// </summary>
        public UITextElement Element = new(){BorderThickness = 2f};

        /// <summary>
        /// If true, the text will use this item's <see cref="TextMenu.Item.Container"/>'s <see cref="TextMenu.HighlightColor"/>
        /// while this label appears hovered and <see cref="Color.White"/> while it doesn't.
        /// </summary>
        public bool UseDefaultColors = true;

        /// <summary>
        /// If true, <see cref="State"/> will automatically be set according to the second argument passed to <see cref="Render"/>. 
        /// </summary>
        public bool AutoState = true;

        /// <summary>
        /// This label's current state, which controls its appearance.
        /// </summary>
        public GetterEventProperty<string> State = new() { Value = UITextElement.States.Idle };

        /// <summary>
        /// Assign this label to the given item. This label will appear hovered when any item it is assigned to is actually hovered.
        /// </summary>
        /// <param name="item">The item to assign this label to.</param>
        /// <exception cref="ArgumentNullException"/>
        public void AssignTo(Item item) {
            if (item == null) { throw new ArgumentNullException(nameof(item)); }
            else {
                AutoState = false;
                item.OnEnter += () => State.Value = UITextElement.States.Hovered;
                item.OnLeave += () => State.Value = UITextElement.States.Idle;
            }
        }

        public override float UnrestrictedWidth() => Element?.Measurements.Width ?? base.UnrestrictedWidth();
        public override float UnrestrictedHeight() => Element?.Measurements.Height ?? base.UnrestrictedHeight();
        
        public override void Update() {
            Element?.Update();
            base.Update();
        }
        
        public override void Render(Vector2 position, bool hovered) {
            if (Element != null) {
                if (UseDefaultColors) { Element.ColorsByState[UITextElement.States.Hovered] = Container?.HighlightColor ?? Element.ColorsByState[UITextElement.States.Hovered]; }
                if (AutoState) { State.Value = hovered ? UITextElement.States.Hovered : UITextElement.States.Idle; }
                Element.State = State.Value;
                Element.Position.X = position.X;
                Element.Position.Y = position.Y;
                Element.Justify.X = JustifyX ?? Element.Justify.X;
                Element.Justify.Y = JustifyY ?? Element.Justify.Y;
                Element.MaxWidth = LeftWidth();
                Element.MaxHeight = Height();
                Element.Render();
            }
            base.Render(position, hovered);
        }
    }
}