using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    public class MenuCreator {
        public int? InitialFor = null;
        public TextMenu CreatorFor;
        public Func<MenuCreator, List<TextMenu.Item>> OnCreate;
        public Action<MenuCreator> AfterCreate;
        public void Add(TextMenu.Item item) {
            if (CreatorFor != null) {
                item.Container = CreatorFor;
                CreatorFor.Add(item);
            }
        }
        public TextMenu Create() {
            var items = OnCreate?.Invoke(this) ?? [];
            foreach (var item in items) {
                Add(item);
            }
            AfterCreate?.Invoke(this);
            return CreatorFor;
        }
    }

    public class MenuPart {
        public TextMenu Container;
        public List<TextMenu.Item> Items = [];
        public Func<MenuPart, List<TextMenu.Item>> Creator;

        public void Add(TextMenu.Item item) {
            if (Container != null) {
                item.Container = Container;
                Container.Add(item);
                Items.Add(item);
            }
        }

        /// <summary>
        /// Call <see cref="Creator"/> and add the resulting items to the given <see cref="TextMenu"/>.<br/>
        /// By default, this appends the items to the end of the menu, but a custom delegate can be supplied to add them elsewhere.
        /// </summary>
        /// <param name="addTo">The <see cref="TextMenu"/> to add items to.</param>
        /// <param name="adder">Optional custom delegate that receives this <see cref="MenuPart"/> and the list
        /// of <see cref="TextMenu.Item"/>s. If supplied, this is expected to add the items to the menu somehow.</param>
        /// <returns><paramref name="addTo"/>.</returns>
        public TextMenu Create(TextMenu addTo, Action<MenuPart, List<TextMenu.Item>> adder = null) {
            if (addTo == null) {
                //TODO log warning
            } else {
                Container = addTo;
                var items = Creator?.Invoke(this) ?? [];
                adder ??= (self, items) => {
                    foreach (var item in items) {
                        self.Add(item);
                    }
                };
                adder.Invoke(this, items);
            }
            return addTo;
        }

        /// <summary>
        /// Call <see cref="Creator"/> and add the resulting items to the given <see cref="TextMenu"/>'s item list at the specified index.<br/>
        /// </summary>
        /// <param name="addTo">The <see cref="TextMenu"/> to add items to.</param>
        /// <param name="index">Index at which the items are to be added to the menu's item list.</param>
        /// <returns><paramref name="addTo"/>.</returns>
        public TextMenu Create(TextMenu addTo, int index) {
            index = Math.Clamp(index, -addTo.Items.Count, addTo.Items.Count);
            if (index < 0) {index = addTo.Items.Count + index;}
            Create(addTo, (self, items) => {
                if (index == addTo.Items.Count) {
                    foreach (var item in items) {
                        addTo.Add(item);
                    }
                } else {
                    for (int i = 0; i < items.Count; i++) {
                        var item = items[i];
                        addTo.Add(item);
                        addTo.Items.Remove(item);
                        addTo.Items.Insert(index + i, items[i]);
                    }
                }
            });
            return addTo;
        }

        /// <summary>
        /// Call <see cref="Creator"/> and add the resulting items to the given <see cref="TextMenu"/>'s item list adjacent to the specified item.
        /// </summary>
        /// <param name="addTo">The <see cref="TextMenu"/> to add items to.</param>
        /// <param name="addAt">The <see cref="TextMenu.Item"/> to add items adjacent to.</param>
        /// <param name="after">Whether to add before (false) or after (true) the specified item. Default after.</param>
        /// <returns></returns>
        public TextMenu Create(TextMenu addTo, TextMenu.Item addAt, bool after = true) {
            if (addAt == null) {
                //TODO log warning
            }
            Create(addTo, addTo.Items.FindIndex(item => item == addAt) + (after ? 1 : 0));
            return addTo;
        }

        public TextMenu Remove() {
            if (Container != null) {
                foreach (TextMenu.Item item in Items) {
                    Container.Remove(item);
                }
            }
            return Container;
        }
    }

    /// <summary>
    /// The text menu currently shown in the graph viewer.
    /// </summary>
    public static TextMenu CurrentMenu;

    public static Dictionary<int, MenuCreator> InitialMenusByMode = [];
    public static MenuCreator ModeInitialMenu => InitialMenusByMode.TryGetValue(Mode, out MenuCreator menu) ? menu : null;

    public static TextMenu NewMenu() {
        TextMenu menu = new(){
            Focused = false,
            Visible = false,
            X = MarginH,
            Y = HeadbarHeight + GFX.Gui["strawberryCountBG"].Height + MarginV,
            Width = MenuWidth,
            Height = Celeste.TargetHeight - (HeadbarHeight + GFX.Gui["strawberryCountBG"].Height + MarginV),
            Justify = Vector2.Zero,
            InnerContent = TextMenu.InnerContentMode.TwoColumn
        };
        MenuDataContainer dataContainer = new();
        dataContainer.MenuData.Add(new MultiDisplayData());
        menu.Add(dataContainer);
        return menu;
    }

    public static void SwapMenu(MenuCreator menu) {
        if (CurrentMenu != null) {
            CurrentMenu.Focused = CurrentMenu.Visible = false;
        }
        if (menu == null) {
            CurrentMenu = null;
        } else {
            menu.CreatorFor?.RemoveSelf();
            CurrentMenu = menu.Create();
            DebugMap.Add(CurrentMenu);
        }
        if (CurrentMenu != null) {
            CurrentMenu.Focused = CurrentMenu.Visible = true;
        }
    }

    public static void CreateMenus() {
        foreach (MenuCreator menu in
            typeof(GraphViewer)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(MenuCreator))
            .Select(field => (MenuCreator)field.GetValue(null))
        ) {
            DebugMap.Add(menu.Create());
            if (menu.InitialFor != null) {
                InitialMenusByMode[(int)menu.InitialFor] = menu;
            }
        }
    }
}