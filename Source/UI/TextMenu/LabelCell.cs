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
        public TextElement Element = new(){BorderThickness = 2f};

        public override float UnrestrictedWidth() => Element?.Measurements.Width ?? base.UnrestrictedWidth();
        public override float UnrestrictedHeight() => Element?.Measurements.Height ?? base.UnrestrictedHeight();
        
        public override void Update() {
            Element?.Update();
            base.Update();
        }
        
        public override void Render(Vector2 position, bool highlighted) {
            if (Element != null) {
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