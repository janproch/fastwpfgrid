using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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

        private Color _gridLineColor = Colors.LightGray;
        private int _cellPadding = 1;

        private FastGridCellAddress _currentCell;
        private HashSet<FastGridCellAddress> _selectedCells = new HashSet<FastGridCellAddress>();
        private FastGridCellAddress _dragStartCell;
        private int? _mouseOverRow;
        private FastGridCellAddress _inplaceEditorCell;

        private int _headerHeight;
        private int _headerWidth;
        private Dictionary<Tuple<bool, bool>, GlyphFont> _glyphFonts = new Dictionary<Tuple<bool, bool>, GlyphFont>();
        private Dictionary<Color, Brush> _solidBrushes = new Dictionary<Color, Brush>();
        private double _rowHeightReserve = 5;
        //private Color _headerBackground = Color.FromRgb(0xDD, 0xDD, 0xDD);
        private WriteableBitmap _drawBuffer;

        private bool _isInvalidated;
        private List<int> _invalidatedRows = new List<int>();
        private List<int> _invalidatedColumns = new List<int>();
        private List<Tuple<int, int>> _invalidatedCells = new List<Tuple<int, int>>();
        private List<int> _invalidatedRowHeaders = new List<int>();
        private List<int> _invalidatedColumnHeaders = new List<int>();
        private bool _isTransposed;

        public FastGridControl()
        {
            InitializeComponent();
            //gridCore.Grid = this;
            CellFontSize = 12;
        }

        private static void Exchange<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }

        public bool IsTransposed
        {
            get { return _isTransposed; }
            set
            {
                if (_isTransposed != value)
                {
                    _isTransposed = value;
                    Exchange(ref _rowCount, ref _columnCount);
                    Exchange(ref FirstVisibleColumn, ref FirstVisibleRow);
                    if (_currentCell.IsCell) _currentCell = new FastGridCellAddress(_currentCell.Column, _currentCell.Row);
                    RecountColumnWidths();
                    AdjustScrollbars();
                    RenderGrid();
                }
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
                var typeFace = new Typeface(new FontFamily(CellFontName),
                                            isItalic ? FontStyles.Italic : FontStyles.Normal,
                                            isBold ? FontWeights.Bold : FontWeights.Normal,
                                            FontStretches.Normal);

                var font = LetterGlyphTool.GetFont(typeFace, CellFontSize);
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
            int rowHeight = (int)(GetFont(false, false).TextHeight + CellPadding * 2 + 2 + RowHeightReserve);
            int columnWidth = rowHeight * 4;

            _rowSizes.DefaultSize = rowHeight;
            _columnSizes.DefaultSize = columnWidth;

            HeaderWidth = GetTextWidth("0000", false, false);
            HeaderHeight = rowHeight;

            InvalidateAll();
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
            int rowIndex = _rowSizes.GetIndexOnPosition((int)vscroll.Value);
            int columnIndex = _columnSizes.GetIndexOnPosition((int)hscroll.Value);
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
                && Math.Abs(row - FirstVisibleRow) * 2 < VisibleRowCount)
            {
                int scrollY = _rowSizes.GetScroll(FirstVisibleRow, row);// (FirstVisibleRow - row) * RowHeight;

                int oldFirstVisible = FirstVisibleRow;
                FirstVisibleRow = row;
                int visibleRows = VisibleRowCount;

                if (row > oldFirstVisible)
                {
                    for (int i = row + visibleRows; i >= oldFirstVisible + visibleRows - 1; i--)
                    {
                        InvalidateRow(i);
                    }
                }
                else
                {
                    for (int i = row; i <= oldFirstVisible; i++)
                    {
                        InvalidateRow(i);
                    }
                }
                _drawBuffer.ScrollY(scrollY, GetScrollRect());
                _drawBuffer.ScrollY(scrollY, GetRowHeadersRect());
                RenderGrid();
                return;
            }

            if (column != FirstVisibleColumn && !_isInvalidated && row == FirstVisibleRow
                && Math.Abs(column - FirstVisibleColumn) * 2 < VisibleColumnCount)
            {
                int scrollX = _columnSizes.GetScroll(FirstVisibleColumn, column); // (FirstVisibleColumn - column) * ColumnWidth;

                int oldFirstVisible = FirstVisibleColumn;
                FirstVisibleColumn = column;
                int visibleCols = VisibleColumnCount;

                if (column > oldFirstVisible)
                {
                    for (int i = column + visibleCols; i >= oldFirstVisible + visibleCols - 1; i--)
                    {
                        InvalidateColumn(i);
                    }
                }
                else
                {
                    for (int i = column; i <= oldFirstVisible; i++)
                    {
                        InvalidateColumn(i);
                    }
                }

                _drawBuffer.ScrollX(scrollX, GetScrollRect());
                _drawBuffer.ScrollX(scrollX, GetColumnHeadersRect());
                RenderGrid();
                return;
            }


            // render all
            ClearInvalidation();
            FirstVisibleRow = row;
            FirstVisibleColumn = column;
            RenderGrid();
        }


        public Color GetAlternateBackground(int row)
        {
            return _alternatingColors[row % _alternatingColors.Length];
        }

        private void hscroll_Scroll(object sender, ScrollEventArgs e)
        {
            ScrollChanged();
        }

        private void vscroll_Scroll(object sender, ScrollEventArgs e)
        {
            ScrollChanged();
        }

        public IFastGridModel Model
        {
            get { return _model; }
            set
            {
                if (_model != null) _model.DetachView(this);
                _model = value;
                if (_model != null) _model.AttachView(this);
                InvalidateAll();
            }
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
            hscroll.Maximum = _columnSizes.GetTotalSizeSum() - GridScrollAreaWidth;
            hscroll.ViewportSize = GridScrollAreaWidth;

            vscroll.Minimum = 0;
            vscroll.Maximum = _rowSizes.GetTotalSizeSum() - GridScrollAreaHeight;
            vscroll.ViewportSize = GridScrollAreaHeight;
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

        public void InvalidateAll()
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
        }

        private void RecountColumnWidths()
        {
            _columnSizes.Clear();

            if (IsTransposed) return;

            for (int col = 0; col < _columnCount; col++)
            {
                var cell = GetColumnHeader(col);
                _columnSizes.PutSizeOverride(col, GetCellContentWidth(cell));
            }

            for (int row = 0; row < Math.Min(10, _rowCount); row++)
            {
                for (int col = 0; col < _columnCount; col++)
                {
                    var cell = GetCell(row, col);
                    _columnSizes.PutSizeOverride(col, GetCellContentWidth(cell));
                }
            }

            _columnSizes.BuildIndex();
        }

        public void InvalidateRowHeader(int row)
        {
            _isInvalidated = true;
            _invalidatedRowHeaders.Add(row);
        }

        public void InvalidateColumnHeader(int column)
        {
            _isInvalidated = true;
            _invalidatedColumnHeaders.Add(column);
        }

        public void InvalidateColumn(int column)
        {
            _isInvalidated = true;
            _invalidatedColumns.Add(column);
            _invalidatedColumnHeaders.Add(column);
        }

        public void InvalidateRow(int row)
        {
            _isInvalidated = true;
            _invalidatedRows.Add(row);
            _invalidatedRowHeaders.Add(row);
        }

        public void InvalidateCell(int row, int column)
        {
            _isInvalidated = true;
            _invalidatedCells.Add(Tuple.Create(row, column));
        }

        public void InvalidateCell(FastGridCellAddress cell)
        {
            if (cell.Column == null && cell.Row == null)
            {
                // invalidate cell 00
                return;
            }
            if (cell.Column == null)
            {
                InvalidateRowHeader(cell.Row.Value);
                return;
            }
            if (cell.Row == null)
            {
                InvalidateColumnHeader(cell.Column.Value);
                return;
            }
            InvalidateCell(cell.Row.Value, cell.Column.Value);
        }

        public void NotifyAddedRows()
        {
            InvalidateAll();
        }

        public Brush GetSolidBrush(Color color)
        {
            if (!_solidBrushes.ContainsKey(color))
            {
                _solidBrushes[color] = new SolidColorBrush(color);
            }
            return _solidBrushes[color];
        }

        private void ClearInvalidation()
        {
            _invalidatedRows.Clear();
            _invalidatedColumns.Clear();
            _invalidatedCells.Clear();
            _invalidatedColumnHeaders.Clear();
            _invalidatedRowHeaders.Clear();
            _isInvalidated = false;
        }

        private bool ShouldDrawCell(int row, int column)
        {
            if (!_isInvalidated) return true;

            if (_invalidatedRows.Contains(row)) return true;
            if (_invalidatedColumns.Contains(column)) return true;
            if (_invalidatedCells.Contains(Tuple.Create(row, column))) return true;
            return false;
        }

        private bool ShouldDrawRowHeader(int row)
        {
            if (!_isInvalidated) return true;

            if (_invalidatedRows.Contains(row)) return true;
            if (_invalidatedRowHeaders.Contains(row)) return true;
            return false;
        }

        private bool ShouldDrawColumnHeader(int column)
        {
            if (!_isInvalidated) return true;

            if (_invalidatedColumns.Contains(column)) return true;
            if (_invalidatedColumnHeaders.Contains(column)) return true;
            return false;
        }

        private int VisibleRowCount
        {
            get { return _rowSizes.GetVisibleCount(FirstVisibleRow, (int)ActualHeight); }
        }

        private int VisibleColumnCount
        {
            get { return _columnSizes.GetVisibleCount(FirstVisibleColumn, (int)ActualWidth); }
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
            return new IntRect(
                new IntPoint(rect.Left + CellPadding, rect.Top + CellPadding),
                new IntSize(rect.Width - 2 * CellPadding, rect.Height - 2 * CellPadding)
                );
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

                if (!_isInvalidated)
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



                        RenderCell(cell, rect, selectedTextColor, selectedBgColor
                                                            ?? hoverRowColor
                                                            ?? cell.BackgroundColor
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
                    RenderCell(cell, rect, null, selectedBgColor ?? cell.BackgroundColor ?? HeaderBackground);
                }

                for (int col = FirstVisibleColumn; col < FirstVisibleColumn + colsToRender; col++)
                {
                    if (col < 0 || col >= _columnCount) continue;
                    var cell = GetColumnHeader(col);
                    if (!ShouldDrawColumnHeader(col)) continue;

                    Color? selectedBgColor = null;
                    if (col == _currentCell.Column) selectedBgColor = HeaderCurrentBackground;

                    var rect = GetColumnHeaderRect(col);
                    RenderCell(cell, rect, null, selectedBgColor ?? cell.BackgroundColor ?? HeaderBackground);
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
            }

            //if (cell.IsCell) ShowTextEditor(
            //    GetCellRect(cell.Row.Value, cell.Column.Value),
            //    Model.GetCell(cell.Row.Value, cell.Column.Value).GetEditText());
        }

        private void HideInlinEditor()
        {
            using (var ctx = CreateInvalidationContext())
            {
                if (_inplaceEditorCell.IsCell)
                {
                    var cell = GetCell(_inplaceEditorCell.Row.Value, _inplaceEditorCell.Column.Value);
                    cell.SetEditText(edText.Text);
                    InvalidateCell(_inplaceEditorCell);
                }
                _inplaceEditorCell = new FastGridCellAddress();
                edText.Text = "";
                edText.Visibility = Visibility.Hidden;
            }
        }

        private void ShowInlineEditor(FastGridCellAddress cell)
        {
            _inplaceEditorCell = cell;

            string text = GetCell(cell.Row.Value, cell.Column.Value).GetEditText();

            edText.Text = text;
            edText.Visibility = Visibility.Visible;
            AdjustInlineEditorPosition();

            if (edText.IsFocused)
            {
                edText.SelectAll();
            }
            else
            {
                edText.Focus();
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)edText.SelectAll);
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
                return new FastGridCellAddress(null, _rowSizes.GetIndexOnPosition((int) pt.X - HeaderWidth + _columnSizes.GetPosition(FirstVisibleColumn)));
            }
            return new FastGridCellAddress(
                _rowSizes.GetIndexOnPosition((int) pt.Y - HeaderHeight + _rowSizes.GetPosition(FirstVisibleRow)),
                _columnSizes.GetIndexOnPosition((int)pt.X - HeaderWidth + _columnSizes.GetPosition(FirstVisibleColumn)));
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

                SetHoverRow(cell.IsCell ? cell.Row.Value : (int?)null);
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            using (var ctx = CreateInvalidationContext())
            {
                SetHoverRow(null);
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

        private int GetCellContentWidth(IFastGridCell cell)
        {
            int count = cell.BlockCount;
            int rightCount = cell.RightAlignBlockCount;
            int leftCount = count - rightCount;

            int witdh = 0;
            for (int i = 0; i < leftCount; i++)
            {
                var block = cell.GetBlock(i);
                string text = block.TextData;
                bool isBold = block.IsBold;
                bool isItalic = block.IsItalic;
                var font = GetFont(isBold, isItalic);
                witdh += font.GetTextWidth(text);
            }
            return witdh;
        }

        private void RenderCell(IFastGridCell cell, IntRect rect, Color? selectedTextColor, Color bgColor)
        {
            var rectContent = GetContentRect(rect);
            _drawBuffer.DrawRectangle(rect, GridLineColor);
            _drawBuffer.FillRectangle(rectContent, bgColor);

            int count = cell.BlockCount;
            int rightCount = cell.RightAlignBlockCount;
            int leftCount = count - rightCount;
            int leftPos = rectContent.Left;
            int rightPos = rectContent.Right;

            for (int i = 0; i < leftCount && leftPos < rightPos; i++)
            {
                var block = cell.GetBlock(i);
                string text = block.TextData;
                bool isBold = block.IsBold;
                bool isItalic = block.IsItalic;
                var color = block.FontColor;
                var font = GetFont(isBold, isItalic);
                //var glyphTypeface = GetGlyphTypeface(isBold, isItalic);
                int textHeight = font.TextHeight;
                var origin = new IntPoint(leftPos, rectContent.Top + (int)Math.Round(rectContent.Height / 2.0 - textHeight / 2.0));
                //int maxWidth = rect.Right - origin.X;
                int width = _drawBuffer.DrawString(origin.X, origin.Y, rectContent, selectedTextColor ?? color ?? CellFontColor, PreciseCharacterGlyphs ? bgColor : (Color?)null, font, text);
                leftPos += width;
            }
        }


        private void imageGridResized(object sender, SizeChangedEventArgs e)
        {
            int width = (int)imageGrid.ActualWidth - 2;
            int height = (int)imageGrid.ActualHeight - 2;
            if (width > 0 && height > 0)
            {
                _drawBuffer = BitmapFactory.New(width, height);
            }
            else
            {
                _drawBuffer = null;
            }
            image.Source = _drawBuffer;
            image.Width = width;
            image.Height = height;

            AdjustScrollbars();
            RenderGrid();
        }

        private void MoveCurrentCell(int? row, int? col)
        {
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
            int vrows = VisibleRowCount;
            int vcols = VisibleColumnCount;

            if (cell.Row < FirstVisibleRow)
            {
                ScrollContent(cell.Row.Value, FirstVisibleColumn);
            }
            if (cell.Row > FirstVisibleRow + vrows - 2)
            {
                ScrollContent(cell.Row.Value - vrows + 2, FirstVisibleColumn);
            }

            if (cell.Column < FirstVisibleColumn)
            {
                ScrollContent(FirstVisibleRow, cell.Column.Value);
            }
            if (cell.Column > FirstVisibleColumn + vcols - 2)
            {
                ScrollContent(FirstVisibleRow, cell.Column.Value - vcols + 2);
            }
            AdjustScrollBarPositions();
        }

        private static bool ControlPressed
        {
            get { return (Keyboard.Modifiers & ModifierKeys.Control) != 0; }
        }

        private void imageKeyDown(object sender, KeyEventArgs e)
        {
            using (var ctx = CreateInvalidationContext())
            {
                if (e.Key == Key.Up && _currentCell.Row > 0) MoveCurrentCell(_currentCell.Row - 1, _currentCell.Column);
                else if (e.Key == Key.Down && _currentCell.Row < _rowCount - 1) MoveCurrentCell(_currentCell.Row + 1, _currentCell.Column);
                else if (e.Key == Key.Left && _currentCell.Column > 0) MoveCurrentCell(_currentCell.Row, _currentCell.Column - 1);
                else if (e.Key == Key.Right && _currentCell.Column < _columnCount - 1) MoveCurrentCell(_currentCell.Row, _currentCell.Column + 1);

                else if (e.Key == Key.Home && ControlPressed) MoveCurrentCell(0, 0);
                else if (e.Key == Key.End && ControlPressed) MoveCurrentCell(_rowCount - 1, _columnCount - 1);
                else if (e.Key == Key.PageDown && ControlPressed) MoveCurrentCell(_rowCount - 1, _currentCell.Column);
                else if (e.Key == Key.PageUp && ControlPressed) MoveCurrentCell(0, _currentCell.Column);
                else if (e.Key == Key.Home) MoveCurrentCell(_currentCell.Row, 0);
                else if (e.Key == Key.End) MoveCurrentCell(_currentCell.Row, _columnCount - 1);
                else if (e.Key == Key.PageDown) MoveCurrentCell(_currentCell.Row + VisibleRowCount, _currentCell.Column);
                else if (e.Key == Key.PageUp) MoveCurrentCell(_currentCell.Row - VisibleRowCount, _currentCell.Column);
            }
        }

        private void imageMouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(image);
        }

        private void RenderChanged()
        {
            RenderGrid();
        }
    }
}
