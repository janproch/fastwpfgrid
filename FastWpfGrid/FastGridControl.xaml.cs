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

            FixCurrentCellAndSetSelectionToCurrentCell();

            RecountColumnWidths();
            AdjustScrollbars();
            InvalidateAll();
        }

        private void FixCurrentCellAndSetSelectionToCurrentCell()
        {
            int? col = _currentCell.Column;
            int? row = _currentCell.Row;

            if (col.HasValue)
            {
                if (col >= _columnCount) col = _columnCount - 1;
                if (col < 0) col = null;
            }

            if (row.HasValue)
            {
                if (row >= _rowCount) row = _rowCount - 1;
                if (row < 0) row = null;
            }

            _selectedCells.Clear();
            _currentCell = new FastGridCellAddress(row, col);
            if (_currentCell.IsCell) _selectedCells.Add(_currentCell);
            OnChangeSelectedCells();
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

        private void MoveCurrentCell(int? row, int? col, KeyEventArgs e = null)
        {
            if (e != null) e.Handled = true;
            _selectedCells.ToList().ForEach(InvalidateCell);
            _selectedCells.Clear();

            InvalidateCurrentCell();

            if (row < 0) row = 0;
            if (row >= _rowCount) row = _rowCount - 1;
            if (col < 0) col = 0;
            if (col >= _columnCount) col = _columnCount - 1;

            _currentCell = new FastGridCellAddress(row, col);
            if (_currentCell.IsCell) _selectedCells.Add(_currentCell);
            InvalidateCurrentCell();
            ScrollCurrentCellIntoView();
            OnChangeSelectedCells();
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

        public HashSet<FastGridCellAddress> SelectedCells
        {
            get { return _selectedCells; }
        }
    }
}
