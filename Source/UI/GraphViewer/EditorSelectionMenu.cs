using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.MacroRoutingTool.Data;
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
                        if (SelectionHas != SelectionContents.Points) {return "";}
                        var items = Selection.DistinctBy(item => (item as Data.Point).X);
                        return items != null && items.Count() != 1 ? "" : (items.First() as Data.Point).X.ToString();
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
                        if (SelectionHas != SelectionContents.Points) {return "";}
                        var items = Selection.DistinctBy(item => (item as Data.Point).Y);
                        return items != null && items.Count() != 1 ? "" : (items.First() as Data.Point).Y.ToString();
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
                MultiDisplayData.TextMenuOption<Data.Point> connFrom = new(MRTDialog.ConnectionSelectionFrom);
                connFrom.Add("-", null);
                for (int i = 0; i < Graph.Points.Count; i++) {
                    var pt = Graph.Points[i];
                    connFrom.Add($"({i + 1}) {(string.IsNullOrWhiteSpace(pt.Name) ? $"[{MRTDialog.GraphDefaultName}]" : pt.Name)}", pt);
                }
                var initFroms = Selection.DistinctBy(item => (item as Connection).From);
                if (initFroms != null && initFroms.Count() == 1) {
                    var initFromId = (initFroms.First() as Connection).From;
                    connFrom.Index = Graph.Points.FindIndex(pt => pt.ID == initFromId) + 1;
                }
                connFrom.OnValueChange = pt => {
                    if (pt == null) {return;}
                    foreach (var conn in Selection.ConvertAll(item => (Connection)item)) {
                        conn.From = pt.ID;
                    }
                };
                items.Add(connFrom);

                //To
                MultiDisplayData.TextMenuOption<Data.Point> connTo = new(MRTDialog.ConnectionSelectionTo);
                connTo.Add("-", null);
                for (int i = 0; i < Graph.Points.Count; i++) {
                    var pt = Graph.Points[i];
                    connTo.Add($"({i + 1}) {(string.IsNullOrWhiteSpace(pt.Name) ? $"[{MRTDialog.GraphDefaultName}]" : pt.Name)}", pt);
                }
                var initTos = Selection.DistinctBy(item => (item as Connection).To);
                if (initTos != null && initTos.Count() == 1) {
                    var initToId = (initTos.First() as Connection).To;
                    connTo.Index = Graph.Points.FindIndex(pt => pt.ID == initToId) + 1;
                }
                connTo.OnValueChange = pt => {
                    foreach (var conn in Selection.ConvertAll(item => (Connection)item)) {
                        conn.To = pt?.ID ?? default;
                    }
                };
                items.Add(connTo);
            }

            return items;
        }
    };
}