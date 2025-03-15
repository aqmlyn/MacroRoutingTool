using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    public class Menu {
        public int? InitialFor = null;
        public TextMenu Current;
        public Func<TextMenu> Creator;
        public TextMenu Create() {
            return Current = Creator?.Invoke() ?? null;
        }
    }

    /// <summary>
    /// The text menu currently shown in the graph viewer.
    /// </summary>
    public static TextMenu CurrentMenu;

    public static Dictionary<int, Menu> InitialMenusByMode = [];
    public static Menu ModeInitialMenu => InitialMenusByMode.TryGetValue(Mode, out Menu menu) ? menu : null;

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

    public static TextMenu NewMainMenu() {
        TextMenu menu = NewMenu();
        MenuDataContainer dataContainer = menu.EnsureDataContainer();
        HeaderScaleData headerScaleData = new(){
            Scale = 0.7f
        };

        //GRAPH INFO
        TextMenu.Header graphHeader = new(MRTDialog.GraphMenuHeader){Container = menu};
        dataContainer.ItemData.Add(graphHeader, headerScaleData);
        menu.Add(graphHeader);

        //Choose another graph...
        TextMenu.Button chooseGraphButton = new(MRTDialog.GraphMenuChooser){Container = menu};
        menu.Add(chooseGraphButton);

        //Name
        ListItem graphNameDisplay = new(false, true){Container = menu};
        graphNameDisplay.Left.Value = MRTDialog.ItemName;
        graphNameDisplay.Left.Handler.HandleUsing<string>(new());
        menu.Add(graphNameDisplay);

        return menu;
    }

    public static void SwapMenu(Menu menu, Scene scene = null) {
        if (CurrentMenu != null) {
            CurrentMenu.Focused = CurrentMenu.Visible = false;
        }
        if (menu == null) {
            CurrentMenu = null;
        } else if (scene == null) {
            CurrentMenu = menu.Current;
        } else {
            menu.Current.RemoveSelf();
            CurrentMenu = menu.Create();
            scene.Add(CurrentMenu);
        }
        if (CurrentMenu != null) {
            CurrentMenu.Focused = CurrentMenu.Visible = true;
        }
    }

    public static void CreateMenus(Scene scene) {
        foreach (Menu menu in
            typeof(GraphViewer)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(Menu))
            .Select(field => (Menu)field.GetValue(null))
        ) {
            scene.Add(menu.Create());
            if (menu.InitialFor != null) {
                InitialMenusByMode[(int)menu.InitialFor] = menu;
            }
        }
    }
}