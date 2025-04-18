using Celeste.Editor;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
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
        TextMenu.Button chooseGraphButton = new(string.Format(MRTDialog.OpenChooser, MRTDialog.Graph)){Container = menu};
        menu.Add(chooseGraphButton);

        //Name
        ListItem graphNameDisplay = new(false, true){Container = menu, LeftWidthPortion = 0.4f};
        graphNameDisplay.Left.Value = MRTDialog.ItemName;
        graphNameDisplay.Left.Handler.Bind<string>(new());
        graphNameDisplay.Right.Handler.Bind<string>(new(){
            ValueGetter = () => Graph.Name,
            ValueParser = name => Graph.Name = name
        });
        menu.Add(graphNameDisplay);

        //Path
        ListItem graphPathDisplay = new(false, true){Container = menu, LeftWidthPortion = 0.4f};
        graphPathDisplay.Left.Value = MRTDialog.ItemPath;
        graphPathDisplay.Left.Handler.Bind<string>(new());
        graphPathDisplay.Right.Handler.Bind<string>(new(){
            ValueGetter = () => Graph.Path,
            ValueParser = path => {
                Graph.Path = path;
                ((MapEditor)Engine.Scene).Save();
                return Graph.Path;
            }
        });
        menu.Add(graphPathDisplay);

        return menu;
    }
}