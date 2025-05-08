using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                MultiDisplayData.TextMenuOption<int> selChooser = new(string.Format(MRTDialog.SelectionChooserLabel, "-", Selection.Count));
                selChooser.OnValueChange = idx => {
                    string idxText = "-";
                    List<Traversable> items = Selection;
                    if (idx > 0) {
                        idxText = idx.ToString();
                        items = [Selection[idx - 1]];
                    }
                    ActiveSelection.Clear();
                    ActiveSelection.AddRange(items);
                    selChooser.Label = string.Format(MRTDialog.SelectionChooserLabel, idxText, Selection.Count);
                };
                selChooser.Add(MRTDialog.SelectionChooserAll, 0, true);
                foreach (var item in Selection) {
                    selChooser.Add($"{item.EditorID()} {(string.IsNullOrEmpty(item.Name) ? MRTDialog.GraphDefaultName : item.Name)}", selChooser.Values.Count);
                }

                return [selChooser];
            }

            return [];
        }
    };

    public static MenuPart EditorSelectionMenu = new(){
        Creator = part => {
            if (ActiveSelection.Count == 0) {return [];}

            //Name
            ListItem itemNameDisplay = new(false, true){LeftWidthPortion = 0.4f};
            itemNameDisplay.Left.Value = MRTDialog.ItemName;
            itemNameDisplay.Left.Handler.Bind<string>(new());
            itemNameDisplay.Right.Handler.Bind<string>(new() {
                ValueGetter = () => {
                    var items = ActiveSelection.DistinctBy(item => item.Name);
                    return items.Count() != 1 ? "" : items.First().Name;
                },
                ValueParser = str => {
                    foreach (var item in ActiveSelection) {
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
                        var items = ActiveSelection.DistinctBy(item => (item as Data.Point).X);
                        return items != null && items.Count() != 1 ? "" : (items.First() as Data.Point).X.ToString();
                    },
                    ValueParser = str => {
                        if (int.TryParse(str, out int x)) {
                            foreach (var item in ActiveSelection) {
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
                        var items = ActiveSelection.DistinctBy(item => (item as Data.Point).Y);
                        return items != null && items.Count() != 1 ? "" : (items.First() as Data.Point).Y.ToString();
                    },
                    ValueParser = str => {
                        if (int.TryParse(str, out int y)) {
                            foreach (var item in ActiveSelection) {
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
                items.Add(new SelectionEditor.Option<Guid>(() => MRTDialog.ConnectionSelectionFrom, typeof(Connection).GetField(nameof(Connection.From))) {
                    Options = [.. Graph.Points.Select(pt => pt.ID)],
                    OptionLabeller = id => {
                        Data.Point pt = Graph.Points.First(pt => pt.ID == id);
                        return $"({Graph.Points.IndexOf(pt) + 1}) {(string.IsNullOrWhiteSpace(pt.Name) ? MRTDialog.GraphDefaultName : pt.Name)}";
                    }
                }.Create());

                //To
                items.Add(new SelectionEditor.Option<Guid>(() => MRTDialog.ConnectionSelectionTo, typeof(Connection).GetField(nameof(Connection.To))) {
                    Options = [.. Graph.Points.Select(pt => pt.ID)],
                    OptionLabeller = id => {
                        Data.Point pt = Graph.Points.First(pt => pt.ID == id);
                        return $"({Graph.Points.IndexOf(pt) + 1}) {(string.IsNullOrWhiteSpace(pt.Name) ? MRTDialog.GraphDefaultName : pt.Name)}";
                    }
                }.Create());

                //Visible
                items.Add(new SelectionEditor.Option<string>(() => MRTDialog.ConnectionSelectionVisibility, typeof(Connection).GetField(nameof(Connection.VisibleWhen))){
                    Options = [.. MRTDialog.ConnectionVisibilityTypes.Keys],
                    OptionLabeller = opt => MRTDialog.ConnectionVisibilityTypes[opt]()
                }.Create());
            }

            return items;
        }
    };

    public static class SelectionEditor {
        public class Option<TValue> : MemberEditor.Option<TValue> {
            public Option(Func<string> labelGetter) {
                ItemsToEdit = () => ActiveSelection.ConvertAll(item => (object)item);
                LabelGetter = labelGetter;
            }

            public Option(Func<string> labelGetter, FieldInfo field) : this(labelGetter) {
                OnGet = () => field.GetValue;
                OnSet = () => field.SetValue;
            }

            public Option(Func<string> labelGetter, PropertyInfo prop) : this(labelGetter) {
                OnGet = () => prop.GetValue;
                OnSet = () => prop.SetValue;
            }

            public Option(string label) : this(() => label) {}
            public Option(string label, FieldInfo field) : this(() => label, field) {}
            public Option(string label, PropertyInfo prop) : this(() => label, prop) {}
        }
    }
}