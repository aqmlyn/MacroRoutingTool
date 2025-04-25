namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    public static MenuCreator MainEditingMenu = new() {
        InitialFor = (int)Modes.Editing,
        OnCreate = creator => {
            TextMenu menu = NewMainMenu();
            creator.CreatorFor = menu;

            MenuDataContainer dataContainer = menu.EnsureDataContainer();
            HeaderScaleData headerScaleData = new(){
                Scale = 0.7f
            };

            //Edit Weights
            TextMenu.Button editWeightsButton = new(MRTDialog.GraphMenuEditWeights);

            //Add Point
            TextMenu.Button addPointButton = new(MRTDialog.GraphMenuAddPoint) {
                OnPressed = AddPoint
            };

            //Add Connection
            TextMenu.Button addConnectionButton = new(MRTDialog.GraphMenuAddConnection);

            //SELECTION
            TextMenu.Header selectionHeader = new(MRTDialog.SelectionHeader);
            dataContainer.ItemData.Add(selectionHeader, headerScaleData);

            return [
                editWeightsButton,
                addPointButton,
                addConnectionButton,
                selectionHeader
            ];
        },
        AfterCreate = creator => {
            EditorSelectionChooser.Create(creator.CreatorFor);
            EditorSelectionMenu.Create(creator.CreatorFor);
        }
    };

    public static void AddPoint() {
        Graph.Points.Add(new() {
            Image = UIHelpers.AtlasPaths.Gui + "dot", //TODO allow this to be changed, ideally with like an image picker
            X = (int)DebugMap.mousePosition.X,
            Y = (int)DebugMap.mousePosition.Y
        });
    }
}