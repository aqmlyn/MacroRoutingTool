using System.Collections.Generic;

namespace Celeste.Mod.MacroRoutingTool.UI;

/// <summary>
/// Once added to a <see cref="TextMenu"/>, the data contained in this object will affect the behavior of the menu or some of its items.<br/>
/// This item is only intended for use in code -- it's not intended to be usable by or visible to players.
/// </summary>
public class MenuDataContainer : TextMenu.Item {
    /// <summary>
    /// Contains data affecting the entire menu.
    /// </summary>
    public List<object> MenuData = [];
    /// <summary>
    /// Contains data affecting individual items of this menu.
    /// </summary>
    public Dictionary<TextMenu.Item, object> ItemData = [];

    /// <summary>
    /// Tries to get this container's item data entry of type <typeparamref name="T"/> for the given <c>item</c> and place it in <c>result</c>.
    /// Returns whether an entry was found.
    /// </summary>
    /// <typeparam name="T">The object type to check for.</typeparam>
    /// <param name="fallback">Optional, value to assign to <c>result</c> if no entry is found.</param>
    public bool TryGetItemData<T>(TextMenu.Item item, out T result, T fallback = default) {
        if (ItemData.TryGetValue(item, out object itemData)) {
            if (itemData is T dataOfTargetType) {
                result = dataOfTargetType;
                return true;
            }
        }
        result = fallback;
        return false;
    }
}

/// <summary>
/// Intended to be added to a <see cref="MenuDataContainer.MenuData"/> to change the <see cref="TextMenu"/> to be
/// able to be displayed with arbitrary position and dimensions and alongside other <see cref="TextMenu"/>s. 
/// </summary>
public partial class MultiDisplayData {
    /// <summary>
    /// Height to add to items' positions when rendering them, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.
    /// </summary>
    public float ScrollOffset;

    /// <summary>
    /// Maximum allowed value of ScrollOffset, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.<br/>
    /// Calculated to prevent scrolling past the point where the last item is fully visible.
    /// </summary>
    public float MaxScrollOffset;

    /// <summary>
    /// Amount by which the scale of each item's visuals (text and images) will be multiplied.
    /// </summary>
    public float ItemScaleMult = 1f;

    /// <summary>
    /// Maximum allowed value of ItemScaleMult.
    /// </summary>
    public float ItemScaleMaxMult = 1f;

    /// <summary>
    /// Combined height of each <c>Visible</c> item plus the vertical <c>ItemSpacing</c> added in between each item,
    /// <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.
    /// </summary>
    public float TotalItemHeight;
}

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
public partial class StickyItemData {
    /// <summary>
    /// Whether this item sticks to the top of the menu's display area.
    /// </summary>
    public bool Top;
    /// <summary>
    /// Whether this item sticks to the bottom of the menu's display area.
    /// </summary>
    public bool Bottom;
}

public partial class HeaderScaleData {
    public float Scale;
}