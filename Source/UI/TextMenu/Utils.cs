using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;

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

    public class TextElement {
        public TextMenu.Item Container = null;
        public string Text = "";
        public Vector2 Position = Vector2.Zero;
        public Vector2 Justify = Vector2.Zero;
        public Vector2 Scale = Vector2.One;
        public Color Color = Color.White;
        public float? BorderThickness = null;
        public Color BorderColor = Color.Black;
        public float? DropShadowOffset = null;
        public Color DropShadowColor = Color.DarkSlateBlue;

        public void Render() {
            if (DropShadowOffset != null) {
                ActiveFont.DrawEdgeOutline(Text, Position, Justify, Scale, Color, (float)DropShadowOffset, DropShadowColor, BorderThickness ?? 0, BorderColor);
            } else if (BorderThickness != null) {
                ActiveFont.DrawOutline(Text, Position, Justify, Scale, Color, (float)BorderThickness, BorderColor);
            } else {
                ActiveFont.Draw(Text, Position, Justify, Scale, Color);
            }
        }
    }

    public static MethodInfo TextBoxSetText = typeof(TextMenuExt.TextBox).GetMethod("set_" + nameof(TextMenuExt.TextBox.Text), BindingFlags.NonPublic | BindingFlags.Instance);
    public static void SetText(this TextMenuExt.TextBox textBox, string text) {
        TextBoxSetText.Invoke(textBox, [text]);
    }
}