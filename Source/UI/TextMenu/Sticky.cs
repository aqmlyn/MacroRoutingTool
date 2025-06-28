namespace Celeste.Mod.MacroRoutingTool.UI;

/// <summary>
/// Intended to be added to a member of a <see cref="MenuDataContainer.ItemData"/> to change that item
/// to not scroll past the menu's display area.<br/>
/// Behavior differs from the vanilla <see cref="TextMenu.Item.AboveAll"/> in the following ways:
/// <list type="bullet">
/// <item>If there are non-sticky items before this item that are visible with the current scroll offset,
/// this item will be drawn below them if there's space.</item>
/// <item>This system also allows items to stick to the bottom of the menu, not only the top.</item>
/// </list>
/// </summary>
public class StickyItemData {
    /// <summary>
    /// Whether this item sticks to the top of the menu's display area.
    /// </summary>
    public bool Top;
    /// <summary>
    /// Whether this item sticks to the bottom of the menu's display area.
    /// </summary>
    public bool Bottom;

    //TODO
}