using System.Collections.Generic;
using System.Linq;
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

            //Name
            ListItem itemNameDisplay = new(false, true){LeftWidthPortion = 0.4f};
            itemNameDisplay.Left.Value = MRTDialog.ItemName;
            itemNameDisplay.Left.Handler.Bind<string>(new());
            itemNameDisplay.Right.Handler.Bind<string>(new() {
                ValueGetter = () => {
                    var items = Selection.DistinctBy(item => item.Name);
                    return items.Count() != 1 ? "" : items.First().Name;
                },
                ValueParser = str => {
                    foreach (var item in Selection) {
                        item.Name = str;
                    }
                    return str;
                }
            });

            //Requirements
            TextMenu.Button toggleShowRequirements = new(MRTDialog.SelectionRequirementList);

            //Results
            TextMenu.Button toggleShowResults = new(MRTDialog.SelectionResultList);

            List<TextMenu.Item> items = [
                itemNameDisplay,
                toggleShowRequirements,
                toggleShowResults
            ];

            if (SelectionHas == SelectionContents.Points) {
                //TODO Image

                //X
                ListItem pointX = new(false, true){LeftWidthPortion = 0.4f};
                pointX.Left.Value = MRTDialog.PointSelectionX;
                pointX.Left.Handler.Bind<string>(new());
                pointX.Right.Handler.Bind<string>(new() {
                    ValueGetter = () => {
                        var items = Selection.DistinctBy(item => (item as Data.Point).X);
                        return items.Count() != 1 ? "" : (items.First() as Data.Point).X.ToString();
                    },
                    ValueParser = str => {
                        if (int.TryParse(str, out int x)) {
                            foreach (var item in Selection) {
                                (item as Data.Point).X = x;
                            }
                        } else {
                            str = "";
                        }
                        return str;
                    }
                });
                items.Add(pointX);

                //Y
                ListItem pointY = new(false, true){LeftWidthPortion = 0.4f};
                pointY.Left.Value = MRTDialog.PointSelectionY;
                pointY.Left.Handler.Bind<string>(new());
                pointY.Right.Handler.Bind<string>(new() {
                    ValueGetter = () => {
                        var items = Selection.DistinctBy(item => (item as Data.Point).Y);
                        return items.Count() != 1 ? "" : (items.First() as Data.Point).Y.ToString();
                    },
                    ValueParser = str => {
                        if (int.TryParse(str, out int y)) {
                            foreach (var item in Selection) {
                                (item as Data.Point).Y = y;
                            }
                        } else {
                            str = "";
                        }
                        return str;
                    }
                });
                items.Add(pointY);

                //Default End
                MultiDisplayData.TextMenuOption<string> pointEndType = new(MRTDialog.PointSelectionEndpointType);
                pointEndType.Add(MRTDialog.PointEndTypes[Data.Point.EndType.None](), Data.Point.EndType.None, true);
                pointEndType.Add(MRTDialog.PointEndTypes[Data.Point.EndType.Start](), Data.Point.EndType.Start);
                pointEndType.Add(MRTDialog.PointEndTypes[Data.Point.EndType.Finish](), Data.Point.EndType.Finish);
                items.Add(pointEndType);

                //TODO Fast Travel
                
                if (Selection.Count == 1) {
                    //ID
                    ListItem idDisplay = new();
                    idDisplay.Left.Value = MRTDialog.PointSelectionID;
                    idDisplay.Left.Handler.Bind<string>(new());
                    items.Add(idDisplay);
                }
            } else if (SelectionHas == SelectionContents.Connections) {
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

            return items;
        }
    };
}