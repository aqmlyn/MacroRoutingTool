using Celeste.Editor;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    public static Menu MainRoutingMenu = new() {
        InitialFor = (int)Modes.Routing,
        Creator = () => {
            TextMenu menu = NewMainMenu();
            MenuDataContainer dataContainer = menu.EnsureDataContainer();
            HeaderScaleData headerScaleData = new(){
                Scale = 0.7f
            };

            //ROUTE INFO
            TextMenu.Header routeHeader = new(MRTDialog.RouteMenuHeader){Container = menu};
            dataContainer.ItemData.Add(routeHeader, headerScaleData);
            menu.Add(routeHeader);

            //Choose another route...
            TextMenu.Button chooseRouteButton = new(string.Format(MRTDialog.OpenChooser, MRTDialog.Route)){Container = menu};
            menu.Add(chooseRouteButton);

            //Name
            ListItem routeNameDisplay = new(false, true){Container = menu, LeftWidthPortion = 0.4f};
            routeNameDisplay.Left.Value = MRTDialog.ItemName;
            routeNameDisplay.Left.Handler.Bind<string>(new());
            routeNameDisplay.Right.Handler.Bind<string>(new(){
                ValueGetter = () => Route.Name,
                ValueParser = name => Route.Name = name
            });
            menu.Add(routeNameDisplay);

            //Path
            ListItem routePathDisplay = new(false, true){Container = menu, LeftWidthPortion = 0.4f};
            routePathDisplay.Left.Value = MRTDialog.ItemPath;
            routePathDisplay.Left.Handler.Bind<string>(new());
            routePathDisplay.Right.Handler.Bind<string>(new(){
                ValueGetter = () => Route.Path,
                ValueParser = path => {
                    Route.Path = path;
                    ((MapEditor)Engine.Scene).Save();
                    return Route.Path;
                }
            });
            menu.Add(routePathDisplay);

            return menu;
        }
    };
}