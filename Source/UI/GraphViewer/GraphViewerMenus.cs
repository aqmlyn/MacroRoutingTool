using System.Collections.Generic;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    public static class MenuParts {
      #region Graph selection
        public static IEnumerable<TextMenu.Item> EditingAnythingItems(TextMenu menu) {
            //Name
            ListItem itemNameDisplay = new(false, true);
            itemNameDisplay.Left.Value = MRTDialog.ItemName;
            itemNameDisplay.Left.Handler.HandleUsing<string>(new());

            //Weights
            TextMenu.Button toggleShowWeights = new(MRTDialog.SelectionWeightList);

            //Requirements
            TextMenu.Button toggleShowRequirements = new(MRTDialog.SelectionRequirementList);

            //Results
            TextMenu.Button toggleShowResults = new(MRTDialog.SelectionResultList);

            return [
                itemNameDisplay,
                toggleShowWeights,
                toggleShowRequirements,
                toggleShowResults
            ];
        }

        public static IEnumerable<TextMenu.Item> EditingPointItems(TextMenu menu) {
            //X
            ListItem pointX = new(false, true);
            pointX.Left.Value = MRTDialog.PointSelectionX;
            pointX.Left.Handler.HandleUsing<string>(new());

            //Y
            ListItem pointY = new(false, true);
            pointY.Left.Value = MRTDialog.PointSelectionY;
            pointY.Left.Handler.HandleUsing<string>(new());

            //Default End
            MultiDisplayData.TextMenuOption<string> pointEndType = new(MRTDialog.PointSelectionEndpointType);
            pointEndType.Add(MRTDialog.PointEndTypes[Data.Point.EndType.None](), Data.Point.EndType.None, true);
            pointEndType.Add(MRTDialog.PointEndTypes[Data.Point.EndType.Start](), Data.Point.EndType.Start);
            pointEndType.Add(MRTDialog.PointEndTypes[Data.Point.EndType.Finish](), Data.Point.EndType.Finish);

            return [
                pointX,
                pointY,
                pointEndType
            ];
        }

        public static IEnumerable<TextMenu.Item> EditingConnectionItems(TextMenu menu) {
            //From
            ListItem connFrom = new(false, true);
            connFrom.Left.Value = MRTDialog.ConnectionSelectionFrom;
            connFrom.Left.Handler.HandleUsing<string>(new());

            //To
            ListItem connTo = new(false, true);
            connTo.Left.Value = MRTDialog.ConnectionSelectionTo;
            connTo.Left.Handler.HandleUsing<string>(new());

            return [
                connFrom,
                connTo
            ];
        }
      #endregion

      #region Graph/route chooser
        public static IEnumerable<TextMenu.Item> ChooserChoices(TextMenu menu) {
            return [
                
            ];
        }

        public static IEnumerable<TextMenu.Item> ChooserActions(TextMenu menu) {
            return [
                
            ];
        }
      #endregion

      #region Weights
        public static IEnumerable<TextMenu.Item> WeightEditorChoices(TextMenu menu) {
            return [
                
            ];
        }

        public static IEnumerable<TextMenu.Item> WeightEditorActions(TextMenu menu) {
            return [
                
            ];
        }
      #endregion

      #region Requirements
        public static IEnumerable<TextMenu.Item> RequirementEditorChoices(TextMenu menu) {
            return [
                
            ];
        }

        public static IEnumerable<TextMenu.Item> RequirementEditorSingleActions(TextMenu menu) {
            return [
                
            ];
        }

        public static IEnumerable<TextMenu.Item> RequirementEditorGroupActions(TextMenu menu) {
            return [
                
            ];
        }
      #endregion

      #region Results
        public static IEnumerable<TextMenu.Item> ResultEditorChoices(TextMenu menu) {
            return [
                
            ];
        }

        public static IEnumerable<TextMenu.Item> ResultEditorSingleActions(TextMenu menu) {
            return [
                
            ];
        }

        public static IEnumerable<TextMenu.Item> ResultEditorGroupActions(TextMenu menu) {
            return [
                
            ];
        }
      #endregion

      #region Route menu
        public static IEnumerable<TextMenu.Item> RoutingMenuInfo(TextMenu menu) {
            return [
                
            ];
        }

        public static IEnumerable<TextMenu.Item> RoutingMenuWeights(TextMenu menu) {
            return [
                
            ];
        }

        public static IEnumerable<TextMenu.Item> RoutingMenuVariables(TextMenu menu) {
            return [
                
            ];
        }
      #endregion
    }
}