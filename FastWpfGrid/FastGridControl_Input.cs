using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FastWpfGrid
{
    partial class FastGridControl
    {
        public static readonly object ToggleTransposedCommand = new object();
        public static readonly object ToggleAllowFlexibleRowsCommand = new object();

        public class ActiveRegion
        {
            public IntRect Rect;
            public object CommandParameter;
            public string Tooltip;
        }

        public event Action<object, ColumnClickEventArgs> ColumnHeaderClick;
        public event Action<object, RowClickEventArgs> RowHeaderClick;
        public event EventHandler<SelectionChangedEventArgs> SelectedCellsChanged;
        public List<ActiveRegion> CurrentCellActiveRegions = new List<ActiveRegion>();
        public ActiveRegion CurrentHoverRegion;
        private Point? _mouseCursorPoint;
        private ToolTip _tooltip;
        private object _tooltipTarget;
        private string _tooltipText;
        private DispatcherTimer _tooltipTimer;
        private FastGridCellAddress _dragStartCell;
        private FastGridCellAddress _mouseOverCell;
        private bool _mouseOverCellIsTrimmed;
        private int? _mouseOverRow;
        private int? _mouseOverRowHeader;
        private int? _mouseOverColumnHeader;
        private FastGridCellAddress _inplaceEditorCell;
        private FastGridCellAddress _shiftDragStartCell;
        private bool _inlineTextChanged;
        public event EventHandler ScrolledModelRows;
        public event EventHandler ScrolledModelColumns;
        private FastGridCellAddress _showCellEditorIfMouseUp;

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _showCellEditorIfMouseUp = FastGridCellAddress.Empty;

            var pt = e.GetPosition(image);
            pt.X *= DpiDetector.DpiXKoef;
            pt.Y *= DpiDetector.DpiYKoef;
            var cell = GetCellAddress(pt);

            var currentRegion = CurrentCellActiveRegions.FirstOrDefault(x => x.Rect.Contains(pt));
            if (currentRegion != null)
            {
                HandleCommand(cell, currentRegion.CommandParameter);
                return;
            }

            using (var ctx = CreateInvalidationContext())
            {
                int? resizingColumn = GetResizingColumn(pt);
                if (resizingColumn != null)
                {
                    Cursor = Cursors.SizeWE;
                    _resizingColumn = resizingColumn;
                    _resizingColumnOrigin = pt;
                    _resizingColumnStartSize = _columnSizes.GetSizeByRealIndex(_resizingColumn.Value);
                    CaptureMouse();
                }

                if (_resizingColumn == null && cell.IsColumnHeader)
                {
                    if (IsTransposed)
                    {
                        OnModelRowClick(_columnSizes.RealToModel(cell.Column.Value));
                    }
                    else
                    {
                        OnModelColumnClick(_columnSizes.RealToModel(cell.Column.Value));
                    }
                }
                if (cell.IsRowHeader)
                {
                    if (IsTransposed)
                    {
                        OnModelColumnClick(_rowSizes.RealToModel(cell.Row.Value));
                    }
                    else
                    {
                        OnModelRowClick(_rowSizes.RealToModel(cell.Row.Value));
                    }
                }

                if (cell.IsCell)
                {
                    if (ControlPressed)
                    {
                        HideInlinEditor();
                        if (_selectedCells.Contains(cell)) _selectedCells.Remove(cell);
                        else _selectedCells.Add(cell);
                        InvalidateCell(cell);
                    }
                    else if (ShiftPressed)
                    {
                        _selectedCells.ToList().ForEach(InvalidateCell);
                        _selectedCells.Clear();

                        HideInlinEditor();
                        foreach (var cellItem in GetCellRange(_currentCell, cell))
                        {
                            _selectedCells.Add(cellItem);
                            InvalidateCell(cellItem);
                        }
                    }
                    else
                    {
                        _selectedCells.ToList().ForEach(InvalidateCell);
                        _selectedCells.Clear();
                        if (_currentCell == cell)
                        {
                            _showCellEditorIfMouseUp = _currentCell;
                        }
                        else
                        {
                            HideInlinEditor();
                            SetCurrentCell(cell);
                        }
                        _dragStartCell = cell;
                        _selectedCells.Add(cell);
                    }
                    OnChangeSelectedCells(true);
                }
            }

            //if (cell.IsCell) ShowTextEditor(
            //    GetCellRect(cell.Row.Value, cell.Column.Value),
            //    Model.GetCell(cell.Row.Value, cell.Column.Value).GetEditText());
        }

        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            _dragStartCell = new FastGridCellAddress();
            //bool wasColumnResizing = false;
            if (_resizingColumn.HasValue)
            {
                _resizingColumn = null;
                _resizingColumnOrigin = null;
                _resizingColumnStartSize = null;
                //wasColumnResizing = true;
                ReleaseMouseCapture();
            }

            var pt = e.GetPosition(image);
            pt.X *= DpiDetector.DpiXKoef;
            pt.Y *= DpiDetector.DpiYKoef;
            var cell = GetCellAddress(pt);

            if (cell == _showCellEditorIfMouseUp)
            {
                ShowInlineEditor(_showCellEditorIfMouseUp);
                _showCellEditorIfMouseUp = FastGridCellAddress.Empty;
            }
        }

        private void edTextChanged(object sender, TextChangedEventArgs e)
        {
            _inlineTextChanged = true;
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);

            var pt = e.GetPosition(image);
            pt.X *= DpiDetector.DpiXKoef;
            pt.Y *= DpiDetector.DpiYKoef;
            var cell = GetCellAddress(pt);

            if (!_selectedCells.Contains(cell))
            {
                using (var ctx = CreateInvalidationContext())
                {
                    InvalidateCell(_currentCell);
                    _selectedCells.ToList().ForEach(InvalidateCell);
                    _selectedCells.Clear();
                    _selectedCells.Add(cell);
                    _currentCell = cell;
                    InvalidateCell(_currentCell);
                    OnChangeSelectedCells(true);
                }
            }
        }

        private void OnModelColumnClick(int column)
        {
            if (ColumnHeaderClick != null && column >= 0 && column < _modelColumnCount)
            {
                ColumnHeaderClick(this, new ColumnClickEventArgs
                    {
                        Grid = this,
                        Column = column,
                    });
            }
        }

        private void OnModelRowClick(int row)
        {
            if (row >= 0 && row < _modelRowCount)
            {
                var args = new RowClickEventArgs
                {
                    Grid = this,
                    Row = row,
                };
                if (RowHeaderClick != null)
                {
                    RowHeaderClick(this, args);
                }
                if (!args.Handled)
                {
                    HideInlinEditor();

                    if (ControlPressed)
                    {
                        foreach (var cell in GetCellRange(ModelToReal(new FastGridCellAddress(row, 0)), ModelToReal(new FastGridCellAddress(row, _modelColumnCount - 1))))
                        {
                            if (_selectedCells.Contains(cell)) _selectedCells.Remove(cell);
                            else _selectedCells.Add(cell);
                            InvalidateCell(cell);
                        }
                    }
                    else if (ShiftPressed)
                    {
                        _selectedCells.ToList().ForEach(InvalidateCell);
                        _selectedCells.Clear();
                        var currentModel = RealToModel(_currentCell);

                        foreach (var cell in GetCellRange(ModelToReal(new FastGridCellAddress(currentModel.Row, 0)), ModelToReal(new FastGridCellAddress(row, _modelColumnCount - 1))))
                        {
                            _selectedCells.Add(cell);
                            InvalidateCell(cell);
                        }
                    }
                    else
                    {
                        _selectedCells.ToList().ForEach(InvalidateCell);
                        _selectedCells.Clear();
                        if (_currentCell.IsCell)
                        {
                            var currentModel = RealToModel(_currentCell);
                            SetCurrentCell(ModelToReal(new FastGridCellAddress(row, currentModel.Column)));
                            _dragStartCell = ModelToReal(new FastGridCellAddress(row, null));
                        }
                        foreach (var cell in GetCellRange(ModelToReal(new FastGridCellAddress(row, 0)), ModelToReal(new FastGridCellAddress(row, _modelColumnCount - 1))))
                        {
                            _selectedCells.Add(cell);
                            InvalidateCell(cell);
                        }
                    }
                }
            }
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

                HandleCursorMove(e, true);
                if (e.Handled) HideInlinEditor();
            }
        }

        private void imageMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ControlPressed)
            {
                if (e.Delta < 0 && CellFontSize < 20) CellFontSize++;
                if (e.Delta > 0 && CellFontSize > 6) CellFontSize--;

                RecountColumnWidths();
                RecountRowHeights();
                AdjustScrollbars();
                SetScrollbarMargin();
                FixScrollPosition();
                InvalidateAll();
            }
            else
            {
                if (e.Delta < 0) vscroll.Value = vscroll.Value + vscroll.LargeChange/2;
                if (e.Delta > 0) vscroll.Value = vscroll.Value - vscroll.LargeChange/2;
                ScrollChanged();
            }
        }

        private static bool ControlPressed
        {
            get { return (Keyboard.Modifiers & ModifierKeys.Control) != 0; }
        }

        private static bool ShiftPressed
        {
            get { return (Keyboard.Modifiers & ModifierKeys.Shift) != 0; }
        }

        private bool HandleCursorMove(KeyEventArgs e, bool isInTextBox = false)
        {
            if (e.Key == Key.Up && ControlPressed) return MoveCurrentCell(0, _currentCell.Column, e);
            if (e.Key == Key.Down && ControlPressed) return MoveCurrentCell(_realRowCount - 1, _currentCell.Column, e);
            if (e.Key == Key.Left && ControlPressed) return MoveCurrentCell(_currentCell.Row, 0, e);
            if (e.Key == Key.Right && ControlPressed) return MoveCurrentCell(_currentCell.Row, _realColumnCount - 1, e);

            if (e.Key == Key.Up) return MoveCurrentCell(_currentCell.Row - 1, _currentCell.Column, e);
            if (e.Key == Key.Down) return MoveCurrentCell(_currentCell.Row + 1, _currentCell.Column, e);
            if (e.Key == Key.Left && !isInTextBox) return MoveCurrentCell(_currentCell.Row, _currentCell.Column - 1, e);
            if (e.Key == Key.Right && !isInTextBox) return MoveCurrentCell(_currentCell.Row, _currentCell.Column + 1, e);

            if (e.Key == Key.Home && ControlPressed) return MoveCurrentCell(0, 0, e);
            if (e.Key == Key.End && ControlPressed) return MoveCurrentCell(_realRowCount - 1, _realColumnCount - 1, e);
            if (e.Key == Key.PageDown && ControlPressed) return MoveCurrentCell(_realRowCount - 1, _currentCell.Column, e);
            if (e.Key == Key.PageUp && ControlPressed) return MoveCurrentCell(0, _currentCell.Column, e);
            if (e.Key == Key.Home && !isInTextBox) return MoveCurrentCell(_currentCell.Row, 0, e);
            if (e.Key == Key.End && !isInTextBox) return MoveCurrentCell(_currentCell.Row, _realColumnCount - 1, e);
            if (e.Key == Key.PageDown) return MoveCurrentCell(_currentCell.Row + VisibleRowCount, _currentCell.Column, e);
            if (e.Key == Key.PageUp) return MoveCurrentCell(_currentCell.Row - VisibleRowCount, _currentCell.Column, e);
            return false;
        }

        private void imageKeyDown(object sender, KeyEventArgs e)
        {
            using (var ctx = CreateInvalidationContext())
            {
                if (ShiftPressed)
                {
                    if (!_shiftDragStartCell.IsCell)
                    {
                        _shiftDragStartCell = _currentCell;
                    }
                }
                else
                {
                    _shiftDragStartCell = FastGridCellAddress.Empty;
                }

                bool moved = HandleCursorMove(e);
                if (ShiftPressed && moved) SetSelectedRectangle(_shiftDragStartCell, _currentCell);

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
                pt.X *= DpiDetector.DpiXKoef;
                pt.Y *= DpiDetector.DpiYKoef;
                _mouseCursorPoint = pt;
                var cell = GetCellAddress(pt);

                if (_resizingColumn.HasValue)
                {
                    int newSize = _resizingColumnStartSize.Value + (int) Math.Round(pt.X - _resizingColumnOrigin.Value.X);
                    if (newSize < MinColumnWidth) newSize = MinColumnWidth;
                    if (newSize > GridScrollAreaWidth) newSize = GridScrollAreaWidth;
                    _columnSizes.Resize(_resizingColumn.Value, newSize);
                    if (_resizingColumn < _columnSizes.FrozenCount)
                    {
                        SetScrollbarMargin();
                    }
                    AdjustScrollbars();
                    InvalidateAll();
                }
                else
                {
                    int? column = GetResizingColumn(pt);
                    if (column != null) Cursor = Cursors.SizeWE;
                    else Cursor = Cursors.Arrow;
                }

                if (_dragStartCell.IsCell && cell.IsCell 
                    || _dragStartCell.IsRowHeader && cell.Row.HasValue
                    || _dragStartCell.IsColumnHeader && cell.Column.HasValue)
                {
                    SetSelectedRectangle(_dragStartCell, cell);
                }

                SetHoverRow(cell.IsCell ? cell.Row.Value : (int?) null);
                SetHoverRowHeader(cell.IsRowHeader ? cell.Row.Value : (int?) null);
                SetHoverColumnHeader(cell.IsColumnHeader ? cell.Column.Value : (int?) null);
                SetHoverCell(cell);

                var currentRegion = CurrentCellActiveRegions.FirstOrDefault(x => x.Rect.Contains(pt));
                if (currentRegion != CurrentHoverRegion)
                {
                    InvalidateCell(cell);
                }
            }

            HandleMouseMoveTooltip();
        }

        private void SetSelectedRectangle(FastGridCellAddress origin, FastGridCellAddress cell)
        {
            var newSelected = GetCellRange(origin, cell);
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
            OnChangeSelectedCells(true);
        }

        private void HandleMouseMoveTooltip()
        {
            if (CurrentHoverRegion != null && CurrentHoverRegion.Tooltip != null)
            {
                ShowTooltip(CurrentHoverRegion, CurrentHoverRegion.Tooltip);
                return;
            }

            if (CurrentHoverRegion == null)
            {
                var modelCell = GetCell(_mouseOverCell);
                if (modelCell != null)
                {
                    if (modelCell.ToolTipVisibility == TooltipVisibilityMode.Always || _mouseOverCellIsTrimmed)
                    {
                        string tooltip = modelCell.ToolTipText;
                        if (tooltip != null)
                        {
                            ShowTooltip(_mouseOverCell, tooltip);
                            return;
                        }
                    }
                }
            }

            HideTooltip();
        }

        private void HideTooltip()
        {
            if (_tooltip != null && _tooltip.IsOpen)
            {
                _tooltip.IsOpen = false;
            }
            _tooltipTarget = null;
            if (_tooltipTimer != null)
            {
                _tooltipTimer.IsEnabled = false;
            }
        }

        private void ShowTooltip(object tooltipTarget, string text)
        {
            if (Equals(tooltipTarget, _tooltipTarget) && _tooltipText == text) return;
            HideTooltip();

            if (_tooltip == null)
            {
                _tooltip = new ToolTip();
            }
            if (_tooltipTimer == null)
            {
                _tooltipTimer = new DispatcherTimer();
                _tooltipTimer.Interval = TimeSpan.FromSeconds(0.5);
                _tooltipTimer.Tick += _tooltipTimer_Tick;
            }

            _tooltipText = text;
            _tooltipTarget = tooltipTarget;
            _tooltip.Content = text;
            _tooltipTimer.IsEnabled = true;
        }

        private void _tooltipTimer_Tick(object sender, EventArgs e)
        {
            _tooltip.IsOpen = true;
            _tooltipTimer.IsEnabled = false;
        }

        private void OnChangeSelectedCells(bool isInvokedByUser)
        {
            if (SelectedCellsChanged != null) SelectedCellsChanged(this, new SelectionChangedEventArgs {IsInvokedByUser = isInvokedByUser});
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

        private void HandleCommand(FastGridCellAddress address, object commandParameter)
        {
            if (commandParameter == ToggleTransposedCommand)
            {
                IsTransposed = !IsTransposed;
            }
            if (commandParameter == ToggleAllowFlexibleRowsCommand)
            {
                AllowFlexibleRows = !AllowFlexibleRows;
            }
            if (Model != null)
            {
                var addressModel = RealToModel(address);
                Model.HandleCommand(this, addressModel, commandParameter);
            }
        }

        private void imageMouseLeave(object sender, MouseEventArgs e)
        {
            HideTooltip();
        }

        private void OnScrolledModelRows()
        {
            if (ScrolledModelRows != null) ScrolledModelRows(this, EventArgs.Empty);
        }

        private void OnScrolledModelColumns()
        {
            if (ScrolledModelColumns != null) ScrolledModelColumns(this, EventArgs.Empty);
        }

        private void edTextLostFocus(object sender, RoutedEventArgs e)
        {
            HideInlinEditor();
        }
    }
}
