using Celeste.Editor;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    public static MenuCreator MainRoutingMenu = new() {
        InitialFor = (int)Modes.Routing,
        OnCreate = (creator) => {
            TextMenu menu = NewMainMenu();
            creator.CreatorFor = menu;

            MenuDataContainer dataContainer = menu.EnsureDataContainer();
            HeaderScaleData headerScaleData = new(){
                Scale = 0.7f
            };

            //ROUTE INFO
            TextMenu.Header routeHeader = new(MRTDialog.RouteMenuHeader);
            dataContainer.ItemData.Add(routeHeader, headerScaleData);

            //Choose another route...
            TextMenu.Button chooseRouteButton = new(string.Format(MRTDialog.OpenChooser, MRTDialog.Route));

            //Name
            ListItem routeNameDisplay = new(false, true){LeftWidthPortion = 0.4f};
            routeNameDisplay.Left.Value = MRTDialog.ItemName;
            routeNameDisplay.Left.Handler.Bind<string>(new());
            routeNameDisplay.Right.Handler.Bind<string>(new(){
                ValueGetter = () => Route.Name,
                ValueParser = name => Route.Name = name
            });

            //Path
            ListItem routePathDisplay = new(false, true){LeftWidthPortion = 0.4f};
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

            return [
                routeHeader,
                chooseRouteButton,
                routeNameDisplay,
                routePathDisplay
            ];
        }
    };
}