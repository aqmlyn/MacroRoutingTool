using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Celeste.Mod.MacroRoutingTool;

/// <summary>
/// Contains properties whose getters fetch text from this mod's Dialog directory.<br/>
/// Properties are used because in an IDE, it's much easier to rename a symbol than to replace specific occurences of a string.
/// If I think of a better name for a dialog key in the future, it's much easier to make the change this way.
/// </summary>
public static class MRTDialog {
    public const string DevPreferredLanguage = "english";

    public const string DKPrefix = "macroroutingtool_";
    public static bool TryGet(string keyPart, out string result, bool cleaned = true) {
        if (Dialog.Languages == null) {
            Logger.Log(LogLevel.Warn, "MacroRoutingTool/Dialog", $"{nameof(MRTDialog)}.{nameof(TryGet)} was called before the game's dialog was loaded. Some dialog may not show properly as a result.\n{new StackTrace()}");
        } else {
            if ((cleaned ? Dialog.Language.Cleaned : Dialog.Language.Dialog).TryGetValue(keyPart, out result)) {return true;}
            if ((cleaned ? Dialog.Languages[DevPreferredLanguage].Cleaned : Dialog.Languages[DevPreferredLanguage].Dialog).TryGetValue(keyPart, out result)) {return true;}
        }
        result = "{" + keyPart + "}";
        return false;
    }
    public static string Get(string keyPart, bool cleaned = true) {
        TryGet(keyPart, out string result, cleaned);
        return result;
    }
    public static string GetInternal(string keyPart, bool cleaned = true) => Get(DKPrefix + keyPart, cleaned);

  
  #region Mod info
    public static string ModSettingsSectionTitle => GetInternal("modsettingstitle");
    public static string DebugMapTitle => GetInternal("debugmap_title");
  #endregion

  #region Mod settings
    public static string PathSetting => GetInternal("settings_path");
  #endregion

  #region Debug info
    public static Dictionary<AreaMode, Func<string>> ChapterSideTexts = new() {
        {AreaMode.BSide, () => Get("overworld_remix")},
        {AreaMode.CSide, () => Get("overworld_remix2")}
    };
  #endregion

  #region Generic
    public static string ItemName => GetInternal("genlist_namelabel");
    public static string RenameItem => GetInternal("genlist_rename");
    public static string DeleteItem => GetInternal("genlist_delete");
    public static string CreateNewItem => GetInternal("genlist_createnew");
    public static string ReorderItems => GetInternal("genlist_reorder");
  #endregion

  #region Viewer controls
    public static Dictionary<bool, Func<string>> ViewerFocusStates = new() {
        {false, () => GetInternal("viewercontrols_menulabel")},
        {true, () => GetInternal("viewercontrols_exitmenulabel")}
    };
    public static string ViewerMode => GetInternal("viewercontrols_modelabel");
    public static Dictionary<int, Func<string>> ViewerModes = new() {
        {(int)UI.GraphViewer.Modes.Disabled, () => GetInternal("viewermode_disabled")},
        {(int)UI.GraphViewer.Modes.Routing, () => GetInternal("viewermode_routing")},
        {(int)UI.GraphViewer.Modes.Editing, () => GetInternal("viewermode_editing")}
    };
  #endregion

  #region Graph menu
    public static string GraphMenuHeader => GetInternal("graphmenu_header");
    public static string GraphMenuChooser => GetInternal("graphmenu_choose");
    public static string GraphMenuEditWeights => GetInternal("graphmenu_editing_weights");
    public static string GraphMenuAddPoint => GetInternal("graphmenu_editing_addpt");
    public static string GraphMenuAddConnection => GetInternal("graphmenu_editing_addconn");
  #endregion

  #region Route menu
    public static string RouteMenuHeader => GetInternal("routemenu_header");
    public static string RouteMenuChooser => GetInternal("routemenu_choose");
    public static string RouteIsComplete => GetInternal("routemenu_complete");
    public static string RouteIsPossible => GetInternal("routemenu_possible");
    public static string RouteWeightHeader => GetInternal("routemenu_weightsheader");
    public static string RouteVariableHeader => GetInternal("routemenu_varsheader");
  #endregion

  #region Selection info
    public static string SelectionHeader => GetInternal("graphsel_header");
    public static string SelectedNothing => GetInternal("graphsel_empty");
    public static string SelectedMultiplePoints => GetInternal("graphsel_points");
    public static string SelectedMultipleConnections => GetInternal("graphsel_conns");
    public static string SelectionWeightList => GetInternal("graphsel_weights");
    public static string SelectionRequirementList => GetInternal("graphsel_requires");
    public static string SelectionResultList => GetInternal("graphsel_results");
    public static string PointSelectionID => GetInternal("graphsel_ptid");
    public static string PointSelectionX => GetInternal("graphsel_ptx");
    public static string PointSelectionY => GetInternal("graphsel_pty");
    public static string PointSelectionImage => GetInternal("graphsel_ptimage");
    public static string PointSelectionEndpointType => GetInternal("graphsel_ptend");
    public static string PointSelectionFastTravelType => GetInternal("graphsel_ptfasttravel");
    public static string ConnectionSelectionFrom => GetInternal("graphsel_connfrom");
    public static string ConnectionSelectionTo => GetInternal("graphsel_connto");

    public static Dictionary<string, Func<string>> PointEndTypes = new() {
        {Data.Point.EndType.None, () => GetInternal("genopt_na")},
        {Data.Point.EndType.Start, () => GetInternal("graphsel_ptend_start")},
        {Data.Point.EndType.Finish, () => GetInternal("graphsel_ptend_finish")}
    };
  #endregion

  #region Graph/route chooser
    public static string ChooserAssignedHeader => GetInternal("chooser_assignedheader");
    public static string ChooserAssignedSubHeader => GetInternal("chooser_assignedsubheader");
    public static string ChooserUnassignedHeader => GetInternal("chooser_unassignedheader");
    public static string ChooserChooseItem => GetInternal("chooser_choose");
    public static string ChooserUnassignItem => GetInternal("chooser_unassign");
  #endregion

  #region Weights
    public static string WeightFormatLabel => GetInternal("weighteditor_format");
    public static class WeightFormats {
        public static string Integer => GetInternal("weightformat_int");
        public static string Float => GetInternal("weightformat_float");
        public static string Time => GetInternal("weightformat_time");
    }
  #endregion

  #region Requirements
    public static string RequirementGroupType => GetInternal("reqeditor_grouptype");
    public static string EditRequirement => GetInternal("reqeditor_edit");
    public static string ChangeRequirementGroup => GetInternal("reqeditor_changegroup");
    public static class RequirementGroupTypes {
        public static string All => GetInternal("reqviewer_all");
        public static string Any => GetInternal("reqviewer_any");
    }
  #endregion

  #region Results
    public static string EditResultVariable => GetInternal("reseditor_var");
    public static string EditResultValue => GetInternal("reseditor_value");
    public static class ResultGroupTypes {
        public static string FirstVisit => GetInternal("reseditor_firstvisit");
        public static string EachVisit => GetInternal("reseditor_eachvisit");
        //TODO results that only get applied if an expression is true
    }
  #endregion

  #region IO issues
    public static string IOFailCreateRoot => GetInternal("io_failcreaterootdir", false);
    public static string IOFailOpenRoot => GetInternal("io_failopenrootdir", false);
    public static string IOFailOpenFile => GetInternal("io_failopenfile", false);
    public static string IOFailOpenFileList => GetInternal("io_failopenfilelist", false);
    public static string IOFailOpenFileItem => GetInternal("io_failopenfileitem", false);
  #endregion
}