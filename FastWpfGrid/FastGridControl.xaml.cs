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
        private bool _isLeftMouseDown;
        private int? _mouseOverRow;

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
                InvalidateVisual();
            }
        }

        public double CellFontSize
        {
            get { return _cellFontSize; }
            set
            {
                _cellFontSize = value;
                RecalculateDefaultCellSize();
                InvalidateVisual();
            }
        }

        public double RowHeightReserve
        {
            get { return _rowHeightReserve; }
            set
            {
                _rowHeightReserve = value;
                RecalculateDefaultCellSize();
                InvalidateVisual();
            }
        }

        public Color CellFontColor
        {
            get { return _cellFontColor; }
            set
            {
                _cellFontColor = value;
                InvalidateVisual();
            }
        }

        public Color SelectedColor
        {
            get { return _selectedColor; }
            set
            {
                _selectedColor = value;
                InvalidateVisual();
            }
        }

        public Color SelectedTextColor
        {
            get { return _selectedTextColor; }
            set
            {
                _selectedTextColor = value;
                InvalidateVisual();
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
            FirstVisibleRow = rowIndex;
            FirstVisibleColumn = columnIndex;
            RenderGrid();
            //gridCore.ScrollContent(rowIndex, columnIndex);
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

        public void InvalidateCell(int row, int column)
        {
            InvalidateAll();
        }

        public void InvalidateRowHeader(int row)
        {
            InvalidateAll();
        }

        public void InvalidateColumnHeader(int column)
        {
            InvalidateAll();
        }

        //private void InvalidateColumn(int column)
        //{
        //    _isInvalidated = true;
        //    _invalidatedColumns.Add(column);
        //    _invalidatedColumnHeaders.Add(column);
        //}

        //private void InvalidateRow(int row)
        //{
        //    _isInvalidated = true;
        //    _invalidatedRows.Add(row);
        //    _invalidatedRowHeaders.Add(row);
        //}

        //private void InvalidateCell(int row, int column)
        //{
        //    _isInvalidated = true;
        //    _invalidatedCells.Add(Tuple.Create(row, column));
        //}

        //private void FinishInvalidate()
        //{
        //    _scrollBuffer.Render(this);
        //    InvalidateVisual();
        //}

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

        public void ShowTextEditor(IntRect rect, string text)
        {
            edText.Margin = new Thickness
                {
                    Left = rect.Left,
                    Top = rect.Top,
                    Right = imageGrid.ActualWidth - rect.Right,
                    Bottom = imageGrid.ActualHeight - rect.Bottom,
                };
            edText.Text = text;
            edText.Visibility = Visibility.Visible;
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

        //int x()
        //{
        //    base.OnRender(dc);

        //    if (Grid == null) return;

        //    var emptyPen = new Pen();
        //    var cellPen = new Pen(new SolidColorBrush(Grid.GridLineColor), 1);

        //    var start = DateTime.Now;

        //    try
        //    {
        //        dc.DrawRectangle(Brushes.White, emptyPen, new Rect(Grid.HeaderWidth, Grid.HeaderHeight,
        //                                                           (int) ActualWidth - Grid.HeaderWidth, (int) ActualHeight - Grid.HeaderHeight));

        //        if (_scrollY != 0)
        //        {
        //            dc.PushClip(new RectangleGeometry(new Rect(0, Grid.HeaderHeight,
        //                                                       (int) ActualWidth, (int) ActualHeight - Grid.HeaderHeight)));
        //            dc.DrawImage(_scrollBuffer, new Rect(0, _scrollY, (int) ActualWidth, (int) ActualHeight));
        //            dc.Pop();
        //        }

        //        if (_scrollX != 0)
        //        {
        //            dc.PushClip(new RectangleGeometry(new Rect(Grid.HeaderWidth, 0,
        //                                                       (int) ActualWidth - Grid.HeaderWidth, (int) ActualHeight)));
        //            dc.DrawImage(_scrollBuffer, new Rect(_scrollX, 0, (int) ActualWidth, (int) ActualHeight));
        //            dc.Pop();
        //        }

        //        if (_scrollX == 0 && _scrollY == 0 && _isInvalidated)
        //        {
        //            dc.DrawImage(_scrollBuffer, new Rect(0, 0, (int) ActualWidth, (int) ActualHeight));
        //        }

        //        if (Grid == null || Grid.Model == null) return;
        //        int colsToRender = VisibleColumnCount;
        //        int rowsToRender = VisibleRowCount;

        //        dc.PushClip(new RectangleGeometry(new Rect(Grid.HeaderWidth, Grid.HeaderHeight,
        //                                                   (int) ActualWidth - Grid.HeaderWidth, (int) ActualHeight - Grid.HeaderHeight)));

        //        for (int row = FirstVisibleRow; row < FirstVisibleRow + rowsToRender; row++)
        //        {
        //            for (int col = FirstVisibleColumn; col < FirstVisibleColumn + colsToRender; col++)
        //            {
        //                if (!ShouldDrawCell(row, col)) continue;
        //                var rect = GetCellRect(row, col);
        //                var cell = Grid.Model.GetCell(row, col);
        //                Color? selectedBgColor = null;
        //                Color? selectedTextColor = null;
        //                Color? hoverRowColor = null;
        //                if (_currentCell.TestCell(row, col))
        //                {
        //                    selectedBgColor = Grid.SelectedColor;
        //                    selectedTextColor = Grid.SelectedTextColor;
        //                }
        //                if (row == _mouseOverRow)
        //                {
        //                    hoverRowColor = Grid.MouseOverRowColor;
        //                }

        //                dc.DrawRectangle(Grid.GetSolidBrush(selectedBgColor
        //                                                    ?? hoverRowColor
        //                                                    ?? cell.BackgroundColor
        //                                                    ?? Grid.GetAlternateBackground(row)),
        //                                 cellPen, rect);

        //                var rectContent = GetContentRect(rect);

        //                RenderCell(cell, rectContent, dc, selectedTextColor);
        //            }
        //        }
        //        dc.Pop();

        //        for (int row = FirstVisibleRow; row < FirstVisibleRow + rowsToRender; row++)
        //        {
        //            var cell = Grid.Model.GetRowHeader(row);
        //            if (!ShouldDrawRowHeader(row)) continue;

        //            var rect = GetRowHeaderRect(row);

        //            dc.DrawRectangle(Grid.GetSolidBrush(cell.BackgroundColor ?? Grid.HeaderBackground), cellPen, rect);
        //            var rectContent = GetContentRect(rect);
        //            RenderCell(cell, rectContent, dc, null);
        //        }

        //        for (int col = FirstVisibleColumn; col < FirstVisibleColumn + colsToRender; col++)
        //        {
        //            var cell = Grid.Model.GetColumnHeader(col);
        //            if (!ShouldDrawColumnHeader(col)) continue;

        //            var rect = GetColumnHeaderRect(col);

        //            dc.DrawRectangle(Grid.GetSolidBrush(cell.BackgroundColor ?? Grid.HeaderBackground), cellPen, rect);
        //            var rectContent = GetContentRect(rect);
        //            RenderCell(cell, rectContent, dc, null);
        //        }

        //        RenderCell00(dc);
        //    }
        //    finally
        //    {
        //        ClearInvalidation();
        //    }

        //    Debug.WriteLine((DateTime.Now - start).TotalMilliseconds);
        //}

        private void ClearInvalidation()
        {
            _invalidatedRows.Clear();
            _invalidatedColumns.Clear();
            _invalidatedCells.Clear();
            _invalidatedColumnHeaders.Clear();
            _invalidatedRowHeaders.Clear();
            _isInvalidated = false;
            //_scrollX = 0;
            //_scrollY = 0;
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

        private void RenderGrid()
        {
            if (_drawBuffer == null) return;
            using (_drawBuffer.GetBitmapContext())
            {
                int colsToRender = VisibleColumnCount;
                int rowsToRender = VisibleRowCount;

                _drawBuffer.Clear(Colors.White);

                for (int row = FirstVisibleRow; row < FirstVisibleRow + rowsToRender; row++)
                {
                    for (int col = FirstVisibleColumn; col < FirstVisibleColumn + colsToRender; col++)
                    {
                        if (!ShouldDrawCell(row, col)) continue;
                        var rect = GetCellRect(row, col);
                        var cell = Model.GetCell(row, col);
                        Color? selectedBgColor = null;
                        Color? selectedTextColor = null;
                        Color? hoverRowColor = null;
                        if (_currentCell.TestCell(row, col))
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
                    var cell = Model.GetColumnHeader(col);
                    if (!ShouldDrawColumnHeader(col)) continue;

                    var rect = GetColumnHeaderRect(col);
                    var rectContent = GetContentRect(rect);

                    _drawBuffer.DrawRectangle(rect, GridLineColor);
                    _drawBuffer.FillRectangle(rectContent, cell.BackgroundColor ?? HeaderBackground);
                    RenderCell(cell, rectContent, null);
                }

            }
        }

        private void RenderCell00()
        {
            //dc.DrawRectangle(Brushes.White, new Pen(), new Rect(0, 0, Grid.HeaderWidth, Grid.HeaderHeight));
        }

        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            _isLeftMouseDown = false;
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _isLeftMouseDown = true;
            var pt = e.GetPosition(this);
            HandleLeftButtonDownMove(pt);
            var cell = GetCellAddress(pt);
            if (cell.IsCell) ShowTextEditor(
                GetCellRect(cell.Row.Value, cell.Column.Value),
                Model.GetCell(cell.Row.Value, cell.Column.Value).GetEditText());
        }


        public FastGridCellAddress GetCellAddress(Point pt)
        {
            if (pt.X <= HeaderWidth && pt.Y < HeaderHeight)
            {
                return new FastGridCellAddress();
            }
            if (pt.X < HeaderWidth)
            {
                return new FastGridCellAddress
                {
                    Row = (int)((pt.Y - HeaderHeight) / RowHeight) + FirstVisibleRow,
                };
            }
            if (pt.Y < HeaderHeight)
            {
                return new FastGridCellAddress
                {
                    Column = (int)((pt.X - HeaderWidth) / ColumnWidth) + FirstVisibleColumn,
                };
            }
            return new FastGridCellAddress
            {
                Row = (int)((pt.Y - HeaderHeight) / RowHeight) + FirstVisibleRow,
                Column = (int)((pt.X - HeaderWidth) / ColumnWidth) + FirstVisibleColumn,
            };
        }

        private void HandleLeftButtonDownMove(Point pt)
        {
            var cell = GetCellAddress(pt);
            if (cell.IsCell)
            {
                if (_currentCell.IsCell) InvalidateCell(_currentCell.Row.Value, _currentCell.Column.Value);
                _currentCell = cell;
                InvalidateCell(_currentCell.Row.Value, _currentCell.Column.Value);
                //FinishInvalidate();
            }
        }


        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var pt = e.GetPosition(this);
            if (_isLeftMouseDown) HandleLeftButtonDownMove(pt);
            var cell = GetCellAddress(pt);
            SetHoverRow(cell.IsCell ? cell.Row.Value : (int?)null);
        }

        private void SetHoverRow(int? row)
        {
            if (row == _mouseOverRow) return;
            //if (_mouseOverRow.HasValue) InvalidateRow(_mouseOverRow.Value);
            _mouseOverRow = row;
            //if (_mouseOverRow.HasValue) InvalidateRow(_mouseOverRow.Value);
            //FinishInvalidate();
            RenderGrid();
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
            //var vis = new DrawingVisual();
            //var rdp=new Ren
            //var tb = new TextBlock {Text = "Ahoj"};
            //var bmp = new RenderTargetBitmap((int)tb.ActualWidth, (int)tb.ActualHeight, 92, 92, PixelFormats.Pbgra32);

            //var bmp2 = new WriteableBitmap(bmp);

            //var typeface = new Typeface(this.FontFamily, FontStyles.Normal, FontWeights.Normal, new FontStretch());

            //FormattedText text = new FormattedText("Grid 1",
            //            new CultureInfo("en-us"),
            //            FlowDirection.LeftToRight,
            //            typeface,
            //            this.FontSize,
            //            Brushes.White);

            //DrawingVisual drawingVisual = new DrawingVisual();
            //DrawingContext drawingContext = drawingVisual.RenderOpen();
            //drawingContext.DrawRectangle(Brushes.Black, new Pen(), new Rect(0, 0, 180, 180));
            //drawingContext.DrawText(text, new Point(2, 2));
            //drawingContext.Close();

            //RenderTargetBitmap bmp = new RenderTargetBitmap(180, 180, 96, 96, PixelFormats.Pbgra32);
            //bmp.Render(drawingVisual);


            //var bmp2 = new WriteableBitmap(bmp);

            int width = (int)imageGrid.ActualWidth - 2;
            int height = (int)imageGrid.ActualHeight - 2;
            if (width > 0 && height > 0)
            {
                //_drawBuffer = new WriteableBitmap(width, height, 96.0, 96.0, PixelFormats.Pbgra32, null);
                _drawBuffer = BitmapFactory.New(width, height);
            }
            else
            {
                _drawBuffer = null;
            }
            image.Source = _drawBuffer;
            image.Width = width;
            image.Height = height;

            //image.Source = bmp2;
            //image.Width = bmp2.Width;
            //image.Height = bmp2.Height;
            //image.HorizontalAlignment = HorizontalAlignment.Left;
            //image.VerticalAlignment = VerticalAlignment.Top;
            //image.Margin = new Thickness(0, 30, 0, 0);

            AdjustScrollbars();
            RenderGrid();


            //if (_drawBuffer != null)
            //{
            //    //var start = DateTime.Now;
            //    //for (int i = 0; i < 2700; i++)
            //    //{
            //    //    var g = LetterGlyph.CreateGlyph(typeface, 12, 'G');
            //    //}
            //    //Debug.WriteLine((DateTime.Now - start).TotalMilliseconds);
            //    //var glyph = LetterGlyph.CreateGlyph(typeface, 12, 'G');

            //    using (_drawBuffer.GetBitmapContext())
            //    {
            //        _drawBuffer.Clear(Colors.White);
            //        _drawBuffer.DrawString(0, 40, Colors.Red, new Typeface(this.FontFamily, FontStyles.Normal, FontWeights.Normal, new FontStretch()), 12, "Pokusny_text");
            //        _drawBuffer.DrawString(0, 80, Colors.Blue, new Typeface(this.FontFamily, FontStyles.Normal, FontWeights.Normal, new FontStretch()), 12, "Pokusny_text");

            //        //int count = 0;
            //        //for (int x = 0; x < _drawBuffer.PixelWidth / glyph.Width; x++)
            //        //{
            //        //    for (int y = 0; y < _drawBuffer.PixelHeight / glyph.Height; y++)
            //        //    {
            //        //        _drawBuffer.DrawLetter(x * glyph.Width, y * glyph.Height + 30, Colors.Black, glyph);
            //        //        count++;
            //        //    }
            //        //}


            //        //_drawBuffer.Blit(new Point(100, 50), bmp2, new Rect(0, 0, 100, 30), Colors.White, WriteableBitmapExtensions.BlendMode.Alpha);
            //    }

            //    //_drawBuffer.DrawLetter(100, 140, Colors.Black, glyph);


            //    //try
            //    //{
            //    //    //_drawBuffer.DrawQuad();
            //    //    _drawBuffer.Lock();
            //    //    _drawBuffer.Clear(Colors.White);
            //    //    _drawBuffer.AddDirtyRect(new Int32Rect(0, 0, _drawBuffer.PixelWidth, _drawBuffer.PixelHeight));
            //    //}
            //    //finally
            //    //{
            //    //    _drawBuffer.Unlock();
            //    //}
            //}
        }
    }
}
