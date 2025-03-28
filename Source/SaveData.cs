using System;
using System.Collections.Generic;

namespace Celeste.Mod.MacroRoutingTool;

public class MRTSaveData : EverestModuleSaveData {
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
}