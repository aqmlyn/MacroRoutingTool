using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.MacroRoutingTool.UI;

partial class TableMenu {
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
    
    partial class CellItem {
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
    }

    /// <summary>
    /// Assign measurements to each row and column in the table.
    /// </summary>
    public void AssignMeasures() {
        NeedsRemeasured = false;

        //group and populate information for each row, column, and cell.
        _rows.Clear();
        _columns.Clear();
        var rowIndex = 0;
        for (var itemIndex = 0; itemIndex < Items.Count; itemIndex++) {
            var item = Items[itemIndex];
            if (item != null && item is Row row) {
                _rows.Add(row);
                row._index = rowIndex;
                for (var columnIndex = 0; columnIndex < row.Items.Count; columnIndex++) {
                    var column = _columns.EnsureGet(columnIndex, _ => new());
                    column.Index = columnIndex;
                    var cell = row.Items[columnIndex];
                    column.Items.EnsureSet(rowIndex, cell, _ => null);
                    if (cell != null) {
                        cell._row = rowIndex;
                        cell._column = columnIndex;
                        //set each cross measure using the cell on each axis with the longest individual cross measure.
                        var rowFormat = RowFormats.EnsureGet(rowIndex, _ => new() { Table = this });
                        rowFormat._measure = Math.Max(rowFormat._measure, Math.Max(rowFormat.MinMeasure ?? 0f, Math.Min(rowFormat.MaxMeasure ?? float.PositiveInfinity, cell.UnrestrictedHeight())));
                        var columnFormat = ColumnFormats.EnsureGet(columnIndex, _ => new() { Table = this });
                        columnFormat._measure = Math.Max(columnFormat._measure, Math.Max(columnFormat.MinMeasure ?? 0f, Math.Min(columnFormat.MaxMeasure ?? float.PositiveInfinity, cell.UnrestrictedWidth())));
                    }
                }
                rowIndex++;
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
}