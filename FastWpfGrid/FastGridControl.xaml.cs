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
        private int _rowHeight;
        private int _columnWidth;
        private Color _gridLineColor = Colors.LightGray;
        private int _cellPadding = 1;

        private FastGridCellAddress _currentCell;
        private HashSet<FastGridCellAddress> _selectedCells = new HashSet<FastGridCellAddress>();
        private FastGridCellAddress _dragStartCell;
        private int? _mouseOverRow;
        private FastGridCellAddress _inplaceEditorCell;

        private Color[] _alternatingColors = new Color[]
            {
                Colors.White,
                Colors.White,
                Color.FromRgb(235, 235, 235),
                Colors.White,
                Colors.White,
                Color.FromRgb(235, 245, 255)
            };

        private string _cellFontName = "Arial";
        private double _cellFontSize;
        private int _headerHeight;
        private int _headerWidth;
        private Dictionary<Tuple<bool, bool>, GlyphFont> _glyphFonts = new Dictionary<Tuple<bool, bool>, GlyphFont>();
        private Dictionary<Color, Brush> _solidBrushes = new Dictionary<Color, Brush>();
        private Color _cellFontColor = Colors.Black;
        private double _rowHeightReserve = 5;
        private Color _headerBackground = Color.FromRgb(0xDD, 0xDD, 0xDD);
        private Color _selectedColor = Color.FromRgb(51, 153, 255);
        private Color _selectedTextColor = Colors.White;
        private Color _mouseOverRowColor = Colors.Beige;
        private WriteableBitmap _drawBuffer;

        private bool _isInvalidated;
        private List<int> _invalidatedRows = new List<int>();
        private List<int> _invalidatedColumns = new List<int>();
        private List<Tuple<int, int>> _invalidatedCells = new List<Tuple<int, int>>();
        private List<int> _invalidatedRowHeaders = new List<int>();
        private List<int> _invalidatedColumnHeaders = new List<int>();

        private class InvalidationContext : IDisposable
        {
            private FastGridControl _grid;
            internal InvalidationContext(FastGridControl grid)
            {
                _grid = grid;
                _grid.EnterInvalidation();
            }

            public void Dispose()
            {
                _grid.LeaveInvalidation();
            }
        }

        private int _invalidationCount;
        private void LeaveInvalidation()
        {
            _invalidationCount--;
            if (_invalidationCount == 0)
            {
                if (_isInvalidated)
                {
                    RenderGrid();
                }
            }
        }

        private void EnterInvalidation()
        {
            _invalidationCount++;
        }

        private InvalidationContext CreateInvalidationContext()
        {
            return new InvalidationContext(this);
        }

        public FastGridControl()
        {
            InitializeComponent();
            //gridCore.Grid = this;
            CellFontSize = 12;
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

        public string CellFontName
        {
            get { return _cellFontName; }
            set
            {
                _cellFontName = value;
                RecalculateDefaultCellSize();
                RenderGrid();
            }
        }

        public double CellFontSize
        {
            get { return _cellFontSize; }
            set
            {
                _cellFontSize = value;
                RecalculateDefaultCellSize();
                RenderGrid();
            }
        }

        public double RowHeightReserve
        {
            get { return _rowHeightReserve; }
            set
            {
                _rowHeightReserve = value;
                RecalculateDefaultCellSize();
                RenderGrid();
            }
        }

        public Color CellFontColor
        {
            get { return _cellFontColor; }
            set
            {
                _cellFontColor = value;
                RenderGrid();
            }
        }

        public Color SelectedColor
        {
            get { return _selectedColor; }
            set
            {
                _selectedColor = value;
                RenderGrid();
            }
        }

        public Color SelectedTextColor
        {
            get { return _selectedTextColor; }
            set
            {
                _selectedTextColor = value;
                RenderGrid();
            }
        }

        public Color MouseOverRowColor
        {
            get { return _mouseOverRowColor; }
            set { _mouseOverRowColor = value; }
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
            _rowHeight = (int)(GetFont(false, false).TextHeight + CellPadding * 2 + 2 + RowHeightReserve);
            _columnWidth = _rowHeight * 4;
            HeaderWidth = GetTextWidth("0000", false, false);
            HeaderHeight = _rowHeight;
            AdjustScrollbars();
            InvalidateAll();
        }

        public int RowHeight
        {
            get { return _rowHeight; }
        }

        public int ColumnWidth
        {
            get { return _columnWidth; }
        }

        private void ScrollChanged()
        {
            int rowIndex = (int)((vscroll.Value + _rowHeight / 2.0) / _rowHeight);
            int columnIndex = (int)((hscroll.Value + _columnWidth / 2.0) / _columnWidth);
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
                int scrollY = (FirstVisibleRow - row) * RowHeight;
                if (row > FirstVisibleRow)
                {
                    for (int i = row + VisibleRowCount; i >= FirstVisibleRow + VisibleRowCount - 1; i--)
                    {
                        InvalidateRow(i);
                    }
                }
                else
                {
                    for (int i = row; i <= FirstVisibleRow; i++)
                    {
                        InvalidateRow(i);
                    }
                }
                FirstVisibleRow = row;
                _drawBuffer.ScrollY(scrollY, GetScrollRect());
                _drawBuffer.ScrollY(scrollY, GetRowHeadersRect());
                RenderGrid();
                return;
            }

            if (column != FirstVisibleColumn && !_isInvalidated && row == FirstVisibleRow
                && Math.Abs(column - FirstVisibleColumn) * 2 < VisibleColumnCount)
            {
                int scrollX = (FirstVisibleColumn - column) * ColumnWidth;
                if (column > FirstVisibleColumn)
                {
                    for (int i = column + VisibleColumnCount; i >= FirstVisibleColumn + VisibleColumnCount - 1; i--)
                    {
                        InvalidateColumn(i);
                    }
                }
                else
                {
                    for (int i = column; i <= FirstVisibleColumn; i++)
                    {
                        InvalidateColumn(i);
                    }
                }
                FirstVisibleColumn = column;
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

        public Color GridLineColor
        {
            get { return _gridLineColor; }
            set
            {
                _gridLineColor = value;
                InvalidateVisual();
            }
        }

        public Color[] AlternatingColors
        {
            get { return _alternatingColors; }
            set
            {
                if (value.Length < 1) throw new Exception("Invalid value");
                _alternatingColors = value;
                InvalidateVisual();
            }
        }

        public int CellPadding
        {
            get { return _cellPadding; }
            set
            {
                _cellPadding = value;
                InvalidateVisual();
            }
        }

        public Color HeaderBackground
        {
            get { return _headerBackground; }
            set
            {
                _headerBackground = value;
                InvalidateVisual();
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
            hscroll.Maximum = _columnWidth * _columnCount - GridScrollAreaWidth;
            hscroll.ViewportSize = GridScrollAreaWidth;

            vscroll.Minimum = 0;
            vscroll.Maximum = _rowHeight * _rowCount - GridScrollAreaHeight;
            vscroll.ViewportSize = GridScrollAreaHeight;
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
                _rowCount = _model.RowCount;
                _columnCount = _model.ColumnCount;
            }
            AdjustScrollbars();
            InvalidateVisual();
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
            get { return (int)((ActualHeight - HeaderHeight) / RowHeight) + 1; }
        }

        private int VisibleColumnCount
        {
            get { return (int)((ActualWidth - HeaderWidth) / ColumnWidth) + 1; }
        }

        private int GetRowTop(int row)
        {
            return (row - FirstVisibleRow) * RowHeight + HeaderHeight;
        }

        private int GetColumnLeft(int column)
        {
            return (column - FirstVisibleColumn) * ColumnWidth + HeaderWidth;
        }

        private IntRect GetCellRect(int row, int column)
        {
            return new IntRect(new IntPoint(GetColumnLeft(column), GetRowTop(row)), new IntSize(ColumnWidth + 1, RowHeight + 1));
        }

        private IntRect GetContentRect(IntRect rect)
        {
            return new IntRect(
                new IntPoint(rect.Left + CellPadding, rect.Top + CellPadding),
                new IntSize(rect.Width - 2*CellPadding, rect.Height - 2*CellPadding)
                );
        }

        private IntRect GetRowHeaderRect(int row)
        {
            return new IntRect(new IntPoint(0, GetRowTop(row)), new IntSize(HeaderWidth + 1, RowHeight + 1));
        }

        private IntRect GetColumnHeaderRect(int column)
        {
            return new IntRect(new IntPoint(GetColumnLeft(column), 0), new IntSize(ColumnWidth + 1, HeaderHeight + 1));
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
                        var cell = Model.GetCell(row, col);
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



                        var rectContent = GetContentRect(rect);
                        _drawBuffer.DrawRectangle(rect, GridLineColor);
                        _drawBuffer.FillRectangle(rectContent, selectedBgColor
                                                            ?? hoverRowColor
                                                            ?? cell.BackgroundColor
                                                            ?? GetAlternateBackground(row));

                        RenderCell(cell, rectContent, selectedTextColor);
                    }
                }

                for (int row = FirstVisibleRow; row < FirstVisibleRow + rowsToRender; row++)
                {
                    if (row < 0 || row >= _rowCount) continue;
                    var cell = Model.GetRowHeader(row);
                    if (!ShouldDrawRowHeader(row)) continue;

                    var rect = GetRowHeaderRect(row);
                    var rectContent = GetContentRect(rect);

                    _drawBuffer.DrawRectangle(rect, GridLineColor);
                    _drawBuffer.FillRectangle(rectContent, cell.BackgroundColor ?? HeaderBackground);
                    RenderCell(cell, rectContent, null);
                }

                for (int col = FirstVisibleColumn; col < FirstVisibleColumn + colsToRender; col++)
                {
                    if (col < 0 || col >= _columnCount) continue;
                    var cell = Model.GetColumnHeader(col);
                    if (!ShouldDrawColumnHeader(col)) continue;

                    var rect = GetColumnHeaderRect(col);
                    var rectContent = GetContentRect(rect);

                    _drawBuffer.DrawRectangle(rect, GridLineColor);
                    _drawBuffer.FillRectangle(rectContent, cell.BackgroundColor ?? HeaderBackground);
                    RenderCell(cell, rectContent, null);
                }
            }
            ClearInvalidation();
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
                    var cell = Model.GetCell(_inplaceEditorCell.Row.Value, _inplaceEditorCell.Column.Value);
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

            string text = Model.GetCell(cell.Row.Value, cell.Column.Value).GetEditText();

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
                return new FastGridCellAddress((int) ((pt.Y - HeaderHeight)/RowHeight) + FirstVisibleRow, null);
            }
            if (pt.Y < HeaderHeight)
            {
                return new FastGridCellAddress(null, (int) ((pt.X - HeaderWidth)/ColumnWidth) + FirstVisibleColumn);
            }
            return new FastGridCellAddress((int) ((pt.Y - HeaderHeight)/RowHeight) + FirstVisibleRow, (int) ((pt.X - HeaderWidth)/ColumnWidth) + FirstVisibleColumn);
        }

        private void SetCurrentCell(FastGridCellAddress cell)
        {
            using (var ctx = CreateInvalidationContext())
            {
                if (_currentCell.IsCell) InvalidateCell(_currentCell);
                _currentCell = cell;
                if (_currentCell.IsCell) InvalidateCell(_currentCell);
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

                SetHoverRow(cell.IsCell ? cell.Row.Value : (int?) null);
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


        private void RenderCell(IFastGridCell cell, IntRect rect, Color? selectedTextColor)
        {
            int count = cell.BlockCount;
            int rightCount = cell.RightAlignBlockCount;
            int leftCount = count - rightCount;
            int leftPos = rect.Left;
            int rightPos = rect.Right;

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
                var origin = new IntPoint(leftPos, rect.Top + (int) Math.Round(rect.Height/2.0 - textHeight/2.0));
                //int maxWidth = rect.Right - origin.X;
                int width = _drawBuffer.DrawString(origin.X, origin.Y, rect, selectedTextColor ?? color ?? CellFontColor, font, text);
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
    }
}
