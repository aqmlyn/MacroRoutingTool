using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static class TextMenuUtils {
    public static TextMenu AddRange(this TextMenu menu, IEnumerable<TextMenu.Item> items) {
        foreach (TextMenu.Item item in items) {
            menu.Add(item);
        }
        return menu;
    }

    public static MenuDataContainer EnsureDataContainer(this TextMenu menu) {
        MenuDataContainer dataContainer = (MenuDataContainer)menu.Items.FirstOrDefault(item => item is MenuDataContainer, null);
        if (dataContainer == null) {
            dataContainer = new();
            menu.Add(dataContainer);
        }
        return dataContainer;
    }

    /// <summary>
    /// If the list contains a <see cref="MenuDataContainer"/> and any of its items
    /// are of type <typeparamref name="T"/>, <c>result</c> will be set to the first one. Otherwise, this will return false.
    /// </summary>
    /// <typeparam name="T">The object type to check for.</typeparam>
    /// <param name="fallback">Optional, value to assign to <c>result</c> if no entry is found.</param>
    public static bool TryGetData<T>(this TextMenu menu, out T result, T fallback = default) {
        foreach(TextMenu.Item item in menu.Items) {
            if (item is MenuDataContainer dataItem) {
                foreach (object dataset in dataItem.MenuData) {
                    if (dataset is T dataOfTargetType) {
                        result = dataOfTargetType;
                        return true;
                    }
                }
            }
        }
        result = fallback;
        return false;
    }

    /// <summary>
    /// Whether the given text menu's item list contains a <see cref="MenuDataContainer"/>
    /// whose data list contains an item of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The object type to check for.</typeparam>
    /// <param name="fallback">Optional, value to assign to <c>result</c> if no entry is found.</param>
    public static bool DataContains<T>(this TextMenu menu, T fallback = default) => menu.TryGetData(out _, fallback);

    public static MethodInfo TextBoxSetText = typeof(TextMenuExt.TextBox).GetMethod("set_" + nameof(TextMenuExt.TextBox.Text), BindingFlags.NonPublic | BindingFlags.Instance);
    public static void SetText(this TextMenuExt.TextBox textBox, string text) {
        TextBoxSetText.Invoke(textBox, [text]);
    }
}