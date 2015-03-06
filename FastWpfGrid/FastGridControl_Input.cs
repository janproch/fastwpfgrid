using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FastWpfGrid
{
    partial class FastGridControl
    {
        public event Action<object, ColumnClickEventArgs> ColumnHeaderClick;
        public event Action<object, RowClickEventArgs> RowHeaderClick;
        public event EventHandler SelectedCellsChanged;

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            var pt = e.GetPosition(image);
            var cell = GetCellAddress(pt);

            using (var ctx = CreateInvalidationContext())
            {
                if (cell.IsCell)
                {
                    _selectedCells.ToList().ForEach(InvalidateCell);
                    _selectedCells.Clear();
                    if (_currentCell == cell)
                    {
                        ShowInlineEditor(_currentCell);
                    }
                    else
                    {
                        HideInlinEditor();
                        SetCurrentCell(cell);
                    }
                    _dragStartCell = cell;
                    _selectedCells.Add(cell);
                    OnChangeSelectedCells();
                }

                int? resizingColumn = GetResizingColumn(pt);
                if (resizingColumn != null)
                {
                    Cursor = Cursors.SizeWE;
                    _resizingColumn = resizingColumn;
                    _resizingColumnOrigin = pt;
                    _resizingColumnStartSize = _columnSizes.GetSize(_resizingColumn.Value);
                    CaptureMouse();
                }

                if (resizingColumn == null && cell.IsColumnHeader)
                {
                    if (ColumnHeaderClick != null)
                        ColumnHeaderClick(this, new ColumnClickEventArgs
                        {
                            Grid = this,
                            Column = cell.Column.Value,
                        });
                }
                if (cell.IsRowHeader)
                {
                    if (RowHeaderClick != null)
                        RowHeaderClick(this, new RowClickEventArgs
                        {
                            Grid = this,
                            Row = cell.Row.Value,
                        });
                }
            }

            //if (cell.IsCell) ShowTextEditor(
            //    GetCellRect(cell.Row.Value, cell.Column.Value),
            //    Model.GetCell(cell.Row.Value, cell.Column.Value).GetEditText());
        }

        private void edTextKeyDown(object sender, KeyEventArgs e)
        {
            using (var ctx = CreateInvalidationContext())
            {
                if (e.Key == Key.Escape)
                {
                    HideInlinEditor(false);
                    e.Handled = true;
                }
                if (e.Key == Key.Enter)
                {
                    HideInlinEditor();
                    MoveCurrentCell(_currentCell.Row + 1, _currentCell.Column, e);
                }

                HandleCursorMove(e, false);
                if (e.Handled) HideInlinEditor();
            }
        }

        private void imageMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0) vscroll.Value = vscroll.Value + vscroll.LargeChange / 2;
            if (e.Delta > 0) vscroll.Value = vscroll.Value - vscroll.LargeChange / 2;
            ScrollChanged();
        }

        private static bool ControlPressed
        {
            get { return (Keyboard.Modifiers & ModifierKeys.Control) != 0; }
        }

        private void HandleCursorMove(KeyEventArgs e, bool allowLeftRight = true)
        {
            if (e.Key == Key.Up && _currentCell.Row > 0) MoveCurrentCell(_currentCell.Row - 1, _currentCell.Column, e);
            else if (e.Key == Key.Down) MoveCurrentCell(_currentCell.Row + 1, _currentCell.Column, e);
            else if (e.Key == Key.Left && allowLeftRight) MoveCurrentCell(_currentCell.Row, _currentCell.Column - 1, e);
            else if (e.Key == Key.Right && allowLeftRight) MoveCurrentCell(_currentCell.Row, _currentCell.Column + 1, e);

            else if (e.Key == Key.Home && ControlPressed) MoveCurrentCell(0, 0, e);
            else if (e.Key == Key.End && ControlPressed) MoveCurrentCell(_rowCount - 1, _columnCount - 1, e);
            else if (e.Key == Key.PageDown && ControlPressed) MoveCurrentCell(_rowCount - 1, _currentCell.Column, e);
            else if (e.Key == Key.PageUp && ControlPressed) MoveCurrentCell(0, _currentCell.Column, e);
            else if (e.Key == Key.Home) MoveCurrentCell(_currentCell.Row, 0, e);
            else if (e.Key == Key.End) MoveCurrentCell(_currentCell.Row, _columnCount - 1, e);
            else if (e.Key == Key.PageDown) MoveCurrentCell(_currentCell.Row + VisibleRowCount, _currentCell.Column, e);
            else if (e.Key == Key.PageUp) MoveCurrentCell(_currentCell.Row - VisibleRowCount, _currentCell.Column, e);
        }

        private void imageKeyDown(object sender, KeyEventArgs e)
        {
            using (var ctx = CreateInvalidationContext())
            {
                HandleCursorMove(e);

                if (e.Key == Key.F2 && _currentCell.IsCell)
                {
                    ShowInlineEditor(_currentCell);
                }
            }
        }

        private void imageTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!_currentCell.IsCell) return;
            if (e.Text == null) return;
            if (e.Text != " " && String.IsNullOrEmpty(e.Text.Trim())) return;
            if (e.Text.Length == 1 && e.Text[0] < 32) return;
            ShowInlineEditor(_currentCell, e.Text);
        }

        private void imageMouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(image);
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            using (var ctx = CreateInvalidationContext())
            {
                var pt = e.GetPosition(image);
                var cell = GetCellAddress(pt);

                if (_resizingColumn.HasValue)
                {
                    int newSize = _resizingColumnStartSize.Value + (int)Math.Round(pt.X - _resizingColumnOrigin.Value.X);
                    if (newSize < MinColumnWidth) newSize = MinColumnWidth;
                    if (newSize > GridScrollAreaWidth) newSize = GridScrollAreaWidth;
                    _columnSizes.Resize(_resizingColumn.Value, newSize);
                    InvalidateAll();
                }
                else
                {
                    int? column = GetResizingColumn(pt);
                    if (column != null) Cursor = Cursors.SizeWE;
                    else Cursor = Cursors.Arrow;
                }

                if (_dragStartCell.IsCell && cell.IsCell)
                {
                    _isInvalidated = true;
                    var newSelected = GetCellRange(_dragStartCell, cell);
                    foreach (var added in newSelected)
                    {
                        if (_selectedCells.Contains(added)) continue;
                        InvalidateCell(added);
                    }
                    foreach (var removed in _selectedCells)
                    {
                        if (newSelected.Contains(removed)) continue;
                        InvalidateCell(removed);
                    }
                    _selectedCells = newSelected;
                    SetCurrentCell(cell);
                    OnChangeSelectedCells();
                }

                SetHoverRow(cell.IsCell ? cell.Row.Value : (int?)null);
                SetHoverRowHeader(cell.IsRowHeader ? cell.Row.Value : (int?)null);
                SetHoverColumnHeader(cell.IsColumnHeader ? cell.Column.Value : (int?)null);
            }
        }

        private void OnChangeSelectedCells()
        {
            if (SelectedCellsChanged != null) SelectedCellsChanged(this, EventArgs.Empty);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            using (var ctx = CreateInvalidationContext())
            {
                SetHoverRow(null);
                SetHoverRowHeader(null);
                SetHoverColumnHeader(null);
            }
        }
    }
}
