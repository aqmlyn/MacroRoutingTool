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
    /// Combined width of all columns in this <see cref="TableMenu"/>, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>. 
    /// </summary>
    public float FullWidth { get => ColumnFormats.Aggregate(0f, (sum, format) => sum += format._measure); }
    /// <summary>
    /// Width of the display area for this <see cref="TableMenu"/>, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>. 
    /// </summary>
    public float DisplayWidth = float.MaxValue;
    /// <inheritdoc cref="DisplayWidth"/>
    public new float Width {
        get => DisplayWidth;
        set {DisplayWidth = value;}
    }

    /// <summary>
    /// Combined height of all rows in this <see cref="TableMenu"/>. 
    /// </summary>
    public float FullHeight { get => RowFormats.Aggregate(0f, (sum, format) => sum += format._measure); }
    /// <summary>
    /// Height of the display area for this <see cref="TableMenu"/>, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>. 
    /// </summary>
    public float DisplayHeight = float.MaxValue;
    /// <inheritdoc cref="DisplayHeight"/>
    public new float Height {
        get => DisplayHeight;
        set {DisplayHeight = value;}
    }

    /// <summary>
    /// Representation of a <see cref="TableMenu"/> as an item in another <see cref="TextMenu"/> 
    /// (which may or may not itself be a <see cref="TableMenu"/>). 
    /// </summary>
    public class AsItem : Item {
        /// <summary>
        /// The <see cref="TableMenu"/> this item represents.
        /// </summary>
        public TableMenu Menu;

        /// <inheritdoc cref="AsItem"/> 
        public AsItem() {
            IncludeWidthInMeasurement = false;
        }

        /// <summary>
        /// Called when <see cref="Menu"/> is to gain control.
        /// </summary>
        public void GainFocus() {
            if (Container != null) {
                Container.Focused = false;
                Container.RenderAsFocused = true;
            }
            Menu.Focused = true;
            Menu.RenderAsFocused = false;
            Menu.FirstSelection();
            foreach (var row in Menu.Rows) {
                row.HoverIndex = row.FirstPossibleHover;
            }
        }

        /// <summary>
        /// Called when <see cref="Menu"/> is to lose control.
        /// </summary>
        public void LoseFocus() {
            Menu.RenderAsFocused = true;
            Menu.Focused = false;
            if (Container != null) {
                Container.RenderAsFocused = false;
                Container.Focused = true;
            }
        }

        /// <summary>
        /// Determines whether this item can be hovered.
        /// </summary>
        public virtual bool SelectableCheck() => Menu.Items[0].Selectable || Menu.FirstPossibleSelection != 0;

        public override float LeftWidth() => Menu.AllowShrinkWidth ? Math.Min(Menu.FullWidth, Menu.DisplayWidth) : Menu.DisplayWidth;
        public override float Height() => Menu.AllowShrinkHeight ? Math.Min(Menu.FullHeight, Menu.DisplayHeight) : Menu.DisplayHeight;
        public override void Render(Vector2 position, bool highlighted) {
            Menu.Position.X = position.X + Menu.Width * Menu.Justify.X;
            Menu.Position.Y = position.Y + Menu.Height * (Menu.Justify.Y - 0.5f);
            Menu.Render();
        }

        public override void Update() {
            Selectable = SelectableCheck();
            if (!Menu.Focused && Container.Current == this) {
                GainFocus();
            }
            if (Menu.Focused) {
                if (Input.MenuUp.Pressed && (!Selectable || Menu.Selection == Menu.FirstPossibleSelection) && Container != null && Container.FirstPossibleSelection < Container.Items.IndexOf(this)) {
                    Input.MenuUp.ConsumePress();
                    LoseFocus();
                    Container.MoveSelection(-1);
                }
                if (Input.MenuDown.Pressed && (!Selectable || Menu.Selection == Menu.LastPossibleSelection) && Container != null && Container.LastPossibleSelection > Container.Items.IndexOf(this)) {
                    Input.MenuDown.ConsumePress();
                    LoseFocus();
                    Container.MoveSelection(1);
                }
            }
            Menu.Update();
        }
    }

    /// <inheritdoc cref="AsItem"/>
    public class AsCollapsibleItem : AsItem {
        /// <summary>
        /// Whether this item is currently collapsed.
        /// </summary>
        public bool Collapsed;

        /// <summary>
        /// The <see cref="TextElement"/> shown when this item is collapsed.
        /// </summary>
        public TextElement CollapsedLabel = new(){Justify = new(0f, 0.5f), BorderThickness = 2f};

        /// <summary>
        /// Whether to automatically set <see cref="CollapsedLabel"/>'s color based on whether
        /// this item is currently hovered.  
        /// </summary>
        public bool CollapsedAutoColor = true;

        public override bool SelectableCheck() => Collapsed || base.SelectableCheck();
        public override float LeftWidth() => Collapsed ? CollapsedLabel.Font.Measure(CollapsedLabel.Text).X : base.LeftWidth();
        public override float Height() => Collapsed ? CollapsedLabel.Font.LineHeight : base.Height();

        public override void ConfirmPressed() {
            if (Collapsed) {
                Collapsed = false;
                GainFocus();
            } else {
                base.ConfirmPressed();
            }
        }

        public override void Update() {
            Selectable = SelectableCheck();
            if (Collapsed) {
                CollapsedLabel.Update();
            } else if (Menu.Focused && Input.MenuCancel.Pressed) {
                Input.MenuCancel.ConsumePress();
                LoseFocus();
                Collapsed = true;
                Update();
            } else {
                base.Update();
            }
        }

        public override void Render(Vector2 position, bool highlighted) {
            if (Collapsed) {
                CollapsedLabel.Position = position;
                if (CollapsedAutoColor) { CollapsedLabel.Color = highlighted ? Container.HighlightColor : Color.White; }
                CollapsedLabel.Render();
            } else {
                base.Render(position, highlighted);
            }
        }
    }

    /// <summary>
    /// Create a new <see cref="AsItem"/> containing this <see cref="TableMenu"/>, add it to the given <see cref="TextMenu"/>
    /// (which may or may not itself be a <see cref="TableMenu"/>), and return it.  
    /// </summary>
    public AsItem MakeSubmenuIn(TextMenu menu) {
        AsItem asItem = new(){Menu = this, Container = menu};
        menu.Add(asItem);
        return asItem;
    }

    /// <summary>
    /// Create a new <see cref="AsCollapsibleItem"/> containing this <see cref="TableMenu"/>, add it to the given <see cref="TextMenu"/>
    /// (which may or may not itself be a <see cref="TableMenu"/>), and return it.  
    /// </summary>
    public AsCollapsibleItem MakeSubmenuCollapsedIn(TextMenu menu) {
        AsCollapsibleItem asItem = new() { Menu = this, Container = menu, Collapsed = true };
        menu.Add(asItem);
        return asItem;
    }

    /// <summary>
    /// Stores configuration for formatting each <see cref="CellItem"/> in one row or column of a <see cref="TableMenu"/>. 
    /// </summary>
    public class AxisFormat {
        /// <summary>
        /// Minimum cross measure of each item on this axis, including margins.
        /// </summary>
        public float? MinMeasure = null;
        /// <summary>
        /// Maximum cross measure of each item on this axis, including margins.
        /// </summary>
        public float? MaxMeasure = null;
        /// <summary>
        /// Backing field for <see cref="Measure"/>. 
        /// </summary>
        public float _measure = 0f;
        /// <summary>
        /// The cross measure of each item on this axis, including margins.
        /// </summary>
        public virtual float Measure {
            get => _measure;
            set { MinMeasure = MaxMeasure = _measure = value; }
        }

        /// <summary>
        /// Top or left margin of each item on this axis.
        /// </summary>
        public float? MarginBefore = 0f;
        /// <summary>
        /// Bottom or right margin of each item on this axis.
        /// </summary>
        public float? MarginAfter = 0f;

        /// <summary>
        /// Coordinate on this axis of the position at which each item on this axis will be drawn. (0, 0) is top left and (1, 1) is bottom right,
        /// regardless of the containing <see cref="TableMenu"/>'s <see cref="TextMenu.Justify"/>.  
        /// </summary>
        public float Justify = 0f;
    }
    /// <summary>
    /// Formatting for each row of this <see cref="TableMenu"/>. 
    /// </summary>
    public List<AxisFormat> RowFormats = [];
    /// <summary>
    /// Formatting for each column of this <see cref="TableMenu"/>. 
    /// </summary>
    public List<AxisFormat> ColumnFormats = [];

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
    /// Assign measurements to each row and column in the table.
    /// </summary>
    public void AssignMeasures() {
        //set each line's cross measure using the cell in that line with the longest individual cross measure.
        foreach (var row in Rows) {
            foreach (var cell in row.Items) {
                if (cell != null) {
                    var rowFormat = RowFormats.EnsureGet(cell.Row, _ => new());
                    rowFormat._measure = Math.Max(rowFormat._measure, Math.Max(rowFormat.MinMeasure ?? 0f, Math.Min(rowFormat.MaxMeasure ?? float.PositiveInfinity, cell.UnrestrictedHeight())));
                    var columnFormat = ColumnFormats.EnsureGet(cell.Column, _ => new());
                    columnFormat._measure = Math.Max(columnFormat._measure, Math.Max(columnFormat.MinMeasure ?? 0f, Math.Min(columnFormat.MaxMeasure ?? float.PositiveInfinity, cell.UnrestrictedWidth())));
                }
            }
        }

        //if either axis's new full cross measure is less than its display cross measure and that isn't allowed,
        //expand that axis to fill the display measure
        var fullWidth = FullWidth;
        if (!AllowShrinkWidth && fullWidth < DisplayWidth) {
            var expandableFormats = ColumnFormats.Where(format => format._measure < format.MaxMeasure).ToList();
            while (expandableFormats.Count != 0) {
                int i = 0;
                while (i < expandableFormats.Count) {
                    var desiredExpansionRatio = DisplayWidth / (fullWidth = FullWidth);
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
        var fullHeight = FullHeight;
        if (!AllowShrinkHeight && fullHeight < DisplayHeight) {
            var expandableFormats = RowFormats.Where(format => format._measure < format.MaxMeasure).ToList();
            while (expandableFormats.Count != 0) {
                int i = 0;
                while (i < expandableFormats.Count) {
                    var desiredExpansionRatio = DisplayHeight / (fullHeight = FullHeight);
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

        base.Width = Math.Min(fullWidth, DisplayWidth);
        base.Height = Math.Min(fullHeight, DisplayHeight);
    }

    /// <summary>
    /// Menu item intended to appear as a cell in a table.
    /// </summary>
    public class CellItem : Item {
        /// <summary>
        /// Index (0-indexed) of the <see cref="TableMenu.Row"/> that contains this item in its <see cref="TableMenu"/>. 
        /// </summary>
        public int Row = 0;
        /// <summary>
        /// Index (0-indexed) of the column that contains this item in its <see cref="TableMenu.Row"/>. 
        /// </summary>
        public int Column = 0;

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
                if (Container is TableMenu table) {
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
                if (Container is TableMenu table) {
                    return Math.Min(_marginBottom ?? 0f, table.RowFormats[Row].MarginAfter ?? _marginBottom ?? 0f);
                }
                return _marginBottom;
            }
            set { _marginBottom = value; }
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
    }

    /// <summary>
    /// Get a list of all <see cref="Row"/> items in this <see cref="TableMenu"/>. 
    /// </summary>
    public List<Row> Rows => [.. Items.OfType<Row>()];

    /// <summary>
    /// Create a new <see cref="Row"/>, add it to this <see cref="TableMenu"/>'s <see cref="TextMenu.Items"/>, and return it.   
    /// </summary>
    public Row AddRow() {
        Row row = new(){Container = this};
        Items.Add(row);
        Add(row.SelectWiggler = new()); //only want individual items to wiggle, not whole rows. but SelectWiggler being null crashes the game
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
                    item.Row = table.Rows.IndexOf(this);
                    item.Column = Items.Count - 1;
                }
            }
        }

        public override void Added() {
            foreach (var item in Items) {
                item.Container = Container;
            }
            base.Added();
        }

        public override void Update() {
            foreach (var item in Items) {
                item.Update();
            }
            Selectable = Items.Any(item => item.Selectable);
            base.Update();
        }

        public override void Render(Vector2 position, bool highlighted) {
            Vector2 origPosition = new(position.X, position.Y);
            var height = Height();
            position.Y -= height / 2f; //TextMenus expect items to be rendering text with justify.Y = 0.5f, but a table's rows start from the top left
            if (Container is TableMenu table) {
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
                        item.Render(new(position.X + width * (item.JustifyX ?? 0.5f), top + height * (item.JustifyY ?? 0.5f)), highlighted);

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
            base.ConfirmPressed();
        }
    }
    
    /// <inheritdoc cref="TableMenu"/>
    public TableMenu() : base() {
        MenuDataContainer dataContainer = this.EnsureDataContainer();
        dataContainer.MenuData.Add(new MultiDisplayData());
    }

    //TODO override Update and Render to allow both horizontal and vertical scroll (TextMenu methods only allow vertical)
    public override void Render() {
        AssignMeasures(); //TODO can probably make a system to check for dimension changes to avoid having to run this every single frame
        base.Render();
    }
}