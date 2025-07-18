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

        public GetterEventProperty<string> State = new(){Value = UITextElement.States.Idle};

        public override float UnrestrictedWidth() => Element?.Measurements.Width ?? base.UnrestrictedWidth();
        public override float UnrestrictedHeight() => Element?.Measurements.Height ?? base.UnrestrictedHeight();
        
        public override void Update() {
            Element?.Update();
            base.Update();
        }
        
        public override void Render(Vector2 position, bool highlighted) {
            if (Element != null) {
                if (UseDefaultColors) {
                    Element.ColorsByState[UITextElement.States.Hovered] = Container?.HighlightColor ?? Element.ColorsByState[UITextElement.States.Hovered];
                    State.Value = highlighted ? UITextElement.States.Hovered : UITextElement.States.Idle;
                }
                Element.State = State.Value;
                Element.Position = position;
                Element.Justify.X = JustifyX ?? Element.Justify.X;
                Element.Justify.Y = JustifyY ?? Element.Justify.Y;
                Element.MaxWidth = LeftWidth();
                Element.MaxHeight = Height();
                Element.Render();
            }
            base.Render(position, highlighted);
        }
    }
}