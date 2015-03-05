using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FastWpfGrid
{
    /// <summary>
    /// Interaction logic for FastGridControl.xaml
    /// </summary>
    public partial class FastGridControl : UserControl, IFastGridView
    {
        //private double _lastvscroll;
        private IFastGridModel _model;

        public int FirstVisibleColumn;
        public int FirstVisibleRow;
        private int _rowCount;
        private int _columnCount;
        //private double[] _columnWidths = new double[0];

        //private int _rowHeight;
        //private int _columnWidth;

        //private Dictionary<int, int> _rowHeightOverrides = new Dictionary<int, int>();
        //private Dictionary<int, int> _columnWidthOverrides = new Dictionary<int, int>();

        private SeriesSizes _rowSizes = new SeriesSizes();
        private SeriesSizes _columnSizes = new SeriesSizes();


        private FastGridCellAddress _currentCell;
        private HashSet<FastGridCellAddress> _selectedCells = new HashSet<FastGridCellAddress>();
        private FastGridCellAddress _dragStartCell;
        private int? _mouseOverRow;
        private int? _mouseOverRowHeader;
        private int? _mouseOverColumnHeader;
        private FastGridCellAddress _inplaceEditorCell;

        private int _headerHeight;
        private int _headerWidth;
        private Dictionary<Tuple<bool, bool>, GlyphFont> _glyphFonts = new Dictionary<Tuple<bool, bool>, GlyphFont>();
        private Dictionary<Color, Brush> _solidBrushes = new Dictionary<Color, Brush>();
        private double _rowHeightReserve = 5;
        //private Color _headerBackground = Color.FromRgb(0xDD, 0xDD, 0xDD);
        private WriteableBitmap _drawBuffer;

        private bool _isTransposed;

        private int? _resizingColumn;
        private Point? _resizingColumnOrigin;
        private int? _resizingColumnStartSize;

        private static Dictionary<string, WriteableBitmap> _imageCache = new Dictionary<string, WriteableBitmap>();

        public FastGridControl()
        {
            InitializeComponent();
            //gridCore.Grid = this;
            CellFontSize = 11;
        }

        private static void Exchange<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }

        private void OnIsTransposedPropertyChanged()
        {
            if (_isTransposed != IsTransposed)
            {
                _isTransposed = IsTransposed;
                Exchange(ref _rowCount, ref _columnCount);
                Exchange(ref FirstVisibleColumn, ref FirstVisibleRow);
                if (_currentCell.IsCell) _currentCell = new FastGridCellAddress(_currentCell.Column, _currentCell.Row);
                _columnSizes.Count = _columnCount;
                _rowSizes.Count = _rowCount;
                RecountColumnWidths();
                RecalculateDefaultCellSize();
                AdjustScrollbars();
                AdjustScrollBarPositions();
                AdjustInlineEditorPosition();
            }
        }

        public int HeaderHeight
        {
            get { return _headerHeight; }
            set
            {
                _headerHeight = value;
                vscroll.Margin = new Thickness
                    {
                        Top = HeaderHeight,
                    };
            }
        }

        public int HeaderWidth
        {
            get { return _headerWidth; }
            set
            {
                _headerWidth = value;
                hscroll.Margin = new Thickness
                    {
                        Left = HeaderWidth,
                    };
            }
        }

        public GlyphFont GetFont(bool isBold, bool isItalic)
        {
            var key = Tuple.Create(isBold, isItalic);
            if (!_glyphFonts.ContainsKey(key))
            {
                var font = LetterGlyphTool.GetFont(new PortableFontDesc(CellFontName, CellFontSize, isBold, isItalic, UseClearType));
                _glyphFonts[key] = font;
            }
            return _glyphFonts[key];
        }

        public void ClearCaches()
        {
            _glyphFonts.Clear();
        }

        public int GetTextWidth(string text, bool isBold, bool isItalic)
        {
            return GetFont(isBold, isItalic).GetTextWidth(text);
            //double size = CellFontSize;
            //int totalWidth = 0;
            //var glyphTypeface = GetFont(isBold, isItalic);

            //for (int n = 0; n < text.Length; n++)
            //{
            //    ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];
            //    double width = Math.Round(glyphTypeface.AdvanceWidths[glyphIndex] * size);
            //    totalWidth += (int)width;
            //}
            //return totalWidth;
        }

        private void RecalculateDefaultCellSize()
        {
            ClearCaches();
            int rowHeight = (int) (GetFont(false, false).TextHeight + CellPaddingVertical*2 + 2 + RowHeightReserve);
            int columnWidth = rowHeight*4;

            _rowSizes.DefaultSize = rowHeight;
            _columnSizes.DefaultSize = columnWidth;

            HeaderWidth = GetTextWidth("0000000", false, false);
            HeaderHeight = rowHeight;

            if (IsTransposed) CountTransposedHeaderWidth();

            InvalidateAll();
        }

        private void CountTransposedHeaderWidth()
        {
            int maxw = 0;
            for (int col = 0; col < _columnCount; col++)
            {
                var cell = Model.GetColumnHeader(col);
                int width = GetCellContentWidth(cell) + 2*CellPaddingHorizontal;
                if (width > maxw) maxw = width;
            }
            HeaderWidth = maxw;
        }

        //public int RowHeight
        //{
        //    get { return _rowHeight; }
        //}

        //public int ColumnWidth
        //{
        //    get { return _columnWidth; }
        //}

        private void ScrollChanged()
        {
            int rowIndex = _rowSizes.GetIndexOnPosition((int) vscroll.Value);
            int columnIndex = _columnSizes.GetIndexOnPosition((int) hscroll.Value);
            //FirstVisibleRow = rowIndex;
            //FirstVisibleColumn = columnIndex;
            //RenderGrid();
            ScrollContent(rowIndex, columnIndex);
            AdjustInlineEditorPosition();
        }

        private IntRect GetScrollRect()
        {
            return new IntRect(new IntPoint(HeaderWidth, HeaderHeight), new IntSize(GridScrollAreaWidth, GridScrollAreaHeight));
        }

        private void ScrollContent(int row, int column)
        {
            if (row == FirstVisibleRow && column == FirstVisibleColumn)
            {
                return;
            }

            if (row != FirstVisibleRow && !_isInvalidated && column == FirstVisibleColumn
                && Math.Abs(row - FirstVisibleRow)*2 < VisibleRowCount)
            {
                using (var ctx = CreateInvalidationContext())
                {
                    int scrollY = _rowSizes.GetScroll(FirstVisibleRow, row);
                    _rowSizes.InvalidateAfterScroll(FirstVisibleRow, row, InvalidateRow, GridScrollAreaHeight);
                    FirstVisibleRow = row;

                    _drawBuffer.ScrollY(scrollY, GetScrollRect());
                    _drawBuffer.ScrollY(scrollY, GetRowHeadersRect());
                }
                return;
            }

            if (column != FirstVisibleColumn && !_isInvalidated && row == FirstVisibleRow
                && Math.Abs(column - FirstVisibleColumn)*2 < VisibleColumnCount)
            {
                using (var ctx = CreateInvalidationContext())
                {
                    int scrollX = _columnSizes.GetScroll(FirstVisibleColumn, column);
                    _columnSizes.InvalidateAfterScroll(FirstVisibleColumn, column, InvalidateColumn, GridScrollAreaWidth);
                    FirstVisibleColumn = column;

                    _drawBuffer.ScrollX(scrollX, GetScrollRect());
                    _drawBuffer.ScrollX(scrollX, GetColumnHeadersRect());
                }
                return;
            }


            // render all
            using (var ctx = CreateInvalidationContext())
            {
                FirstVisibleRow = row;
                FirstVisibleColumn = column;
                InvalidateAll();
            }
        }


        public Color GetAlternateBackground(int row)
        {
            return _alternatingColors[row%_alternatingColors.Length];
        }

        private void hscroll_Scroll(object sender, ScrollEventArgs e)
        {
            ScrollChanged();
        }

        private void vscroll_Scroll(object sender, ScrollEventArgs e)
        {
            ScrollChanged();
        }

        private void OnModelPropertyChanged()
        {
            if (_model != null) _model.DetachView(this);
            _model = Model;
            if (_model != null) _model.AttachView(this);
            NotifyRefresh();
        }


        public int GridScrollAreaWidth
        {
            get
            {
                if (_drawBuffer == null) return 1;
                return _drawBuffer.PixelWidth - HeaderWidth;
            }
        }

        public int GridScrollAreaHeight
        {
            get
            {
                if (_drawBuffer == null) return 1;
                return _drawBuffer.PixelHeight - HeaderHeight;
            }
        }

        private void AdjustScrollbars()
        {
            hscroll.Minimum = 0;
            hscroll.Maximum = _columnSizes.GetTotalSizeSum() - GridScrollAreaWidth + _columnSizes.DefaultSize;
            hscroll.ViewportSize = GridScrollAreaWidth;
            hscroll.SmallChange = GridScrollAreaWidth/10.0;
            hscroll.LargeChange = GridScrollAreaWidth/2.0;

            vscroll.Minimum = 0;
            vscroll.Maximum = _rowSizes.GetTotalSizeSum() - GridScrollAreaHeight + _rowSizes.DefaultSize;
            vscroll.ViewportSize = GridScrollAreaHeight;
            vscroll.SmallChange = _rowSizes.DefaultSize;
            vscroll.LargeChange = GridScrollAreaHeight / 2.0;
        }

        private void AdjustScrollBarPositions()
        {
            hscroll.Value = _columnSizes.GetPosition(FirstVisibleColumn); //FirstVisibleColumn* ColumnWidth;
            vscroll.Value = _rowSizes.GetPosition(FirstVisibleRow);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            AdjustScrollbars();
        }

        public void NotifyRefresh()
        {
            _rowCount = 0;
            _columnCount = 0;
            if (_model != null)
            {
                _rowCount = IsTransposed ? _model.ColumnCount : _model.RowCount;
                _columnCount = IsTransposed ? _model.RowCount : _model.ColumnCount;
            }
            _rowSizes.Count = _rowCount;
            _columnSizes.Count = _columnCount;

            RecountColumnWidths();
            AdjustScrollbars();
            InvalidateAll();
        }

        private void RecountColumnWidths()
        {
            _columnSizes.Clear();

            if (IsTransposed) return;

            for (int col = 0; col < _columnCount; col++)
            {
                var cell = GetColumnHeader(col);
                _columnSizes.PutSizeOverride(col, GetCellContentWidth(cell) + 2*CellPaddingHorizontal);
            }

            for (int row = 0; row < Math.Min(10, _rowCount); row++)
            {
                for (int col = 0; col < _columnCount; col++)
                {
                    var cell = GetCell(row, col);
                    _columnSizes.PutSizeOverride(col, GetCellContentWidth(cell) + 2*CellPaddingHorizontal);
                }
            }

            _columnSizes.BuildIndex();
        }

        public void NotifyAddedRows()
        {
            NotifyRefresh();
        }

        public Brush GetSolidBrush(Color color)
        {
            if (!_solidBrushes.ContainsKey(color))
            {
                _solidBrushes[color] = new SolidColorBrush(color);
            }
            return _solidBrushes[color];
        }

        private bool ShouldDrawCell(int row, int column)
        {
            if (!_isInvalidated || _isInvalidatedAll) return true;

            if (_invalidatedRows.Contains(row)) return true;
            if (_invalidatedColumns.Contains(column)) return true;
            if (_invalidatedCells.Contains(Tuple.Create(row, column))) return true;
            return false;
        }

        private bool ShouldDrawRowHeader(int row)
        {
            if (!_isInvalidated || _isInvalidatedAll) return true;

            if (_invalidatedRows.Contains(row)) return true;
            if (_invalidatedRowHeaders.Contains(row)) return true;
            return false;
        }

        private bool ShouldDrawColumnHeader(int column)
        {
            if (!_isInvalidated || _isInvalidatedAll) return true;

            if (_invalidatedColumns.Contains(column)) return true;
            if (_invalidatedColumnHeaders.Contains(column)) return true;
            return false;
        }

        public int VisibleRowCount
        {
            get { return _rowSizes.GetVisibleCount(FirstVisibleRow, GridScrollAreaHeight); }
        }

        public int VisibleColumnCount
        {
            get { return _columnSizes.GetVisibleCount(FirstVisibleColumn, GridScrollAreaWidth); }
        }

        private int GetRowTop(int row)
        {
            return _rowSizes.GetSizeSum(FirstVisibleRow, row) + HeaderHeight;
            //return (row - FirstVisibleRow) * RowHeight + HeaderHeight;
        }

        private int GetColumnLeft(int column)
        {
            return _columnSizes.GetSizeSum(FirstVisibleColumn, column) + HeaderWidth;
            //return (column - FirstVisibleColumn) * ColumnWidth + HeaderWidth;
        }

        private IntRect GetCellRect(int row, int column)
        {
            return new IntRect(new IntPoint(GetColumnLeft(column), GetRowTop(row)), new IntSize(_columnSizes.GetSize(column) + 1, _rowSizes.GetSize(row) + 1));
        }

        private IntRect GetContentRect(IntRect rect)
        {
            return rect.GrowSymmetrical(-CellPaddingHorizontal, -CellPaddingVertical);
        }

        private IntRect GetRowHeaderRect(int row)
        {
            return new IntRect(new IntPoint(0, GetRowTop(row)), new IntSize(HeaderWidth + 1, _rowSizes.GetSize(row) + 1));
        }

        private IntRect GetColumnHeaderRect(int column)
        {
            return new IntRect(new IntPoint(GetColumnLeft(column), 0), new IntSize(_columnSizes.GetSize(column) + 1, HeaderHeight + 1));
        }

        private IntRect GetColumnHeadersRect()
        {
            return new IntRect(new IntPoint(HeaderWidth, 0), new IntSize(GridScrollAreaWidth, HeaderHeight + 1));
        }

        private IntRect GetRowHeadersRect()
        {
            return new IntRect(new IntPoint(0, HeaderHeight), new IntSize(HeaderWidth + 1, GridScrollAreaHeight));
        }

        private void RenderGrid()
        {
            if (_drawBuffer == null)
            {
                ClearInvalidation();
                return;
            }
            using (_drawBuffer.GetBitmapContext())
            {
                int colsToRender = VisibleColumnCount;
                int rowsToRender = VisibleRowCount;

                if (!_isInvalidated || _isInvalidatedAll)
                {
                    _drawBuffer.Clear(Colors.White);
                }

                for (int row = FirstVisibleRow; row < FirstVisibleRow + rowsToRender; row++)
                {
                    for (int col = FirstVisibleColumn; col < FirstVisibleColumn + colsToRender; col++)
                    {
                        if (row < 0 || col < 0 || row >= _rowCount || col >= _columnCount) continue;
                        if (!ShouldDrawCell(row, col)) continue;
                        var rect = GetCellRect(row, col);
                        var cell = GetCell(row, col);
                        Color? selectedBgColor = null;
                        Color? selectedTextColor = null;
                        Color? hoverRowColor = null;
                        if (_currentCell.TestCell(row, col) || _selectedCells.Contains(new FastGridCellAddress(row, col)))
                        {
                            selectedBgColor = SelectedColor;
                            selectedTextColor = SelectedTextColor;
                        }
                        if (row == _mouseOverRow)
                        {
                            hoverRowColor = MouseOverRowColor;
                        }


                        Color? cellBackground = null;
                        if (cell != null) cellBackground = cell.BackgroundColor;

                        RenderCell(cell, rect, selectedTextColor, selectedBgColor
                                                                  ?? hoverRowColor
                                                                  ?? cellBackground
                                                                  ?? GetAlternateBackground(row));
                    }
                }

                for (int row = FirstVisibleRow; row < FirstVisibleRow + rowsToRender; row++)
                {
                    if (row < 0 || row >= _rowCount) continue;
                    var cell = GetRowHeader(row);
                    if (!ShouldDrawRowHeader(row)) continue;

                    Color? selectedBgColor = null;
                    if (row == _currentCell.Row) selectedBgColor = HeaderCurrentBackground;

                    var rect = GetRowHeaderRect(row);
                    Color? cellBackground = null;
                    if (cell != null) cellBackground = cell.BackgroundColor;

                    Color? hoverColor = null;
                    if (row == _mouseOverRowHeader) hoverColor = MouseOverRowColor;

                    RenderCell(cell, rect, null, hoverColor ?? selectedBgColor ?? cellBackground ?? HeaderBackground);
                }

                for (int col = FirstVisibleColumn; col < FirstVisibleColumn + colsToRender; col++)
                {
                    if (col < 0 || col >= _columnCount) continue;
                    var cell = GetColumnHeader(col);
                    if (!ShouldDrawColumnHeader(col)) continue;

                    Color? selectedBgColor = null;
                    if (col == _currentCell.Column) selectedBgColor = HeaderCurrentBackground;

                    var rect = GetColumnHeaderRect(col);
                    Color? cellBackground = null;
                    if (cell != null) cellBackground = cell.BackgroundColor;

                    Color? hoverColor = null;
                    if (col == _mouseOverColumnHeader) hoverColor = MouseOverRowColor;

                    RenderCell(cell, rect, null, hoverColor ?? selectedBgColor ?? cellBackground ?? HeaderBackground);
                }
            }
            ClearInvalidation();
        }

        private IFastGridCell GetColumnHeader(int col)
        {
            if (Model == null) return null;
            if (IsTransposed) return Model.GetRowHeader(col);
            return Model.GetColumnHeader(col);
        }

        private IFastGridCell GetRowHeader(int row)
        {
            if (Model == null) return null;
            if (IsTransposed) return Model.GetColumnHeader(row);
            return Model.GetRowHeader(row);
        }

        private IFastGridCell GetCell(int row, int col)
        {
            if (Model == null) return null;
            if (IsTransposed) return Model.GetCell(col, row);
            return Model.GetCell(row, col);
        }

        private void RenderCell00()
        {
            //dc.DrawRectangle(Brushes.White, new Pen(), new Rect(0, 0, Grid.HeaderWidth, Grid.HeaderHeight));
        }

        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            _dragStartCell = new FastGridCellAddress();
            if (_resizingColumn.HasValue)
            {
                _resizingColumn = null;
                _resizingColumnOrigin = null;
                _resizingColumnStartSize = null;
                ReleaseMouseCapture();
            }
        }

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
                }

                int? column = GetResizingColumn(pt);
                if (column != null)
                {
                    Cursor = Cursors.SizeWE;
                    _resizingColumn = column;
                    _resizingColumnOrigin = pt;
                    _resizingColumnStartSize = _columnSizes.GetSize(_resizingColumn.Value);
                    CaptureMouse();
                }
            }

            //if (cell.IsCell) ShowTextEditor(
            //    GetCellRect(cell.Row.Value, cell.Column.Value),
            //    Model.GetCell(cell.Row.Value, cell.Column.Value).GetEditText());
        }

        private void HideInlinEditor(bool saveCellValue = true)
        {
            using (var ctx = CreateInvalidationContext())
            {
                if (saveCellValue && _inplaceEditorCell.IsCell)
                {
                    var cell = GetCell(_inplaceEditorCell.Row.Value, _inplaceEditorCell.Column.Value);
                    cell.SetEditText(edText.Text);
                    InvalidateCell(_inplaceEditorCell);
                }
                _inplaceEditorCell = new FastGridCellAddress();
                edText.Text = "";
                edText.Visibility = Visibility.Hidden;
            }
            Keyboard.Focus(image);
        }

        private void ShowInlineEditor(FastGridCellAddress cell, string textValueOverride = null)
        {
            string text = GetCell(cell.Row.Value, cell.Column.Value).GetEditText();
            if (text == null) return;

            _inplaceEditorCell = cell;

            edText.Text = textValueOverride ?? text;
            edText.Visibility = Visibility.Visible;
            AdjustInlineEditorPosition();

            if (edText.IsFocused)
            {
                if (textValueOverride == null)
                {
                    edText.SelectAll();
                }
            }
            else
            {
                edText.Focus();
                if (textValueOverride == null)
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action) edText.SelectAll);
                }
            }

            if (textValueOverride != null)
            {
                edText.SelectionStart = textValueOverride.Length;
            }
        }

        private void AdjustInlineEditorPosition()
        {
            if (_inplaceEditorCell.IsCell)
            {
                edText.Visibility = _inplaceEditorCell.Row.Value - FirstVisibleRow >= 0 ? Visibility.Visible : Visibility.Hidden;
                var rect = GetCellRect(_inplaceEditorCell.Row.Value, _inplaceEditorCell.Column.Value);
                edText.Margin = new Thickness
                    {
                        Left = rect.Left,
                        Top = rect.Top,
                        Right = imageGrid.ActualWidth - rect.Right,
                        Bottom = imageGrid.ActualHeight - rect.Bottom,
                    };
            }
        }

        public int? GetResizingColumn(Point pt)
        {
            if (pt.Y > HeaderHeight) return null;
            int index = _columnSizes.GetIndexOnPosition((int) pt.X - HeaderWidth + _columnSizes.GetPosition(FirstVisibleColumn));
            int begin = _columnSizes.GetPosition(index) + HeaderWidth;
            int end = begin + _columnSizes.GetSize(index);
            if (pt.X >= begin - ColumnResizeTheresold && pt.X <= begin + ColumnResizeTheresold) return index - 1;
            if (pt.X >= end - ColumnResizeTheresold && pt.X <= end + ColumnResizeTheresold) return index;
            return null;
        }

        public FastGridCellAddress GetCellAddress(Point pt)
        {
            if (pt.X <= HeaderWidth && pt.Y < HeaderHeight)
            {
                return FastGridCellAddress.Empty;
            }
            if (pt.X >= GridScrollAreaWidth + HeaderWidth)
            {
                return FastGridCellAddress.Empty;
            }
            if (pt.Y >= GridScrollAreaHeight + HeaderHeight)
            {
                return FastGridCellAddress.Empty;
            }
            if (pt.X < HeaderWidth)
            {
                return new FastGridCellAddress(_rowSizes.GetIndexOnPosition((int) pt.Y - HeaderHeight + _rowSizes.GetPosition(FirstVisibleRow)), null);
            }
            if (pt.Y < HeaderHeight)
            {
                return new FastGridCellAddress(null, _columnSizes.GetIndexOnPosition((int) pt.X - HeaderWidth + _columnSizes.GetPosition(FirstVisibleColumn)));
            }
            return new FastGridCellAddress(
                _rowSizes.GetIndexOnPosition((int) pt.Y - HeaderHeight + _rowSizes.GetPosition(FirstVisibleRow)),
                _columnSizes.GetIndexOnPosition((int) pt.X - HeaderWidth + _columnSizes.GetPosition(FirstVisibleColumn)));
        }

        private void InvalidateCurrentCell()
        {
            if (_currentCell.IsCell) InvalidateCell(_currentCell);
            if (_currentCell.Column.HasValue) InvalidateColumnHeader(_currentCell.Column.Value);
            if (_currentCell.Row.HasValue) InvalidateRowHeader(_currentCell.Row.Value);
        }

        private void SetCurrentCell(FastGridCellAddress cell)
        {
            using (var ctx = CreateInvalidationContext())
            {
                InvalidateCurrentCell();
                _currentCell = cell;
                InvalidateCurrentCell();
            }
        }

        private HashSet<FastGridCellAddress> GetCellRange(FastGridCellAddress a, FastGridCellAddress b)
        {
            var res = new HashSet<FastGridCellAddress>();
            int minrow = Math.Min(a.Row.Value, b.Row.Value);
            int maxrow = Math.Max(a.Row.Value, b.Row.Value);
            int mincol = Math.Min(a.Column.Value, b.Column.Value);
            int maxcol = Math.Max(a.Column.Value, b.Column.Value);

            for (int row = minrow; row <= maxrow; row++)
            {
                for (int col = mincol; col <= maxcol; col++)
                {
                    res.Add(new FastGridCellAddress(row, col));
                }
            }
            return res;
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
                    int newSize = _resizingColumnStartSize.Value + (int) Math.Round(pt.X - _resizingColumnOrigin.Value.X);
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
                }

                SetHoverRow(cell.IsCell ? cell.Row.Value : (int?) null);
                SetHoverRowHeader(cell.IsRowHeader ? cell.Row.Value : (int?) null);
                SetHoverColumnHeader(cell.IsColumnHeader ? cell.Column.Value : (int?) null);
            }
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

        private void SetHoverRow(int? row)
        {
            using (var ctx = CreateInvalidationContext())
            {
                if (row == _mouseOverRow) return;
                if (_mouseOverRow.HasValue) InvalidateRow(_mouseOverRow.Value);
                _mouseOverRow = row;
                if (_mouseOverRow.HasValue) InvalidateRow(_mouseOverRow.Value);
            }
        }

        private void SetHoverRowHeader(int? row)
        {
            using (var ctx = CreateInvalidationContext())
            {
                if (row == _mouseOverRowHeader) return;
                if (_mouseOverRowHeader.HasValue) InvalidateRowHeader(_mouseOverRowHeader.Value);
                _mouseOverRowHeader = row;
                if (_mouseOverRowHeader.HasValue) InvalidateRow(_mouseOverRowHeader.Value);
            }
        }

        private void SetHoverColumnHeader(int? column)
        {
            using (var ctx = CreateInvalidationContext())
            {
                if (column == _mouseOverColumnHeader) return;
                if (_mouseOverColumnHeader.HasValue) InvalidateColumnHeader(_mouseOverColumnHeader.Value);
                _mouseOverColumnHeader = column;
                if (_mouseOverColumnHeader.HasValue) InvalidateColumn(_mouseOverColumnHeader.Value);
            }
        }

        private int GetCellContentWidth(IFastGridCell cell)
        {
            if (cell == null) return 0;
            int count = cell.BlockCount;
            int rightCount = cell.RightAlignBlockCount;
            int leftCount = count - rightCount;

            int witdh = 0;
            for (int i = 0; i < leftCount; i++)
            {
                var block = cell.GetBlock(i);
                if (block == null) continue;
                if (i > 0) witdh += BlockPadding;

                switch (block.BlockType)
                {
                    case FastGridBlockType.Text:
                        string text = block.TextData;
                        bool isBold = block.IsBold;
                        bool isItalic = block.IsItalic;
                        var font = GetFont(isBold, isItalic);
                        witdh += font.GetTextWidth(text);
                        break;
                    case FastGridBlockType.Image:
                        witdh += block.ImageWidth;
                        break;
                }

            }
            return witdh;
        }

        private void RenderCell(IFastGridCell cell, IntRect rect, Color? selectedTextColor, Color bgColor)
        {
            if (cell == null) return;
            var rectContent = GetContentRect(rect);
            _drawBuffer.DrawRectangle(rect, GridLineColor);
            _drawBuffer.FillRectangle(rect.GrowSymmetrical(-1, -1), bgColor);

            int count = cell.BlockCount;
            int rightCount = cell.RightAlignBlockCount;
            int leftCount = count - rightCount;
            int leftPos = rectContent.Left;
            int rightPos = rectContent.Right;

            for (int i = 0; i < leftCount && leftPos < rightPos; i++)
            {
                var block = cell.GetBlock(i);
                if (block == null) continue;
                if (i > 0) leftPos += BlockPadding;

                switch (block.BlockType)
                {
                    case FastGridBlockType.Text:
                        string text = block.TextData;
                        bool isBold = block.IsBold;
                        bool isItalic = block.IsItalic;
                        var color = block.FontColor;
                        var font = GetFont(isBold, isItalic);
                        int textHeight = font.TextHeight;
                        var textOrigin = new IntPoint(leftPos, rectContent.Top + (int) Math.Round(rectContent.Height/2.0 - textHeight/2.0));
                        int textWidth = _drawBuffer.DrawString(textOrigin.X, textOrigin.Y, rectContent, selectedTextColor ?? color ?? CellFontColor, UseClearType ? bgColor : (Color?) null,
                                                               font,
                                                               text);
                        leftPos += textWidth;
                        break;
                    case FastGridBlockType.Image:
                        var imgOrigin = new IntPoint(leftPos, rectContent.Top + (int) Math.Round(rectContent.Height/2.0 - block.ImageHeight/2.0));
                        var wbmp = GetImage(block.ImageSource);
                        _drawBuffer.Blit(new Rect(imgOrigin.X, imgOrigin.Y, block.ImageWidth, block.ImageHeight), wbmp, new Rect(0, 0, block.ImageWidth, block.ImageHeight),
                                         WriteableBitmapExtensions.BlendMode.Alpha);
                        leftPos += block.ImageWidth;
                        break;
                }

            }
        }

        private static WriteableBitmap GetImage(string source)
        {
            lock (_imageCache)
            {
                if (_imageCache.ContainsKey(source)) return _imageCache[source];
            }

            string packUri = "pack://application:,,,/" + Assembly.GetEntryAssembly().GetName().Name + ";component/" + source.TrimStart('/');
            BitmapImage bmImage = new BitmapImage();
            bmImage.BeginInit();
            bmImage.UriSource = new Uri(packUri, UriKind.Absolute);
            bmImage.EndInit();
            var wbmp = new WriteableBitmap(bmImage);

            if (wbmp.Format != PixelFormats.Bgra32)
                wbmp = new WriteableBitmap(new FormatConvertedBitmap(wbmp, PixelFormats.Bgra32, null, 0));

            lock (_imageCache)
            {
                _imageCache[source] = wbmp;
            }
            return wbmp;
        }

        private void imageGridResized(object sender, SizeChangedEventArgs e)
        {
            int width = (int) imageGrid.ActualWidth - 2;
            int height = (int) imageGrid.ActualHeight - 2;
            if (width > 0 && height > 0)
            {
                _drawBuffer = BitmapFactory.New(width, height);
            }
            else
            {
                _drawBuffer = null;
            }
            image.Source = _drawBuffer;
            image.Width = Math.Max(0, width);
            image.Height = Math.Max(0, height);

            AdjustScrollbars();
            InvalidateAll();
        }

        private void MoveCurrentCell(int? row, int? col, KeyEventArgs e)
        {
            e.Handled = true;
            _selectedCells.ToList().ForEach(InvalidateCell);
            _selectedCells.Clear();

            InvalidateCurrentCell();

            if (row < 0) row = 0;
            if (row >= _rowCount) row = _rowCount - 1;
            if (col < 0) col = 0;
            if (col >= _columnCount) col = _columnCount - 1;

            _currentCell = new FastGridCellAddress(row, col);
            InvalidateCurrentCell();
            ScrollCurrentCellIntoView();
        }

        public void ScrollCurrentCellIntoView()
        {
            ScrollIntoView(_currentCell);
        }

        public void ScrollIntoView(FastGridCellAddress cell)
        {
            if (cell.Row.HasValue)
            {
                int newRow = _rowSizes.ScrollInView(FirstVisibleRow, cell.Row.Value, GridScrollAreaHeight);
                ScrollContent(newRow, FirstVisibleColumn);
            }

            if (cell.Column.HasValue)
            {
                int newColumn = _columnSizes.ScrollInView(FirstVisibleColumn, cell.Column.Value, GridScrollAreaWidth);
                ScrollContent(FirstVisibleRow, newColumn);
            }

            AdjustScrollBarPositions();
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

        private void RenderChanged()
        {
            InvalidateAll();
        }

        private void OnUseClearTypePropertyChanged()
        {
            ClearCaches();
            RecalculateDefaultCellSize();
            RenderChanged();
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
            if (e.Delta < 0) vscroll.Value = vscroll.Value + vscroll.LargeChange/2;
            if (e.Delta > 0) vscroll.Value = vscroll.Value - vscroll.LargeChange/2;
            ScrollChanged();
        }
    }
}
