using Microsoft.Xna.Framework;

namespace Celeste.Mod.MacroRoutingTool.UI;

public partial class TableMenu {
    /// <summary>
    /// Menu item that displays text in a cell of a table and performs an action when <see cref="Input.MenuConfirm"/> is pressed while it's hovered.
    /// </summary>
    public new class Button : Label {
        /// <summary>
        /// Name passed to <see cref="Audio.Play(string)"/> whenever this button is pressed.
        /// </summary>
        public string AudioOnPress = "event:/ui/main/button_select";
        
        /// <inheritdoc cref="Button"/>
        public Button() : base() {
            Selectable = true;
        }

        public override void ConfirmPressed() {
            if (!string.IsNullOrEmpty(AudioOnPress)) {
                Audio.Play(AudioOnPress);
            }
            SelectWiggler.Start();
            base.ConfirmPressed();
        }
    }
}