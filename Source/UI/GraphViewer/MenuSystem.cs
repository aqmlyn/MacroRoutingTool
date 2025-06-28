using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    public class MenuCreator {
        /// <summary>
        /// The <see cref="Mode"/> that the <see cref="TextMenu"/> this creates should be initially shown for, if any.
        /// </summary>
        public int? InitialFor = null;
        /// <summary>
        /// The <see cref="TextMenu"/> that this <see cref="MenuCreator"/> creates.
        /// </summary>
        public TextMenu CreatorFor;
        /// <summary>
        /// Called by <see cref="Create"/> to get a list of <see cref="TextMenu.Items"/> to add to <see cref="CreatorFor"/>. 
        /// </summary>
        public Func<MenuCreator, List<TextMenu.Item>> OnCreate;
        /// <summary>
        /// Called by <see cref="Create"/> after adding the items to <see cref="CreatorFor"/>. 
        /// </summary>
        public Action<MenuCreator> AfterCreate;
        /// <summary>
        /// Add the given <see cref="TextMenu.Item"/> to <see cref="CreatorFor"/>. 
        /// </summary>
        public void Add(TextMenu.Item item) {
            if (CreatorFor != null) {
                item.Container = CreatorFor;
                CreatorFor.Add(item);
            }
        }
        /// <summary>
        /// Creates and returns a new <see cref="TextMenu"/> populated using the following process:
        /// <list type="number">
        /// <item>Call <see cref="OnCreate"/>.</item>
        /// <item>Add the resulting list of <see cref="TextMenu.Item"/>s to <see cref="CreatorFor"/>.</item>
        /// <item>Call <see cref="AfterCreate"/>.</item>  
        /// </list>
        /// </summary>
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
        /// <summary>
        /// The <see cref="TextMenu"/> that currently contains this <see cref="MenuPart"/>. 
        /// </summary>
        public TextMenu Container;
        /// <summary>
        /// The list of <see cref="TextMenu.Item"/>s this <see cref="MenuPart"/> currently contains. 
        /// </summary>
        public List<TextMenu.Item> Items = [];
        /// <summary>
        /// <see cref="Create"/> will call this function, passing this <see cref="MenuPart"/>, to get the list of <see cref="TextMenu.Item"/>s
        /// to add to its <see cref="Container"/>.  
        /// </summary>
        public Func<MenuPart, List<TextMenu.Item>> Creator;

        /// <summary>
        /// Add the given <see cref="TextMenu.Item"/> to this <see cref="MenuPart"/> and its current <see cref="Container"/>. 
        /// </summary>
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

        /// <summary>
        /// Remove this <see cref="MenuPart"/>'s <see cref="Items"/> from <see cref="Container"/>, the <see cref="TextMenu"/> that currently contains them.<br/>
        /// If an item in this <see cref="MenuPart"/> is currently hovered, the hover will automatically be moved to a hoverable item
        /// outside this <see cref="MenuPart"/>. If there is no such item, the argument <paramref name="closeIfNoHovers"/> controls
        /// whether to close (true, default) or unfocus (false) the <see cref="TextMenu"/>.
        /// </summary>
        public TextMenu Remove(bool closeIfNoHovers = true) {
            if (Container != null) {
                foreach (TextMenu.Item item in Items) {
                    //if this MenuPart contains the currently hovered item:
                    if (Container.Selection >= Container.Items.Count || Container.Current == item) {
                        //if there's a hoverable item before this menu part, move hover to that
                        for (int i = Container.Items.IndexOf(item) - 1; i >= 0; i--) {
                            if (Container.Items[i].Hoverable) {
                                Container.Current = Container.Items[i];
                                break;
                            }
                        }
                        //else
                        if (Container.Selection >= Container.Items.Count || Container.Current == item) {
                            //if there's a hoverable item after this menu part, move hover to that
                            for (int i = Container.Items.IndexOf(Items[^1]); i < Container.Items.Count; i++) {
                                if (Container.Items[i].Hoverable) {
                                    Container.Current = Container.Items[i];
                                    break;
                                }
                            }
                            //else
                            if (Container.Selection >= Container.Items.Count || Container.Current == item) {
                                //there is nothing else in the containing menu that can be hovered
                                if (closeIfNoHovers) {
                                    Container.Close();
                                } else {
                                    Container.Focused = false;
                                    Container.Selection = -1;
                                }
                            }
                        }
                    }
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

    /// <summary>
    /// Whether the <see cref="CurrentMenu"/> is currently focused. 
    /// </summary>
    public static bool InMenu => CurrentMenu != null && (CurrentMenu.Focused || CurrentMenu.RenderAsFocused);

    /// <summary>
    /// Whether the user is currently typing in a <see cref="TextMenuExt.TextBox"/> in the <see cref="CurrentMenu"/>.  
    /// </summary>
    public static bool Typing {
        get {
            if (CurrentMenu == null) return false;
            foreach (TextMenu.Item item in CurrentMenu.Items) {
                if (item is ListItem listItem) {
                    foreach (var side in new ListItem.Part[] {listItem.Left, listItem.Right}) {
                        if (side.Editable && ((TextMenuExt.TextBox)side.Element).Typing) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    /// <summary>
    /// List of <see cref="MenuCreator"/>s for the menus initially shown in each <see cref="Mode"/>.
    /// </summary>
    public static Dictionary<int, MenuCreator> InitialMenusByMode = [];
    /// <summary>
    /// Gets the <see cref="MenuCreator"/> for the initial menu for the current <see cref="Mode"/>.  
    /// </summary>
    public static MenuCreator ModeInitialMenu => InitialMenusByMode.TryGetValue(Mode, out MenuCreator menu) ? menu : null;

    /// <summary>
    /// Creates and returns a new <see cref="TextMenu"/> whose appearance has been configured for display in the graph viewer.
    /// </summary>
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

    /// <summary>
    /// <list type="number">
    /// <item>Remove the <see cref="TextMenu"/> currently stored in <see cref="CurrentMenu"/>.</item>
    /// <item>Add the given menu to the <see cref="DebugMap"/>'s entity list and store that menu in <see cref="CurrentMenu."/></item>  
    /// </list>
    /// </summary>
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

    /// <summary>
    /// For each <c>public static</c> member of the <see cref="GraphViewer"/> class that is a field whose type is <see cref="MenuCreator"/>:
    /// <list type="number">
    /// <item>Call the <see cref="MenuCreator"/>'s <see cref="MenuCreator.Create"/> method.</item>
    /// <item>Add the resulting <see cref="TextMenu"/> to the <see cref="DebugMap"/>'s entity list.</item>
    /// <item>if the creator's menu is the <see cref="MenuCreator.InitialFor"/> a <see cref="Mode"/>, add the creator to <see cref="InitialMenusByMode"/> accordingly.</item>
    /// </list>  
    /// </summary>
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