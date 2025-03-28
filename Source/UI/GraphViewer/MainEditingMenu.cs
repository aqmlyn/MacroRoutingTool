using Microsoft.Xna.Framework;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    public static Menu MainEditingMenu = new() {
        InitialFor = (int)Modes.Editing,
        Creator = () => {
            TextMenu menu = NewMainMenu();
            MenuDataContainer dataContainer = menu.EnsureDataContainer();
            HeaderScaleData headerScaleData = new(){
                Scale = 0.7f
            };

            //Edit Weights
            TextMenu.Button editWeightsButton = new(MRTDialog.GraphMenuEditWeights){Container = menu};
            menu.Add(editWeightsButton);

            //Add Point
            TextMenu.Button addPointButton = new(MRTDialog.GraphMenuAddPoint){Container = menu};
            menu.Add(addPointButton);

            //Add Connection
            TextMenu.Button addConnectionButton = new(MRTDialog.GraphMenuAddConnection){Container = menu};
            menu.Add(addConnectionButton);

            //SELECTION
            TextMenu.Header selectionHeader = new(MRTDialog.SelectionHeader){Container = menu};
            dataContainer.ItemData.Add(selectionHeader, headerScaleData);
            menu.Add(selectionHeader);

            //___ selected
            ListItem selectionCount = new() {
                Container = menu,
                OnAdded = item => {
                    TextMenuUtils.TextElement elem = (TextMenuUtils.TextElement)item.Left.Element;
                    elem.Justify = new Vector2(0.5f, 0.5f);
                    item.Left.Value = 0;
                    item.Left.Handler.Bind<int>(new() {
                        ValueToString = val => {
                            item.Visible = true;
                            if (val == 0) {
                                elem.Color = Color.LightGray;
                                return MRTDialog.SelectedNothing;
                            }
                            if (val > 1) {
                                elem.Color = Color.Aqua;
                                return string.Format(SelectionHas == SelectionContents.Points
                                ? MRTDialog.SelectedMultiplePoints : MRTDialog.SelectedMultipleConnections, val);
                            }
                            item.Visible = false;
                            return "";
                        }
                    });
                }
            };
            menu.Add(selectionCount);

            return menu;
        }
    };
}