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
  #region Cells
    /// <summary>
    /// Menu item intended to appear as a cell in a table.
    /// </summary>
    public partial class CellItem : Item {
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
        public partial class XMLDoc;
    }
  #endregion

  #region Rows
    /// <summary>
    /// Backing field for <see cref="Rows"/>. 
    /// </summary>
    public List<Row> _rows = [];
  
    /// <summary>
    /// Get a list of all <see cref="Row"/> items in this <see cref="TableMenu"/>. 
    /// </summary>
    public List<Row> Rows {
        get {
            if (NeedsRemeasured) { AssignMeasures(); }
            return _rows;
        }
        set {
            NeedsRemeasured |= _rows != value;
            _rows = value;
        }
    }

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
    /// Container for a row of <see cref="CellItem"/>s in a table.
    /// </summary>
    /// <remarks>
    /// I originally wrote this class as extending <see cref="TextMenu.Item"/> so that rows could be used alongside vanilla items in
    /// any menu. I later decided to write the <see cref="AsItem"/> class which obsoletes this functionality, but it still works, so
    /// for now I'm leaving it as is. If it becomes overly difficult to maintain this way, I'll consider rewriting the class to be simpler. 
    /// </remarks>
    public partial class Row : Item {
        /// <summary>
        /// Backing field for <see cref="Index"/>. 
        /// </summary>
        public int _index = -1;
        /// <summary>
        /// Position (0-indexed) of this row in the table.
        /// </summary>
        public int Index {get {
            if (Container != null && Container is TableMenu table && table.NeedsRemeasured) { table.AssignMeasures(); }
            return _index;
        }}

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
                Container.Add([item.SelectWiggler = Wiggler.Create(0.25f, 3f)]);
                item.SelectWiggler.UseRawDeltaTime = true;
                if (Container is TableMenu table) {
                    table.NeedsRemeasured = true;
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
            if ((Container?.Focused ?? false) && HoveredItem != null) {
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
                    if (Input.MenuUp.Pressed) { TryNavigateUp(); }
                }
                if (Input.MenuDown.Pressed) {
                    HoveredItem.OnDownPressed?.Invoke();
                    if (Input.MenuDown.Pressed) { TryNavigateDown(); }
                }
            }
            base.Update();
            OnAltPressed = origOnAltPressed;
            OnCancelPressed = origOnCancelPressed;
        }

        public override void Render(Vector2 position, bool hovered) {
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
                        item.Render(new(position.X + width * (item.JustifyX ?? 0.5f), top + height * (item.JustifyY ?? 0.5f)), hovered && HoverIndex == column);
                    }
                    position.X += width + (item?.MarginRight ?? 0f);
                }
            }
            base.Render(origPosition, hovered);
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
                if (Input.MenuLeft.Pressed) { TryNavigateLeft(); }
            }
            base.LeftPressed();
        }

        public override void RightPressed() {
            if (HoveredItem != null) {
                HoveredItem.RightPressed();
                if (Input.MenuRight.Pressed) { TryNavigateRight(); }
            }
            base.RightPressed();
        }
    }
  #endregion
  
  #region Columns
    /// <summary>
    /// Backing field for <see cref="Columns"/>. 
    /// </summary>
    public List<Column> _columns = [];
    
    /// <summary>
    /// List of <see cref="Column"/> objects for this table, grouping its <see cref="Row"/>s' items into vertical lines. 
    /// </summary>
    public List<Column> Columns {
        get {
            if (NeedsRemeasured) { AssignMeasures(); }
            return _columns;
        }
        set {
            NeedsRemeasured |= _columns != value;
            _columns = value;
        }
    }
  
    /// <summary>
    /// Container for a column (vertical line) of <see cref="CellItem"/>s in a table. 
    /// </summary>
    public class Column {
        /// <summary>
        /// Position (0-indexed) of this column in the table.
        /// </summary>
        public int Index = -1;

        /// <summary>
        /// The items in this column.
        /// </summary>
        public List<CellItem> Items = [];

        /// <summary>
        /// Index of the first hoverable item in this column.
        /// </summary>
        public int FirstPossibleHover => Items.FindIndex(item => item.Selectable);
        /// <summary>
        /// Index of the last hoverable item in this column.
        /// </summary>
        public int LastPossibleHover => Items.FindLastIndex(item => item.Selectable);
    }
  #endregion

    /// <inheritdoc cref="TableMenu"/>
    public TableMenu() : base() {
        MenuDataContainer dataContainer = this.EnsureDataContainer();
        dataContainer.MenuData.Add(new MultiDisplayData());
    }

    public override void Update() {
        var origAutoScroll = AutoScroll;
        AutoScroll &= !UseNavigationCursor;
        var origOnCancel = OnCancel;
        if (Focused && !(new VirtualButton[] {Input.MenuUp, Input.MenuDown, Input.MenuLeft, Input.MenuRight}).Any(input => input.Pressed)) {
            OnCancel = () => {
                ((Action)Current?.GetType().GetField("OnCancelPressed")?.GetValue(Current))?.Invoke();
                if (Input.MenuCancel.Pressed) { origOnCancel?.Invoke(); }
            };
        }
        base.Update();
        OnCancel = origOnCancel;
        AutoScroll = origAutoScroll;
    }
    
    public override void Render() {
        if (NeedsRemeasured) {AssignMeasures();}
        if (UseNavigationCursor) {
            //TODO once MultiDisplayData.ScrollOffset and .ScrollTarget are working, set them according to CursorItem's center
        }
        base.Render();
    }
}