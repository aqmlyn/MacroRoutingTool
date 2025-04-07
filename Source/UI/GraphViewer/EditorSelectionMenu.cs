using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    public static MenuPart EditorSelectionChooser = new(){
        Creator = part => {
            if (Selection.Count == 0) {
                //No selection
                ListItem noSelection = new() {
                    OnAdded = item => {
                        item.Left.Value = MRTDialog.SelectedNothing;
                        item.Left.Handler.Bind<string>(new());
                        UIHelpers.TextElement elem = (UIHelpers.TextElement)item.Left.Element;
                        elem.Justify = new Vector2(0.5f, 0.5f);
                        elem.Color = Color.Gray;
                    }
                };
                return [noSelection];
            }

            if (Selection.Count > 1) {
                //Multiple items selected -- display a chooser
            }

            return [];
        }
    };

    public static MenuPart EditorSelectionMenu = new(){
        Creator = part => {
            if (Selection.Count == 0) {return [];}

            //Weights
            TextMenu.Button toggleShowWeights = new(MRTDialog.SelectionWeightList);

            //Requirements
            TextMenu.Button toggleShowRequirements = new(MRTDialog.SelectionRequirementList);

            //Results
            TextMenu.Button toggleShowResults = new(MRTDialog.SelectionResultList);

            List<TextMenu.Item> items = [
                toggleShowWeights,
                toggleShowRequirements,
                toggleShowResults
            ];

            if (Selection.Count == 1) {
                //Name
                ListItem itemNameDisplay = new(false, true);
                itemNameDisplay.Left.Value = MRTDialog.ItemName;
                itemNameDisplay.Left.Handler.Bind<string>(new());
                items.Insert(0, itemNameDisplay);
            }

            if (SelectionHas == SelectionContents.Points) {
                //Default End
                MultiDisplayData.TextMenuOption<string> pointEndType = new(MRTDialog.PointSelectionEndpointType);
                pointEndType.Add(MRTDialog.PointEndTypes[Data.Point.EndType.None](), Data.Point.EndType.None, true);
                pointEndType.Add(MRTDialog.PointEndTypes[Data.Point.EndType.Start](), Data.Point.EndType.Start);
                pointEndType.Add(MRTDialog.PointEndTypes[Data.Point.EndType.Finish](), Data.Point.EndType.Finish);
                items.Add(pointEndType);

                //TODO Fast Travel
                
                if (Selection.Count == 1) {
                    //X
                    ListItem pointX = new(false, true);
                    pointX.Left.Value = MRTDialog.PointSelectionX;
                    pointX.Left.Handler.Bind<string>(new());
                    items.Add(pointX);

                    //Y
                    ListItem pointY = new(false, true);
                    pointY.Left.Value = MRTDialog.PointSelectionY;
                    pointY.Left.Handler.Bind<string>(new());
                    items.Add(pointY);

                    //TODO Image

                    //ID
                    ListItem idDisplay = new();
                    pointY.Left.Value = MRTDialog.PointSelectionID;
                    pointY.Left.Handler.Bind<string>(new());
                    items.Add(idDisplay);
                }
            } else if (SelectionHas == SelectionContents.Connections) {
                if (Selection.Count == 1) {
                    //From
                    ListItem connFrom = new(false, true);
                    connFrom.Left.Value = MRTDialog.ConnectionSelectionFrom;
                    connFrom.Left.Handler.Bind<string>(new());
                    items.Add(connFrom);

                    //To
                    ListItem connTo = new(false, true);
                    connTo.Left.Value = MRTDialog.ConnectionSelectionTo;
                    connTo.Left.Handler.Bind<string>(new());
                    items.Add(connTo);
                }
            }

            return items;
        }
    };
}