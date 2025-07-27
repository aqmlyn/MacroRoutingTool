using System;
using System.Linq;

namespace Celeste.Mod.MacroRoutingTool.UI;

partial class TableMenu {
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
    /// A <see cref="TableMenu"/>'s navigation cursor, used as a fallback to ensure a table is fully navigable
    /// even when its cells don't have working navigation actions.
    /// </summary>
    public class NavigationCursor {
        /// <summary>
        /// Row of the item the cursor is over.
        /// </summary>
        public int Row = 0;
        /// <summary>
        /// Column of the item the cursor is over.
        /// </summary>
        public int Column = 0;
    }

    /// <summary>
    /// This table's navigation cursor, used as a fallback to ensure the table is fully navigable
    /// even when its cells don't have working navigation actions.
    /// </summary>
    public NavigationCursor Cursor = new();

    /// <summary>
    /// The item this table's navigation cursor is currently over, if any.
    /// </summary>
    public Item CursorItem {get {
        if (Rows.Count > Cursor.Row && Columns.Count > Cursor.Column) { return Rows[Cursor.Row].Items[Cursor.Column]; }
        return null;
    }}

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
    /// Throw an exception if the cursor is out of bounds.
    /// </summary>
    /// <exception cref="Exception">The table is empty.</exception>
    /// <exception cref="IndexOutOfRangeException">One of the cursor's fields is out of bounds of the list of items for the associated dimension.</exception>
    public void CursorBoundsCheck() {
        if (Columns.Count == 0) {
            throw new Exception("This table is empty!");
        }
        if (Cursor.Row < 0 || Cursor.Row >= Rows.Count) {
            throw new IndexOutOfRangeException($"{nameof(Cursor)}.{nameof(Cursor.Row)} ({Cursor.Row}) is out of bounds of {nameof(Rows)} (0-{Rows.Count - 1}).");
        }
        if (Cursor.Column < 0 || Cursor.Column >= Columns.Count) {
            throw new IndexOutOfRangeException($"{nameof(Cursor)}.{nameof(Cursor.Column)} ({Cursor.Column}) is out of bounds of {nameof(Columns)} (0-{Columns.Count - 1}).");
        }
    }
    
    /// <summary>
    /// Move the navigation cursor up, including by wrapping to the bottom of the table, until it reaches another non-null item.
    /// </summary>
    public void MoveCursorUp() {
        CursorBoundsCheck();
        for (int i = Cursor.Row - 1; i != Cursor.Row; i = (--i + Rows.Count) % Rows.Count) {
            if (Columns[Cursor.Column].Items[i] != null) {
                Cursor.Row = i;
                return;
            }
        }
    }
    
    /// <summary>
    /// Move the navigation cursor down, including by wrapping to the top of the table, until it reaches another non-null item.
    /// </summary>
    public void MoveCursorDown() {
        CursorBoundsCheck();
        for (int i = Cursor.Row + 1; i != Cursor.Row; i = (++i + Rows.Count) % Rows.Count) {
            if (Columns[Cursor.Column].Items[i] != null) {
                Cursor.Row = i;
                return;
            }
        }
    }
    
    /// <summary>
    /// Move the navigation cursor left, including by wrapping to the right side of the table, until it reaches another non-null item.
    /// </summary>
    public void MoveCursorLeft() {
        CursorBoundsCheck();
        for (int i = Cursor.Column - 1; i != Cursor.Column; i = (--i + Columns.Count) % Columns.Count) {
            if (Rows[Cursor.Row].Items[i] != null) {
                Cursor.Column = i;
                return;
            }
        }
    }
    
    /// <summary>
    /// Move the navigation cursor right, including by wrapping to the left side of the table, until it reaches another non-null item.
    /// </summary>
    public void MoveCursorRight() {
        CursorBoundsCheck();
        for (int i = Cursor.Column + 1; i != Cursor.Column; i = (++i + Columns.Count) % Columns.Count) {
            if (Rows[Cursor.Row].Items[i] != null) {
                Cursor.Column = i;
                return;
            }
        }
    }
    
    partial class CellItem {
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
        /// Default action used when navigating up from the top row of a table when neither this item's <see cref="OnNavigateUp"/>
        /// nor the table's <see cref="TableMenu.DefaultNavigateUpFromTop"/> consumes <see cref="Input.MenuUp"/>.  
        /// </summary>
        public void DefaultNavigateUpFromTop() {
            bool isHoverable(Row row) => row.Items.Count > _column && row.Items[_column] != null && row.Items[_column].Hoverable;
            if (Container != null) {
                Input.MenuUp.ConsumePress();
                if (Container is TableMenu table) {
                    int hoverables = 0;
                    var rows = table.Rows;
                    foreach (var row in rows) {
                        if (isHoverable(row) && hoverables++ == 3) {
                            table.Selection = table.Items.IndexOf(rows.Last(isHoverable));
                            return;
                        }
                    }
                }
                Container.MoveSelection(-1, true);
            }
        }
        
        /// <summary>
        /// Default action used when navigating down from the bottom row of a table when neither this item's <see cref="OnNavigateDown"/>
        /// nor the table's <see cref="TableMenu.DefaultNavigateDownFromBottom"/> consumes <see cref="Input.MenuDown"/>.  
        /// </summary>
        public void DefaultNavigateDownFromBottom() {
            bool isHoverable(Row row) => row.Items.Count > _column && row.Items[_column] != null && row.Items[_column].Hoverable;
            if (Container != null) {
                Input.MenuDown.ConsumePress();
                if (Container is TableMenu table) {
                    int hoverables = 0;
                    var rows = table.Rows;
                    foreach (var row in rows) {
                        if (isHoverable(row) && hoverables++ == 3) {
                            table.Selection = table.Items.IndexOf(rows.First(isHoverable));
                            return;
                        }
                    }
                }
                Container.MoveSelection(1, true);
            }
        }
        
        /// <summary>
        /// Default action used when navigating left from the leftmost column of a table when neither this item's <see cref="OnNavigateLeft"/>
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
        /// Default action used when navigating right from the rightmost column of a table when neither this item's <see cref="OnNavigateRight"/>
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
        
        partial class XMLDoc {
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
    
    partial class Row {
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
        /// If <see cref="Input.MenuUp"/> hasn't been consumed already,
        /// <list type="bullet">
        /// <item>If this row's <see cref="Container"/> is a <see cref="TableMenu"/> which is using its navigation cursor,
        /// move the cursor and return.</item>
        /// <item>Call the hovered item's <see cref="CellItem.OnNavigateUp"/>. If it consumes the input, return.</item>
        /// <item>If this row's <see cref="Container"/> is not a <see cref="TableMenu"/>, skip to the last step.</item>
        /// <item>If the hovered item is not the first hoverable item in its column, return.</item>
        /// <item>Call the table's <see cref="DefaultNavigateUpFromTop"/>. If it consumes the input, return.</item>
        /// <item>Call the hovered item's <see cref="CellItem.DefaultNavigateUpFromTop"/>.</item>
        /// </list>
        /// </summary>
        /// <exception cref="NullReferenceException">This row's <see cref="Container"/> or <see cref="HoveredItem"/> is null.</exception>
        public void TryNavigateUp() {
            if (Container == null) { throw new NullReferenceException($"This row's {nameof(Container)} is null."); }
            if (HoveredItem == null) { throw new NullReferenceException($"This row's {nameof(HoveredItem)} is null."); }
            if (!Input.MenuUp.Pressed) { return; }
            var table = Container as TableMenu;
            if (table != null && table.UseNavigationCursor) {
                table.MoveCursorUp();
                return;
            }
            HoveredItem.OnNavigateUp?.Invoke();
            if (!Input.MenuUp.Pressed) { return; }
            if (table != null) {
                if (table.Columns[HoveredItem._column].FirstPossibleHover != Index) { return; }
                table.DefaultNavigateUpFromTop?.Invoke();
                if (!Input.MenuUp.Pressed) { return; }
            }
            HoveredItem.DefaultNavigateUpFromTop();
        }
        
        /// <summary>
        /// If <see cref="Input.MenuDown"/> hasn't been consumed already,
        /// <list type="bullet">
        /// <item>If this row's <see cref="Container"/> is a <see cref="TableMenu"/> which is using its navigation cursor,
        /// move the cursor and return.</item>
        /// <item>Call the hovered item's <see cref="CellItem.OnNavigateDown"/>. If it consumes the input, return.</item>
        /// <item>If this row's <see cref="Container"/> is not a <see cref="TableMenu"/>, skip to the last step.</item>
        /// <item>If the hovered item is not the last hoverable item in its column, return.</item>
        /// <item>Call the table's <see cref="DefaultNavigateDownFromBottom"/>. If it consumes the input, return.</item>
        /// <item>Call the hovered item's <see cref="CellItem.DefaultNavigateDownFromBottom"/>.</item>
        /// </list>
        /// </summary>
        /// <exception cref="NullReferenceException">This row's <see cref="Container"/> or <see cref="HoveredItem"/> is null.</exception>
        public void TryNavigateDown() {
            if (HoveredItem == null) { throw new NullReferenceException($"The row's {nameof(HoveredItem)} is null."); }
            if (!Input.MenuDown.Pressed) { return; }
            var table = Container as TableMenu;
            if (table != null && table.UseNavigationCursor) {
                table.MoveCursorDown();
                return;
            }
            HoveredItem.OnNavigateDown?.Invoke();
            if (!Input.MenuDown.Pressed) { return; }
            if (table != null) {
                if (table.Columns[HoveredItem._column].LastPossibleHover != Index) { return; }
                table.DefaultNavigateDownFromBottom?.Invoke();
                if (!Input.MenuDown.Pressed) { return; }
            }
            HoveredItem.DefaultNavigateDownFromBottom();
        }
        
        /// <summary>
        /// If <see cref="Input.MenuLeft"/> hasn't been consumed already,
        /// <list type="bullet">
        /// <item>If this row's <see cref="Container"/> is a <see cref="TableMenu"/> which is using its navigation cursor,
        /// move the cursor and return.</item>
        /// <item>Call the hovered item's <see cref="CellItem.OnNavigateLeft"/>. If it consumes the input, return.</item>
        /// <item>If this row's <see cref="Container"/> is not a <see cref="TableMenu"/>, skip to the last step.</item>
        /// <item>If the hovered item is not the first hoverable item in its row, return.</item>
        /// <item>Call the table's <see cref="DefaultNavigateLeftFromLeft"/>. If it consumes the input, return.</item>
        /// <item>Call the hovered item's <see cref="CellItem.DefaultNavigateLeftFromLeft"/>.</item>
        /// </list>
        /// </summary>
        /// <exception cref="NullReferenceException">This row's <see cref="Container"/> or <see cref="HoveredItem"/> is null.</exception>
        public void TryNavigateLeft() {
            if (HoveredItem == null) { throw new NullReferenceException($"The row's {nameof(HoveredItem)} is null."); }
            if (!Input.MenuLeft.Pressed) { return; }
            var table = Container as TableMenu;
            if (table != null && table.UseNavigationCursor) {
                table.MoveCursorLeft();
                return;
            }
            HoveredItem.OnNavigateLeft?.Invoke();
            if (!Input.MenuLeft.Pressed) { return; }
            if (table != null) {
                if (FirstPossibleHover != HoveredItem._column) { return; }
                table.DefaultNavigateLeftFromLeft?.Invoke();
                if (!Input.MenuLeft.Pressed) { return; }
            }
            HoveredItem.DefaultNavigateLeftFromLeft();
        }
        
        /// <summary>
        /// If <see cref="Input.MenuRight"/> hasn't been consumed already,
        /// <list type="bullet">
        /// <item>If this row's <see cref="Container"/> is a <see cref="TableMenu"/> which is using its navigation cursor,
        /// move the cursor and return.</item>
        /// <item>Call the hovered item's <see cref="CellItem.OnNavigateRight"/>. If it consumes the input, return.</item>
        /// <item>If this row's <see cref="Container"/> is not a <see cref="TableMenu"/>, skip the next two steps.</item>
        /// <item>If the hovered item is not the last hoverable item in its row, return.</item>
        /// <item>Call the table's <see cref="DefaultNavigateRightFromRight"/>. If it consumes the input, return.</item>
        /// <item>Call the hovered item's <see cref="CellItem.DefaultNavigateRightFromRight"/>.</item>
        /// </list>
        /// </summary>
        /// <exception cref="NullReferenceException">This row's <see cref="Container"/> or <see cref="HoveredItem"/> is null.</exception>
        public void TryNavigateRight() {
            if (HoveredItem == null) { throw new NullReferenceException($"The row's {nameof(HoveredItem)} is null."); }
            if (!Input.MenuRight.Pressed) { return; }
            var table = Container as TableMenu;
            if (table != null && table.UseNavigationCursor) {
                table.MoveCursorRight();
                return;
            }
            HoveredItem.OnNavigateRight?.Invoke();
            if (!Input.MenuRight.Pressed) { return; }
            if (table != null) {
                if (LastPossibleHover != HoveredItem._column) { return; }
                table.DefaultNavigateRightFromRight?.Invoke();
                if (!Input.MenuRight.Pressed) { return; }
            }
            HoveredItem.DefaultNavigateRightFromRight();
        }
    }
}