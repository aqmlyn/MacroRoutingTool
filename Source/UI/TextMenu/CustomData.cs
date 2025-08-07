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
    
    /// <inheritdoc cref="MenuDataContainer"/>
    public MenuDataContainer() : base() {
        Visible = false;
    }
}