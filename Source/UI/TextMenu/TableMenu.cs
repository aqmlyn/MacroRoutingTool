using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

/// <summary>
/// A <see cref="TextMenu"/> with custom characteristics:
/// <list type="bullet">
/// <item>Its position and dimensions are manually set. It doesn't have to take up the entire screen or be wide enough to entirely fit every item onscreen.</item>
/// <item>It can act as a submenu in another <see cref="TextMenu"/> (which may or may not itself be a <see cref="TableMenu"/>).</item>
/// <item>Its items are laid out in a table format. It can have more than two columns. Cells cannot literally span multiple lines, but items can be made to appear as such by adding multiple tables to the same menu.</item>
/// </list>
/// </summary>
public partial class TableMenu : TextMenu {
    /// <summary>
    /// Indicates to various measurement-related property getters whether <see cref="AssignMeasures"/> needs to be called before they return.
    /// </summary>
    public bool NeedsRemeasured = true;

    /// <summary>
    /// Backing field for <see cref="FullWidth"/>. 
    /// </summary>
    public float _fullWidth = 0f;
    /// <summary>
    /// Combined width of all columns in this <see cref="TableMenu"/>, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>. 
    /// </summary>
    public float FullWidth { get {
        if (NeedsRemeasured) { AssignMeasures(); }
        return _fullWidth;
    } }
    /// <summary>
    /// Backing field for <see cref="DisplayWidth"/>. 
    /// </summary>
    public float _displayWidth = float.MaxValue;
    /// <summary>
    /// Width of the display area for this <see cref="TableMenu"/>, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>. 
    /// </summary>
    public float DisplayWidth {
        get => _displayWidth;
        set {
            NeedsRemeasured |= _displayWidth != value;
            _displayWidth = value;
        }
    }
    /// <summary>
    /// Backing field for <see cref="Width"/>. 
    /// </summary>
    public float _width = 0f;
    /// <inheritdoc cref="DisplayWidth"/>
    public new float Width {
        get {
            if (NeedsRemeasured) { AssignMeasures(); }
            return _width;
        }
        set {DisplayWidth = value;}
    }

    /// <summary>
    /// Backing field for <see cref="FullHeight"/>. 
    /// </summary>
    public float _fullHeight = 0f;
    /// <summary>
    /// Combined height of all rows in this <see cref="TableMenu"/>. 
    /// </summary>
    public float FullHeight { get {
        if (NeedsRemeasured) { AssignMeasures(); }
        return _fullHeight;
    } }
    /// <summary>
    /// Backing field for <see cref="DisplayHeight"/>. 
    /// </summary>
    public float _displayHeight = float.MaxValue;
    /// <summary>
    /// Height of the display area for this <see cref="TableMenu"/>, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>. 
    /// </summary>
    public float DisplayHeight {
        get => _displayHeight;
        set {
            NeedsRemeasured |= _displayHeight != value;
            _displayHeight = value;
        }
    }
    /// <summary>
    /// Backing field for <see cref="Height"/>. 
    /// </summary>
    public float _height = 0f;
    /// <inheritdoc cref="DisplayHeight"/>
    public new float Height {
        get {
            if (NeedsRemeasured) { AssignMeasures(); }
            return _height;
        }
        set {DisplayHeight = value;}
    }
    
    /// <summary>
    /// If <see cref="FullWidth"/> is less than <see cref="DisplayWidth"/>,
    /// allow setting <see cref="Width"/> to <see cref="FullWidth"/> instead of <see cref="DisplayWidth"/>.  
    /// </summary>
    public bool AllowShrinkWidth = true;
    /// <summary>
    /// If <see cref="FullHeight"/> is less than <see cref="DisplayHeight"/>,
    /// allow setting <see cref="Height"/> to <see cref="FullHeight"/> instead of <see cref="DisplayHeight"/>.  
    /// </summary>
    public bool AllowShrinkHeight = true;

    /// <summary>
    /// Stores configuration for formatting each <see cref="CellItem"/> in one row or column of a <see cref="TableMenu"/>. 
    /// </summary>
    public class LineFormat {
        /// <summary>
        /// The <see cref="TableMenu"/> this is a <see cref="LineFormat"/> for.
        /// </summary>
        public TableMenu Table;

        /// <summary>
        /// Backing field for <see cref="MinMeasure"/>. 
        /// </summary>
        public float? _minMeasure = null;
        /// <summary>
        /// Minimum cross measure of each item on this axis, including margins.
        /// </summary>
        public float? MinMeasure {
            get => _minMeasure;
            set {
                if (Table != null) { Table.NeedsRemeasured |= _minMeasure != value; }
                _minMeasure = value;
            }
        }
        /// <summary>
        /// Backing field for <see cref="MaxMeasure"/>. 
        /// </summary>
        public float? _maxMeasure = null;
        /// <summary>
        /// Maximum cross measure of each item on this axis, including margins.
        /// </summary>
        public float? MaxMeasure {
            get => _maxMeasure;
            set {
                if (Table != null) { Table.NeedsRemeasured |= _maxMeasure != value; }
                _maxMeasure = value;
            }
        }
        /// <summary>
        /// Backing field for <see cref="Measure"/>. 
        /// </summary>
        public float _measure = 0f;
        /// <summary>
        /// The cross measure of each item on this axis, including margins.
        /// </summary>
        public virtual float Measure {
            get {
                if (Table != null && Table.NeedsRemeasured) { Table.AssignMeasures(); }
                return _measure;
            }
            set { MinMeasure = MaxMeasure = _measure = value; }
        }

        /// <summary>
        /// Backing field for <see cref="MarginBefore"/>. 
        /// </summary>
        public float? _marginBefore = 0f;
        /// <summary>
        /// Top or left margin of each item on this axis.
        /// </summary>
        public float? MarginBefore {
            get => _marginBefore;
            set {
                if (Table != null) { Table.NeedsRemeasured |= _marginBefore != value; }
                _marginBefore = value;
            }
        }
        /// <summary>
        /// Backing field for <see cref="MarginAfter"/>. 
        /// </summary>
        public float? _marginAfter = 0f;
        /// <summary>
        /// Bottom or right margin of each item on this axis.
        /// </summary>
        public float? MarginAfter {
            get => _marginAfter;
            set {
                if (Table != null) { Table.NeedsRemeasured |= _marginAfter != value; }
                _marginAfter = value;
            }
        }

        /// <summary>
        /// Coordinate on this axis of the position at which each item on this axis will be drawn. (0, 0) is top left and (1, 1) is bottom right,
        /// regardless of the containing <see cref="TableMenu"/>'s <see cref="TextMenu.Justify"/>.  
        /// </summary>
        public float Justify = 0f;
    }
    /// <summary>
    /// Formatting for each row of this <see cref="TableMenu"/>. 
    /// </summary>
    public List<LineFormat> RowFormats = [];
    /// <summary>
    /// Formatting for each column of this <see cref="TableMenu"/>. 
    /// </summary>
    public List<LineFormat> ColumnFormats = [];

    /// <summary>
    /// Whether this table is navigable with the cursor. Allows pressing the menu direction binds to pan over any cell item,
    /// even ones that aren't hoverable.<br/><br/>
    /// If false, navigation is expected to be handled by each hoverable item's <see cref="CellItem.OnNavigateUp"/>,
    /// <see cref="CellItem.OnNavigateDown"/>, <see cref="CellItem.OnNavigateLeft"/>, and <see cref="CellItem.OnNavigateRight"/>.<br/><br/>
    /// If true, those actions will still be used for cells at the edges of the table. If the actions don't exist or don't consume the
    /// associated inputs, the default action is to wrap around to the opposite edge of the table if that axis has any lines with
    /// at least 3 hoverable items, and to do nothing otherwise.
    /// </summary>
    public bool UseNavigationCursor = true;

    /// <summary>
    /// Default action used when navigating up from the top row of this table, regardless of <see cref="UseNavigationCursor"/>,
    /// when <see cref="CellItem.OnNavigateUp"/> does not exist or does not consume <see cref="Input.MenuUp"/>.  
    /// </summary>
    public Action DefaultNavigateUpFromTop;
    /// <summary>
    /// Default action used when navigating down from the bottom row of this table, regardless of <see cref="UseNavigationCursor"/>,
    /// when <see cref="CellItem.OnNavigateDown"/> does not exist or does not consume <see cref="Input.MenuDown"/>.  
    /// </summary>
    public Action DefaultNavigateDownFromBottom;
    /// <summary>
    /// Default action used when navigating left from the leftmost column of this table, regardless of <see cref="UseNavigationCursor"/>,
    /// when <see cref="CellItem.OnNavigateLeft"/> does not exist or does not consume <see cref="Input.MenuLeft"/>.  
    /// </summary>
    public Action DefaultNavigateLeftFromLeft;
    /// <summary>
    /// Default action used when navigating right from the rightmost column of this table, regardless of <see cref="UseNavigationCursor"/>,
    /// when <see cref="CellItem.OnNavigateRight"/> does not exist or does not consume <see cref="Input.MenuRight"/>.
    /// </summary>
    public Action DefaultNavigateRightFromRight;

    /// <summary>
    /// Assign measurements to each row and column in the table.
    /// </summary>
    public void AssignMeasures() {
        NeedsRemeasured = false;

        //set each line's cross measure using the cell in that line with the longest individual cross measure.
        var rows = Rows;
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++) {
            var row = rows[rowIndex];
            if (row != null) {
                for (var columnIndex = 0; columnIndex < row.Items.Count; columnIndex++) {
                    var cell = row.Items[columnIndex];
                    if (cell != null) {
                        cell._row = rowIndex;
                        cell._column = columnIndex;
                        var rowFormat = RowFormats.EnsureGet(rowIndex, _ => new() { Table = this });
                        rowFormat._measure = Math.Max(rowFormat._measure, Math.Max(rowFormat.MinMeasure ?? 0f, Math.Min(rowFormat.MaxMeasure ?? float.PositiveInfinity, cell.UnrestrictedHeight())));
                        var columnFormat = ColumnFormats.EnsureGet(columnIndex, _ => new() { Table = this });
                        columnFormat._measure = Math.Max(columnFormat._measure, Math.Max(columnFormat.MinMeasure ?? 0f, Math.Min(columnFormat.MaxMeasure ?? float.PositiveInfinity, cell.UnrestrictedWidth())));
                    }
                }
            }
        }

        //if either axis's new full cross measure is less than its display cross measure and that isn't allowed,
        //expand that axis to fill the display measure
        var fullWidth = ColumnFormats.Aggregate(0f, (sum, format) => sum += format._measure);
        if (!AllowShrinkWidth && fullWidth < _displayWidth) {
            var expandableFormats = ColumnFormats.Where(format => format._measure < format.MaxMeasure).ToList();
            while (expandableFormats.Count != 0) {
                int i = 0;
                while (i < expandableFormats.Count) {
                    var desiredExpansionRatio = _displayWidth / (fullWidth = ColumnFormats.Aggregate(0f, (sum, format) => sum += format._measure));
                    var sumOfExpandables = expandableFormats.Aggregate(0f, (sum, format) => sum += format._measure);
                    var newMeasure = expandableFormats[i]._measure * (expandableFormats[i]._measure / sumOfExpandables) * desiredExpansionRatio;
                    if (newMeasure > (expandableFormats[i].MaxMeasure ?? float.PositiveInfinity)) {
                        expandableFormats[i]._measure = expandableFormats[i].MaxMeasure ?? float.MaxValue;
                        expandableFormats.RemoveAt(i);
                    } else {
                        expandableFormats[i]._measure = newMeasure;
                        i++;
                    }
                }
            }
        }
        var fullHeight = RowFormats.Aggregate(0f, (sum, format) => sum += format._measure);
        if (!AllowShrinkHeight && fullHeight < _displayHeight) {
            var expandableFormats = RowFormats.Where(format => format._measure < format.MaxMeasure).ToList();
            while (expandableFormats.Count != 0) {
                int i = 0;
                while (i < expandableFormats.Count) {
                    var desiredExpansionRatio = _displayHeight / (fullHeight = RowFormats.Aggregate(0f, (sum, format) => sum += format._measure));
                    var sumOfExpandables = expandableFormats.Aggregate(0f, (sum, format) => sum += format._measure);
                    var newMeasure = expandableFormats[i]._measure * (expandableFormats[i]._measure / sumOfExpandables) * desiredExpansionRatio;
                    if (newMeasure > (expandableFormats[i].MaxMeasure ?? float.PositiveInfinity)) {
                        expandableFormats[i]._measure = expandableFormats[i].MaxMeasure ?? float.MaxValue;
                        expandableFormats.RemoveAt(i);
                    } else {
                        expandableFormats[i]._measure = newMeasure;
                        i++;
                    }
                }
            }
        }

        //update the relevant fields
        _width = base.Width = Math.Min(fullWidth, _displayWidth);
        _fullWidth = fullWidth;
        _height = base.Height = Math.Min(fullHeight, _displayHeight);
        _fullHeight = fullHeight;
    }

  #region Cells
    /// <summary>
    /// Menu item intended to appear as a cell in a table.
    /// </summary>
    public class CellItem : Item {
        /// <summary>
        /// Backing field for <see cref="Row"/>. 
        /// </summary>
        public int _row = 0;
        /// <summary>
        /// Index (0-indexed) of the <see cref="TableMenu.Row"/> that contains this item in its <see cref="TableMenu"/>. 
        /// </summary>
        public int Row {
            get {
                if (Container != null && Container is TableMenu table && table.NeedsRemeasured) { table.AssignMeasures(); }
                return _row;
            }
        }
        /// <summary>
        /// Backing field for <see cref="Column"/>. 
        /// </summary>
        public int _column = 0;
        /// <summary>
        /// Index (0-indexed) of the column that contains this item in its <see cref="TableMenu.Row"/>. 
        /// </summary>
        public int Column {
            get {
                if (Container != null && Container is TableMenu table && table.NeedsRemeasured) { table.AssignMeasures(); }
                return _column;
            }
        }

        /// <summary>
        /// Backing field for <see cref="JustifyX"/>. 
        /// </summary>
        public float? _justifyX = null;
        /// <summary>
        /// X coordinate of this item's origin relative to its bounding box. 0 is on the left edge, 1 is on the right edge.<br/>
        /// Assign null to use the column's justify value instead.
        /// </summary>
        public float? JustifyX {
            get {
                if (_justifyX == null && Container is TableMenu table && table.ColumnFormats.Count > Column) {
                    return table.ColumnFormats[Column].Justify;
                }
                return _justifyX;
            }
            set { _justifyX = value; }
        }
        /// <summary>
        /// Backing field for <see cref="JustifyY"/>.
        /// </summary>
        public float? _justifyY = null;
        /// <summary>
        /// Y coordinate of this item's origin relative to its bounding box. 0 is on the top edge, 1 is on the bottom edge.<br/>
        /// Assign null to use the row's justify value instead.
        /// </summary>
        public float? JustifyY {
            get {
                if (_justifyY == null && Container is TableMenu table && table.RowFormats.Count > Row) {
                    return table.RowFormats[Row].Justify;
                }
                return _justifyY;
            }
            set { _justifyY = value; }
        }
        
        /// <summary>
        /// Backing field for <see cref="MarginLeft"/>. 
        /// </summary>
        public float? _marginLeft = 0f;
        /// <summary>
        /// Margin to add to this cell's left side.
        /// </summary>
        public float? MarginLeft {
            get {
                if (Container is TableMenu table && table.ColumnFormats.Count > Column) {
                    return Math.Min(_marginLeft ?? 0f, table.ColumnFormats[Column].MarginBefore ?? _marginLeft ?? 0f);
                }
                return _marginLeft;
            }
            set { _marginLeft = value; }
        }
        /// <summary>
        /// Backing field for <see cref="MarginRight"/>. 
        /// </summary>
        public float? _marginRight = 0f;
        /// <summary>
        /// Margin to add to this cell's right side.
        /// </summary>
        public float? MarginRight {
            get {
                if (Container is TableMenu table && table.ColumnFormats.Count > Column) {
                    return Math.Min(_marginRight ?? 0f, table.ColumnFormats[Column].MarginAfter ?? _marginRight ?? 0f);
                }
                return _marginRight;
            }
            set { _marginRight = value; }
        }
        /// <summary>
        /// Backing field for <see cref="MarginTop"/>. 
        /// </summary>
        public float? _marginTop = 0f;
        /// <summary>
        /// Margin to add to this cell's top side.
        /// </summary>
        public float? MarginTop {
            get {
                if (Container is TableMenu table && table.RowFormats.Count > Row) {
                    return Math.Min(_marginTop ?? 0f, table.RowFormats[Row].MarginBefore ?? _marginTop ?? 0f);
                }
                return _marginTop;
            }
            set { _marginTop = value; }
        }
        /// <summary>
        /// Backing field for <see cref="MarginBottom"/>. 
        /// </summary>
        public float? _marginBottom = 0f;
        /// <summary>
        /// Margin to add to this cell's bottom side.
        /// </summary>
        public float? MarginBottom {
            get {
                if (Container is TableMenu table && table.RowFormats.Count > Row) {
                    return Math.Min(_marginBottom ?? 0f, table.RowFormats[Row].MarginAfter ?? _marginBottom ?? 0f);
                }
                return _marginBottom;
            }
            set { _marginBottom = value; }
        }
        
        /// <summary>
        /// Performed when <see cref="Input.MenuUp"/> is pressed with this item hovered.<br/><inheritdoc cref="XMLDoc.ConsumeInputToPreventNavigation"/>
        /// </summary>
        public Action OnUpPressed;
        /// <summary>
        /// Performed when <see cref="Input.MenuDown"/> is pressed with this item hovered.<br/><inheritdoc cref="XMLDoc.ConsumeInputToPreventNavigation"/>
        /// </summary>
        public Action OnDownPressed;
        /// <summary>
        /// Performed when <see cref="Input.MenuLeft"/> is pressed with this item hovered.<br/><inheritdoc cref="XMLDoc.ConsumeInputToPreventNavigation"/>
        /// </summary>
        public Action OnLeftPressed;
        /// <summary>
        /// Performed when <see cref="Input.MenuRight"/> is pressed with this item hovered.<br/><inheritdoc cref="XMLDoc.ConsumeInputToPreventNavigation"/>
        /// </summary>
        public Action OnRightPressed;
        /// <summary>
        /// Performed when <see cref="Input.MenuCancel"/> is pressed with this item hovered.
        /// </summary>
        public Action OnCancelPressed;

        /// <summary>
        /// Performed after <see cref="OnUpPressed"/> if that action didn't consume the input.<br/><inheritdoc cref="XMLDoc.NavigationActionExpectations"/>
        /// </summary>
        public Action OnNavigateUp;
        /// <summary>
        /// Performed after <see cref="OnDownPressed"/> if that action didn't consume the input.<br/><inheritdoc cref="XMLDoc.NavigationActionExpectations"/> 
        /// </summary>
        public Action OnNavigateDown;
        /// <summary>
        /// Performed after <see cref="OnLeftPressed"/> if that action didn't consume the input.<br/><inheritdoc cref="XMLDoc.NavigationActionExpectations"/>
        /// </summary>
        public Action OnNavigateLeft;
        /// <summary>
        /// Performed after <see cref="OnRightPressed"/> if that action didn't consume the input.<br/><inheritdoc cref="XMLDoc.NavigationActionExpectations"/>
        /// </summary>
        public Action OnNavigateRight;

        /// <summary>
        /// Default action used when navigating up from the top row of a table, regardless of the
        /// table's <see cref="UseNavigationCursor"/>, when neither this item's <see cref="OnNavigateUp"/>
        /// nor the table's <see cref="TableMenu.DefaultNavigateUpFromTop"/> consumes <see cref="Input.MenuUp"/>.  
        /// </summary>
        public void DefaultNavigateUpFromTop() {
            bool isHoverable(Row row) => row.Items.Count > _column && row.Items[_column] != null && row.Items[_column].Hoverable;
            if (Container != null) {
                if (Container is TableMenu table) {
                    int hoverables = 0;
                    var rows = table.Rows;
                    foreach (var row in rows) {
                        if (isHoverable(row) && hoverables++ == 3) {
                            Input.MenuUp.ConsumePress();
                            table.Selection = table.Items.IndexOf(rows.Last(isHoverable));
                            return;
                        }
                    }
                }
                Container.MoveSelection(-1, true);
            }
        }
        
        /// <summary>
        /// Default action used when navigating down from the bottom row of a table, regardless of the
        /// table's <see cref="UseNavigationCursor"/>, when neither this item's <see cref="OnNavigateDown"/>
        /// nor the table's <see cref="TableMenu.DefaultNavigateDownFromBottom"/> consumes <see cref="Input.MenuDown"/>.  
        /// </summary>
        public void DefaultNavigateDownFromBottom() {
            bool isHoverable(Row row) => row.Items.Count > _column && row.Items[_column] != null && row.Items[_column].Hoverable;
            if (Container != null) {
                if (Container is TableMenu table) {
                    int hoverables = 0;
                    var rows = table.Rows;
                    foreach (var row in rows) {
                        if (isHoverable(row) && hoverables++ == 3) {
                            Input.MenuDown.ConsumePress();
                            table.Selection = table.Items.IndexOf(rows.First(isHoverable));
                            return;
                        }
                    }
                }
                Container.MoveSelection(1, true);
            }
        }
        
        /// <summary>
        /// Default action used when navigating left from the leftmost column of a table, regardless of the
        /// table's <see cref="UseNavigationCursor"/>, when neither this item's <see cref="OnNavigateLeft"/>
        /// nor the table's <see cref="TableMenu.DefaultNavigateLeftFromLeft"/> consumes <see cref="Input.MenuLeft"/>.  
        /// </summary>
        public void DefaultNavigateLeftFromLeft() {
            if (Container != null) {
                if (Container is TableMenu table) {
                    foreach (var row in table.Rows) {
                        int hoverables = 0;
                        foreach (var item in row.Items) {
                            if (item.Hoverable && hoverables++ == 3) {
                                Input.MenuLeft.ConsumePress();
                                row.HoverIndex = row.LastPossibleHover;
                                return;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Default action used when navigating right from the rightmost column of a table, regardless of the
        /// table's <see cref="UseNavigationCursor"/>, when neither this item's <see cref="OnNavigateRight"/>
        /// nor the table's <see cref="TableMenu.DefaultNavigateRightFromRight"/> consumes <see cref="Input.MenuRight"/>.  
        /// </summary>
        public void DefaultNavigateRightFromRight() {
            if (Container != null) {
                if (Container is TableMenu table) {
                    foreach (var row in table.Rows) {
                        int hoverables = 0;
                        foreach (var item in row.Items) {
                            if (item.Hoverable && hoverables++ == 3) {
                                Input.MenuRight.ConsumePress();
                                row.HoverIndex = row.FirstPossibleHover;
                                return;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The minimum allowed width for this item as specified in the containing <see cref="TableMenu"/>'s
        /// <see cref="ColumnFormats"/>, accounting for items that span multiple columns.
        /// </summary>
        public float MinWidth() {
            if (Container is TableMenu table) {
                return table.ColumnFormats[Column].MinMeasure ?? 0f;
            }
            return 0f;
        }
        
        /// <summary>
        /// The minimum allowed height for this item as specified in the containing <see cref="TableMenu"/>'s
        /// <see cref="RowFormats"/>, accounting for items that span multiple rows.
        /// </summary>
        public float MinHeight() {
            if (Container is TableMenu table) {
                return table.RowFormats[Row].MinMeasure ?? 0f;
            }
            return 0f;
        }

        /// <summary>
        /// The maximum allowed width for this item as specified in the containing <see cref="TableMenu"/>'s
        /// <see cref="ColumnFormats"/>, accounting for items that span multiple columns.
        /// </summary>
        public float MaxWidth() {
            if (Container is TableMenu table) {
                return table.ColumnFormats[Column].MaxMeasure ?? float.PositiveInfinity;
            }
            return float.PositiveInfinity;
        }

        /// <summary>
        /// The maximum allowed height for this item as specified in the containing <see cref="TableMenu"/>'s
        /// <see cref="RowFormats"/>, accounting for items that span multiple rows.
        /// </summary>
        public float MaxHeight() {
            if (Container is TableMenu table) {
                return table.RowFormats[Row].MaxMeasure ?? float.PositiveInfinity;
            }
            return float.PositiveInfinity;
        }

        /// <summary>
        /// Width this item would have if there were no minimum or maximum width constraints.
        /// </summary>
        public virtual float UnrestrictedWidth() => 0f;
        /// <summary>
        /// Height this item would have if there were no minimum or maximum height constraints.
        /// </summary>
        public virtual float UnrestrictedHeight() => 0f;

        /// <summary>
        /// The scale that would be required to maintain this item's aspect ratio while fitting it in its restrictions
        /// (<see cref="MinWidth"/>, <see cref="MaxWidth"/>, <see cref="MinHeight"/>, and <see cref="MaxHeight"/>). 
        /// </summary>
        public float FitScale => Math.Min(1f, Math.Min(MaxWidth() / Math.Max(MinWidth(), UnrestrictedWidth()), MaxHeight() / Math.Max(MinHeight(), UnrestrictedHeight())));

        public override float LeftWidth() {
            if (Container != null && Container is TableMenu table) {
                return table.ColumnFormats[Column].Measure;
            }
            return base.LeftWidth();
        }

        public override float Height() {
            if (Container != null && Container is TableMenu table) {
                return table.RowFormats[Row].Measure;
            }
            return base.Height();
        }

        public override void LeftPressed() {
            OnLeftPressed?.Invoke();
            if (Input.MenuLeft.Pressed) { OnNavigateLeft?.Invoke(); }
            if (Input.MenuLeft.Pressed) {
                if (Container != null && Container is TableMenu table) {
                    table.DefaultNavigateLeftFromLeft?.Invoke();
                    if (!Input.MenuLeft.Pressed) { goto Base; }
                }
                DefaultNavigateLeftFromLeft();
            }
            Base:
            base.LeftPressed();
        }

        public override void RightPressed() {
            OnRightPressed?.Invoke();
            if (Input.MenuRight.Pressed) { OnNavigateRight?.Invoke(); }
            if (Input.MenuRight.Pressed) {
                if (Container != null && Container is TableMenu table) {
                    table.DefaultNavigateRightFromRight?.Invoke();
                    if (!Input.MenuRight.Pressed) { goto Base; }
                }
                DefaultNavigateRightFromRight();
            }
            Base:
            base.RightPressed();
        }
        
        /// <inheritdoc cref="UI.XMLDoc"/> 
        public class XMLDoc {
            /// <summary>
            /// If the input should do something other than navigate to another item, this action
            /// is expected to call <see cref="VirtualButton.ConsumePress"/> on the input.<br/>
            /// Otherwise, this action shouldn't do anything, and the corresponding navigation action
            /// should be used to handle the input.
            /// </summary>
            public const bool ConsumeInputToPreventNavigation = true;

            /// <summary>
            /// Navigation actions are expected to move the hover to another menu item where it makes sense to do so.
            /// </summary>
            public const bool NavigationActionExpectations = true;
        }
    }
  #endregion

  #region Rows
    /// <summary>
    /// Get a list of all <see cref="Row"/> items in this <see cref="TableMenu"/>. 
    /// </summary>
    public List<Row> Rows => [.. Items.OfType<Row>()];

    /// <summary>
    /// Create a new <see cref="Row"/>, add it to this <see cref="TableMenu"/>'s <see cref="TextMenu.Items"/>, and return it.   
    /// </summary>
    public Row AddRow() {
        NeedsRemeasured = true;
        Row row = new(){Container = this};
        Items.Add(row);
        Add(row.SelectWiggler = new()); //only want individual items to wiggle, not whole rows. but SelectWiggler being null crashes the game
        RowFormats.Add(new() { Table = this });
        return row;
    }

    /// <summary>
    /// Container for a row of <see cref="TextMenu.Item"/>s. This is itself an item to be added to a
    /// <see cref="TableMenu"/>'s <see cref="TextMenu.Items"/> list.   
    /// </summary>
    public class Row : Item {
        /// <summary>
        /// Index in <see cref="Items"/> of the item currently hovered, or -1 if this row does not contain any <see cref="TextMenu.Item.Selectable"/> items.
        /// </summary>
        public int HoverIndex = -1;
        /// <summary>
        /// The item in <see cref="Items"/> currently hovered, if any.
        /// </summary>
        public CellItem HoveredItem {get {
            if (HoverIndex >= 0 && HoverIndex < Items.Count) {
                return Items[HoverIndex];
            }
            return null;
        }}

        /// <summary>
        /// Index of the first hoverable item in this row.
        /// </summary>
        public int FirstPossibleHover => Items.FindIndex(item => item.Selectable);
        /// <summary>
        /// Index of the last hoverable item in this row.
        /// </summary>
        public int LastPossibleHover => Items.FindLastIndex(item => item.Selectable);

        /// <summary>
        /// The items in this row.
        /// </summary>
        public List<CellItem> Items = [];

        /// <summary>
        /// Performed when <see cref="Input.MenuCancel"/> is pressed with this item hovered. 
        /// </summary>
        public Action OnCancelPressed;

        /// <summary>
        /// Add the given item to the end of this row.
        /// </summary>
        public void Add(CellItem item) {
            Items.Add(item);
            if (Container != null) {
                item.Container = Container;
                //bigger movement than vanilla items since it won't always be obvious where the hover went
                Container.Add([item.SelectWiggler = Wiggler.Create(0.25f, 4f)]);
                item.SelectWiggler.UseRawDeltaTime = true;
                if (Container is TableMenu table) {
                    table.NeedsRemeasured = true;
                    item._row = table.Rows.IndexOf(this);
                    item._column = Items.Count - 1;
                    table.ColumnFormats.EnsureGet(item.Column, _ => new() { Table = table });
                }
            }
        }

        public override void Update() {
            foreach (var item in Items) {
                item.Update();
            }
            Selectable = Items.Any(item => item.Selectable);
            var origOnAltPressed = OnAltPressed;
            var origOnCancelPressed = OnCancelPressed;
            if (HoveredItem != null) {
                OnAltPressed = () => {
                    HoveredItem.OnAltPressed?.Invoke();
                    if (Input.MenuJournal.Pressed) { origOnAltPressed?.Invoke(); }
                };
                OnCancelPressed = () => {
                    HoveredItem.OnCancelPressed?.Invoke();
                    if (Input.MenuCancel.Pressed) { origOnCancelPressed?.Invoke(); }
                };
                if (Input.MenuUp.Pressed) {
                    HoveredItem.OnUpPressed?.Invoke();
                    if (Input.MenuUp.Pressed) { HoveredItem.OnNavigateUp?.Invoke(); }
                    if (Input.MenuUp.Pressed) {
                        if (Container != null && Container is TableMenu table) {
                            table.DefaultNavigateUpFromTop?.Invoke();
                            if (!Input.MenuUp.Pressed) { goto Down; }
                        }
                        HoveredItem.DefaultNavigateUpFromTop();
                    }
                }
                Down:
                if (Input.MenuDown.Pressed) {
                    HoveredItem.OnDownPressed?.Invoke();
                    if (Input.MenuDown.Pressed) { HoveredItem.OnNavigateDown?.Invoke(); }
                    if (Input.MenuDown.Pressed) {
                        if (Container != null && Container is TableMenu table) {
                            table.DefaultNavigateDownFromBottom?.Invoke();
                            if (!Input.MenuDown.Pressed) { goto Base; }
                        }
                        HoveredItem.DefaultNavigateDownFromBottom();
                    }
                }
            }
            Base:
            base.Update();
            OnAltPressed = origOnAltPressed;
            OnCancelPressed = origOnCancelPressed;
        }

        public override void Render(Vector2 position, bool highlighted) {
            Vector2 origPosition = new(position.X, position.Y);
            if (Container is TableMenu table) {
                var height = Height();
                position.Y -= height / 2f; //TextMenus expect items to be rendering text with justify.Y = 0.5f, but rows can have different justifies
                var tableTopLeft = new Vector2(table.Position.X - table.Justify.X * table.Width, table.Position.Y - table.Justify.Y * table.Height);
                if (position.Y > Engine.Height || position.Y - tableTopLeft.Y > table.Height) { return; } //if row is below display area, cull row
                if (position.Y + height < 0f || position.Y + height < tableTopLeft.Y) { return; } //if row is above display area, cull row
                for (int column = 0; column < Items.Count; column++) {
                    var item = Items[column];
                    var width = table.ColumnFormats[column].Measure;
                    if (item != null) {
                        if (item.MarginTop + item.MarginBottom > height || item.MarginLeft + item.MarginRight > width) { continue; } //if item's margins leave no room, cull item
                        var top = position.Y + (item.MarginTop ?? 0f);
                        if (top > Engine.Height || top - tableTopLeft.Y > table.Height) { continue; } //if item is below display area only due to top margin, cull item
                        position.X += item.MarginLeft ?? 0f;
                        if (position.X > Engine.Width || position.X - tableTopLeft.X > table.Width) { return; } //if item is right of display area, cull rest of row
                        if (position.X + width < 0f || position.X + width < tableTopLeft.X) { continue; } //if item is left of display area, cull item
                        item.Render(new(position.X + width * (item.JustifyX ?? 0.5f), top + height * (item.JustifyY ?? 0.5f)), highlighted && HoverIndex == column);
                    }
                    position.X += width + (item?.MarginRight ?? 0f);
                }
            }
            base.Render(origPosition, highlighted);
        }

        public override float Height() => Items.Count == 0 ? 0f : Items.Max(item => item.Height());
        public override float LeftWidth() => Items.Count == 0 ? 0f : Items.Aggregate(0f, (sum, item) => sum += item.LeftWidth());

        public override void ConfirmPressed() {
            HoveredItem?.ConfirmPressed();
            HoveredItem?.OnPressed?.Invoke();
            if (Input.MenuConfirm.Pressed) {base.ConfirmPressed();}
        }

        public override void LeftPressed() {
            if (HoveredItem != null) {
                HoveredItem.LeftPressed();
                if (Input.MenuLeft.Pressed) { HoveredItem.OnNavigateLeft?.Invoke(); }
            }
            base.LeftPressed();
        }

        public override void RightPressed() {
            if (HoveredItem != null) {
                HoveredItem.RightPressed();
                if (Input.MenuRight.Pressed) { HoveredItem.OnNavigateRight?.Invoke(); }
            }
            base.RightPressed();
        }
    }
  #endregion

    /// <inheritdoc cref="TableMenu"/>
    public TableMenu() : base() {
        MenuDataContainer dataContainer = this.EnsureDataContainer();
        dataContainer.MenuData.Add(new MultiDisplayData());
    }

    //TODO when UseNavigationCursor is enabled,
    // - set AutoScroll to false (and set it back to true when disabling)
    // - CellItem.DefaultNavigate... methods should pan the cursor instead of moving the hover
    // - AssignMeasures should calculate an offset based on the pan position that Render should then apply to the vector it passes (like vanilla ScrollTargetY)
    
    public override void Update() {
        var origOnCancel = OnCancel;
        if (Focused && !(new VirtualButton[] {Input.MenuUp, Input.MenuDown, Input.MenuLeft, Input.MenuRight}).Any(input => input.Pressed)) {
            OnCancel = (() => ((Action)Current?.GetType().GetField("OnCancelPressed")?.GetValue(Current))?.Invoke()) + origOnCancel;
        }
        base.Update();
        OnCancel = origOnCancel;
    }
    
    public override void Render() {
        if (NeedsRemeasured) {AssignMeasures();}
        base.Render();
    }
}