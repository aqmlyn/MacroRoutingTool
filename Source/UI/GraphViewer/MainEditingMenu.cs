using Celeste.Mod.MacroRoutingTool.Data;

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
            TextMenu.Button addConnectionButton = new(MRTDialog.GraphMenuAddConnection) {
                OnPressed = AddConnection
            };

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
        Point point = new() {
            Image = UIHelpers.AtlasPaths.Gui + "dot", //TODO allow this to be changed, ideally with like an image picker
            X = (int)DebugMap.mousePosition.X,
            Y = (int)DebugMap.mousePosition.Y
        };
        Graph.Points.Add(point);
        Selection.Clear();
        Selection.Add(point);
        SelectionHas = SelectionContents.Points;
        ActiveSelection.Clear();
        ActiveSelection.AddRange(Selection);
        RefreshSelectionMenu();
    }

    public static void AddConnection() {
        Connection conn = new();
        if (SelectionHas == SelectionContents.Points && Selection.Count == 2) {
            conn.From = Selection[0].ID;
            conn.To = Selection[1].ID;
        } else if (Selection.Count == 1) {
            if (SelectionHas == SelectionContents.Connections) {
                Connection otherConn = Selection[0] as Connection;
                conn.From = otherConn.From;
                conn.To = otherConn.To;
            } else if (SelectionHas == SelectionContents.Points) {
                conn.From = (Selection[0] as Point).ID;
            }
        }
        Graph.Connections.Add(conn);
        Selection.Clear();
        Selection.Add(conn);
        SelectionHas = SelectionContents.Connections;
        ActiveSelection.Clear();
        ActiveSelection.AddRange(Selection);
        RefreshSelectionMenu();
    }
}