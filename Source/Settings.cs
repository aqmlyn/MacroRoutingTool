using System;
using System.Collections.Generic;
using Celeste.Mod.MacroRoutingTool.UI;

namespace Celeste.Mod.MacroRoutingTool;

public class MRTSettings : EverestModuleSettings {
  #region binds
    /// <summary>
    /// When the debug map is open, this bind toggles whether the graph menu is focused.
    /// </summary>
    [DefaultButtonBinding(0, Microsoft.Xna.Framework.Input.Keys.Tab)]
    public ButtonBinding Bind_DebugFocusGraphMenu {get; set;}

    /// <summary>
    /// When the debug map is open, this bind cycles through the graph views.
    /// </summary>
    [DefaultButtonBinding(0, Microsoft.Xna.Framework.Input.Keys.M)]
    public ButtonBinding Bind_DebugGraphViewerMode {get; set;}

    /// <summary>
    /// When the debug map is open, holding this bind shows metadata over some entities.
    /// </summary>
    [DefaultButtonBinding(0, Microsoft.Xna.Framework.Input.Keys.Q)]
    public ButtonBinding Bind_DebugEntityMetadata {get; set;}
  #endregion

  #region graph viewer
    
    /// <summary>
    /// <inheritdoc cref="UI.GraphViewer.Mode"/>
    /// </summary>
    [SettingIgnore]
    public int GraphViewerMode {get; set;} = (int)UI.GraphViewer.Modes.Disabled;

    public class OpenedItems {
        /// <summary>
        /// ID of the most recently opened <see cref="Data.Graph"/> that was assigned to the given <see cref="MapSide"/>. 
        /// </summary>
        public Guid Graph;
        /// <summary>
        /// ID of the most recently opened <see cref="Data.Route"/> that was assigned to the given <see cref="MapSide"/>. 
        /// </summary>
        public Guid Route;
    }

    /// <summary>
    /// List of most recently opened items for each map the user has ever viewed a <see cref="Data.Graph"/> for.
    /// </summary>
    [SettingIgnore]
    public Dictionary<Utils.MapSide, OpenedItems> Open {get; set;} = [];

    /// <summary>
    /// ID of the most recently viewed <see cref="Data.Graph"/> for each map the user has ever viewed a <see cref="Data.Graph"/> for.
    /// </summary>
    [SettingIgnore]
    public Dictionary<Utils.MapSide, Guid> LastGraphID {get; set;} = [];

    /// <summary>
    /// ID of the most recently viewed <see cref="Data.Route"/> for each <see cref="Data.Graph"/> the user has ever viewed a <see cref="Data.Route"/> for.  
    /// </summary>
    [SettingIgnore]
    public Dictionary<Guid, Guid> LastRouteID {get; set;} = [];
  #endregion

  #region IO
    /// <summary>
    /// User-facing text representing the directory containing the YAML files that graphs and routes are imported from and exported to.
    /// </summary>
    public string MRTDirectory {get; set;} = System.IO.Path.Combine("%CELESTE%", "MacroroutingTool");

    /// <summary>
    /// Absolute path of the directory containing the YAML files that graphs and routes are imported from and exported to.
    /// </summary>
    public string MRTDirectoryAbsolute => MRTDirectory.Replace("%CELESTE%", Monocle.Engine.AssemblyDirectory, StringComparison.OrdinalIgnoreCase);

    public void CreateMRTDirectoryEntry(TextMenu menu, bool inGame) {
        UI.ListItem item = new(false, true) {
            Container = menu,
            LeftWidthPortion = 0.35f
        };
        item.Left.Value = MRTDialog.PathSetting;
        item.Left.Handler.Bind<string>(new());
        item.Right.Value = MRTDirectory;
        item.Right.Handler.Bind<string>(new() {
            ValueParser = value => MRTDirectory = value
        });
        menu.Add(item);
    }
  #endregion
  
    /// <summary>
    /// Dummy property to cause Everest to invoke <see cref="CreateIOMenuEntry"/> to create the IO section of MRT's Mod Options menu. 
    /// </summary>
    public bool IOMenu { get; set; }
    /// <summary>
    /// Creates the IO section of MRT's Mod Options menu.
    /// </summary>
    /// <param name="menu">The full Mod Options menu.</param>
    /// <param name="sceneIsLevel">Whether the Mod Options menu was opened from a level or from another scene (e.g. <see cref="OuiMainMenu"/>).</param>
    public void CreateIOMenuEntry(TextMenu menu, bool sceneIsLevel) {
        TableMenu table = new(){DisplayWidth = 1440f, UseNavigationCursor = false};
        var tableItem = table.MakeSubmenuCollapsedIn(menu);
        tableItem.CollapserLabel.Text = "IO";

        float leftPortion = 0.3f;
        table.ColumnFormats.Add(new() { Justify = 0f, Measure = table.DisplayWidth * leftPortion });
        table.ColumnFormats.Add(new() { Justify = 1f, Measure = table.DisplayWidth * (1f - leftPortion), MarginBefore = 20f });

        var directoryRow = table.AddRow();
        var directoryLabel = new TableMenu.Label();
        directoryLabel.Element.Text = MRTDialog.PathSetting;
        directoryRow.Add(directoryLabel);

        var testButtonRow = table.AddRow();
        var testButton = new TableMenu.Button();
        testButton.Element.Text = "bnuuy";
        testButton.OnPressed = () => Monocle.Engine.Commands.Log("bnuuy");
        testButtonRow.Add(testButton);
        
        foreach (var rowFormat in table.RowFormats) { rowFormat.Justify = 0.5f; }
    }
}