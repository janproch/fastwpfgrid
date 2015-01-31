using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FastWpfGrid
{
    public class FastGridCore : Control
    {
        public int FirstVisibleColumn;
        public int FirstVisibleRow;
        private RenderTargetBitmap _scrollBuffer;

        private int _scrollY = 0;
        private int _scrollX = 0;

        private bool _isInvalidated;
        private List<int> _invalidatedRows = new List<int>();
        private List<int> _invalidatedColumns = new List<int>();
        private List<Tuple<int, int>> _invalidatedCells = new List<Tuple<int, int>>();
        private List<int> _invalidatedRowHeaders=new List<int>();
        private List<int> _invalidatedColumnHeaders = new List<int>();

        public FastGridControl Grid;
        private FastGridCellAddress _currentCell;
        private bool _isLeftMouseDown;
        private int? _mouseOverRow;

        public FastGridCore()
        {
            SnapsToDevicePixels = true;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (Grid == null) return;

            var emptyPen = new Pen();
            var cellPen = new Pen(new SolidColorBrush(Grid.GridLineColor), 1);

            var start = DateTime.Now;

            try
            {
                dc.DrawRectangle(Brushes.White, emptyPen, new Rect(Grid.HeaderWidth, Grid.HeaderHeight,
                    (int)ActualWidth - Grid.HeaderWidth, (int)ActualHeight - Grid.HeaderHeight));

                if (_scrollY != 0)
                {
                    dc.PushClip(new RectangleGeometry(new Rect(0, Grid.HeaderHeight,
                        (int)ActualWidth, (int)ActualHeight - Grid.HeaderHeight)));
                    dc.DrawImage(_scrollBuffer, new Rect(0, _scrollY, (int)ActualWidth, (int)ActualHeight));
                    dc.Pop();
                }

                if (_scrollX != 0)
                {
                    dc.PushClip(new RectangleGeometry(new Rect(Grid.HeaderWidth, 0,
                        (int)ActualWidth - Grid.HeaderWidth, (int)ActualHeight)));
                    dc.DrawImage(_scrollBuffer, new Rect(_scrollX, 0, (int)ActualWidth, (int)ActualHeight));
                    dc.Pop();
                }

                if (_scrollX == 0 && _scrollY == 0 && _isInvalidated)
                {
                    dc.DrawImage(_scrollBuffer, new Rect(0, 0, (int)ActualWidth, (int)ActualHeight));
                }

                if (Grid == null || Grid.Model == null) return;
                int colsToRender = VisibleColumnCount;
                int rowsToRender = VisibleRowCount;

                dc.PushClip(new RectangleGeometry(new Rect(Grid.HeaderWidth, Grid.HeaderHeight,
                    (int)ActualWidth - Grid.HeaderWidth, (int)ActualHeight - Grid.HeaderHeight)));

                for (int row = FirstVisibleRow; row < FirstVisibleRow + rowsToRender; row++)
                {
                    for (int col = FirstVisibleColumn; col < FirstVisibleColumn + colsToRender; col++)
                    {
                        if (!ShouldDrawCell(row, col)) continue;
                        var rect = GetCellRect(row, col);
                        var cell = Grid.Model.GetCell(row, col);
                        Color? selectedBgColor = null;
                        Color? selectedTextColor = null;
                        Color? hoverRowColor = null;
                        if (_currentCell.TestCell(row, col))
                        {
                            selectedBgColor = Grid.SelectedColor;
                            selectedTextColor = Grid.SelectedTextColor;
                        }
                        if (row == _mouseOverRow)
                        {
                            hoverRowColor = Grid.MouseOverRowColor;
                        }

                        dc.DrawRectangle(Grid.GetSolidBrush(selectedBgColor 
                            ?? hoverRowColor 
                            ?? cell.BackgroundColor 
                            ?? Grid.GetAlternateBackground(row)),
                                         cellPen, rect);

                        var rectContent = GetContentRect(rect);

                        RenderCell(cell, rectContent, dc, selectedTextColor);
                    }
                }
                dc.Pop();

                for (int row = FirstVisibleRow; row < FirstVisibleRow + rowsToRender; row++)
                {
                    var cell = Grid.Model.GetRowHeader(row);
                    if (!ShouldDrawRowHeader(row)) continue;

                    var rect = GetRowHeaderRect(row);

                    dc.DrawRectangle(Grid.GetSolidBrush(cell.BackgroundColor ?? Grid.HeaderBackground), cellPen, rect);
                    var rectContent = GetContentRect(rect);
                    RenderCell(cell, rectContent, dc, null);
                }

                for (int col = FirstVisibleColumn; col < FirstVisibleColumn + colsToRender; col++)
                {
                    var cell = Grid.Model.GetColumnHeader(col);
                    if (!ShouldDrawColumnHeader(col)) continue;

                    var rect = GetColumnHeaderRect(col);

                    dc.DrawRectangle(Grid.GetSolidBrush(cell.BackgroundColor ?? Grid.HeaderBackground), cellPen, rect);
                    var rectContent = GetContentRect(rect);
                    RenderCell(cell, rectContent, dc, null);
                }

                RenderCell00(dc);
            }
            finally
            {
                ClearInvalidation();
            }

            Debug.WriteLine((DateTime.Now - start).TotalMilliseconds);
        }

        public FastGridCellAddress GetCellAddress(Point pt)
        {
            if (pt.X <= Grid.HeaderWidth && pt.Y < Grid.HeaderHeight)
            {
                return new FastGridCellAddress();
            }
            if (pt.X < Grid.HeaderWidth)
            {
                return new FastGridCellAddress
                    {
                        Row = (int) ((pt.Y - Grid.HeaderHeight)/Grid.RowHeight) + FirstVisibleRow,
                    };
            }
            if (pt.Y < Grid.HeaderHeight)
            {
                return new FastGridCellAddress
                    {
                        Column = (int) ((pt.X - Grid.HeaderWidth)/Grid.ColumnWidth) + FirstVisibleColumn,
                    };
            }
            return new FastGridCellAddress
                {
                    Row = (int)((pt.Y - Grid.HeaderHeight) / Grid.RowHeight) + FirstVisibleRow,
                    Column = (int)((pt.X - Grid.HeaderWidth) / Grid.ColumnWidth) + FirstVisibleColumn,
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
                FinishInvalidate();
            }
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            //base.OnMouseLeftButtonDown(e);
            //_isLeftMouseDown = true;
            //var pt = e.GetPosition(this);
            //HandleLeftButtonDownMove(pt);
            //var cell = GetCellAddress(pt);
            //if (cell.IsCell) Grid.ShowTextEditor(
            //    GetCellRect(cell.Row.Value, cell.Column.Value), 
            //    Grid.Model.GetCell(cell.Row.Value, cell.Column.Value).GetEditText());
        }

        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            _isLeftMouseDown = false;
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var pt = e.GetPosition(this);
            if (_isLeftMouseDown) HandleLeftButtonDownMove(pt);
            var cell = GetCellAddress(pt);
            SetHoverRow(cell.IsCell ? cell.Row.Value : (int?) null);
        }

        private void SetHoverRow(int? row)
        {
            if (row == _mouseOverRow) return;
            if (_mouseOverRow.HasValue) InvalidateRow(_mouseOverRow.Value);
            _mouseOverRow = row;
            if (_mouseOverRow.HasValue) InvalidateRow(_mouseOverRow.Value);
            FinishInvalidate();
        }

        private void RenderCell00(DrawingContext dc)
        {
            dc.DrawRectangle(Brushes.White, new Pen(), new Rect(0, 0, Grid.HeaderWidth, Grid.HeaderHeight));
        }

        private void RenderCell(IFastGridCell cell, Rect rect, DrawingContext dc, Color? selectedTextColor)
        {
            //int count = cell.BlockCount;
            //int rightCount = cell.RightAlignBlockCount;
            //int leftCount = count - rightCount;
            //double leftPos = rect.Left;
            //double rightPos = rect.Right;

            //for (int i = 0; i < leftCount && leftPos < rightPos; i++)
            //{
            //    var block = cell.GetBlock(i);
            //    string text = block.TextData;
            //    bool isBold = block.IsBold;
            //    bool isItalic = block.IsItalic;
            //    var color = block.FontColor;
            //    var glyphTypeface = Grid.GetGlyphTypeface(isBold, isItalic);
            //    double textHeight = glyphTypeface.Height*Grid.CellFontSize;
            //    var origin = new Point(leftPos, rect.Top + (int) (rect.Height/2 - textHeight/2));
            //    int maxWidth = (int) (rect.Right - origin.X);
            //    int width = RenderText(glyphTypeface, text, dc, origin, maxWidth, selectedTextColor ?? color ?? Grid.CellFontColor);
            //    leftPos += width;
            //}
        }

        private int RenderText(GlyphTypeface glyphTypeface, string text, DrawingContext dc, Point origin, int maxWidth, Color fontColor)
        {
            if (maxWidth < 0) return 0;
            double size = Grid.CellFontSize;

            var glyphIndexes = new List<ushort>();
            var advanceWidths = new List<double>();

            int totalWidth = 0;

            for (int n = 0; n < text.Length; n++)
            {
                ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];
                double width = Math.Round(glyphTypeface.AdvanceWidths[glyphIndex]*size);

                if (totalWidth + width > maxWidth) break;

                glyphIndexes.Add(glyphIndex);
                advanceWidths.Add(width);

                totalWidth += (int) width;
            }

            origin = new Point((int) origin.X, (int) (origin.Y + glyphTypeface.Height*size));
            var glyphRun = new GlyphRun(glyphTypeface, 0, false, size,
                                        glyphIndexes, origin, advanceWidths, null, null, null, null,
                                        null, null);

            dc.DrawGlyphRun(Grid.GetSolidBrush(fontColor), glyphRun);

            return totalWidth;

            //double y = origin.Y;
            //dc.DrawLine(new Pen(Brushes.Red, 1), new Point(origin.X, y),
            //    new Point(origin.X + totalWidth, y));

            //y -= (glyphTypeface.Baseline * size);
            //dc.DrawLine(new Pen(Brushes.Green, 1), new Point(origin.X, y),
            //    new Point(origin.X + totalWidth, y));

            //y += (glyphTypeface.Height * size);
            //dc.DrawLine(new Pen(Brushes.Blue, 1), new Point(origin.X, y),
            //    new Point(origin.X + totalWidth, y));
        }

        private void ClearInvalidation()
        {
            _invalidatedRows.Clear();
            _invalidatedColumns.Clear();
            _invalidatedCells.Clear();
            _invalidatedColumnHeaders.Clear();
            _invalidatedRowHeaders.Clear();
            _isInvalidated = false;
            _scrollX = 0;
            _scrollY = 0;
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
            get { return (int) ((ActualHeight - Grid.HeaderHeight)/Grid.RowHeight) + 1; }
        }

        private int VisibleColumnCount
        {
            get { return (int) ((ActualWidth - Grid.HeaderWidth)/Grid.ColumnWidth) + 1; }
        }

        private int GetRowTop(int row)
        {
            return (row - FirstVisibleRow)*Grid.RowHeight + Grid.HeaderHeight;
        }

        private int GetColumnLeft(int column)
        {
            return (column - FirstVisibleColumn)*Grid.ColumnWidth + Grid.HeaderWidth;
        }

        private Rect GetCellRect(int row, int column)
        {
            return new Rect(GetColumnLeft(column), GetRowTop(row), Grid.ColumnWidth, Grid.RowHeight);
        }

        private Rect GetContentRect(Rect rect)
        {
            return new Rect(
                rect.Left + Grid.CellPadding, 
                rect.Top + Grid.CellPadding, 
                rect.Width - 2*Grid.CellPadding, 
                rect.Height - 2*Grid.CellPadding);
        }

        private Rect GetRowHeaderRect(int row)
        {
            return new Rect(0, GetRowTop(row), Grid.HeaderWidth, Grid.RowHeight);
        }

        private Rect GetColumnHeaderRect(int column)
        {
            return new Rect(GetColumnLeft(column), 0, Grid.ColumnWidth, Grid.HeaderHeight);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            _scrollBuffer = new RenderTargetBitmap((int) ActualWidth, (int) ActualHeight, 96, 96, PixelFormats.Pbgra32);
        }

        //private bool IsInvalidated
        //{
        //    get
        //    {
        //        if (_scrollX != 0) return true;
        //        if (_scrollY != 0) return true;
        //        return false;
        //    }
        //}

        public void ScrollContent(int row, int column)
        {
            //_scrollDraw = true;
            if (row == FirstVisibleRow && column == FirstVisibleColumn)
            {
                return;
            }

            if (row != FirstVisibleRow && !_isInvalidated && column == FirstVisibleColumn
                && Math.Abs(row - FirstVisibleRow) * 2 < VisibleRowCount)
            {
                _scrollY = (FirstVisibleRow - row) * Grid.RowHeight;
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
                FinishInvalidate();
                return;
            }

            if (column != FirstVisibleColumn && !_isInvalidated && row == FirstVisibleRow
                && Math.Abs(column - FirstVisibleColumn) * 2 < VisibleColumnCount)
            {
                _scrollX = (FirstVisibleColumn - column) * Grid.ColumnWidth;
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
                FinishInvalidate();
                return;
            }


            // render all
            ClearInvalidation();
            FirstVisibleRow = row;
            FirstVisibleColumn = column;
            InvalidateVisual();
        }

        private void InvalidateColumn(int column)
        {
            _isInvalidated = true;
            _invalidatedColumns.Add(column);
            _invalidatedColumnHeaders.Add(column);
        }

        private void InvalidateRow(int row)
        {
            _isInvalidated = true;
            _invalidatedRows.Add(row);
            _invalidatedRowHeaders.Add(row);
        }

        private void InvalidateCell(int row, int column)
        {
            _isInvalidated = true;
            _invalidatedCells.Add(Tuple.Create(row, column));
        }

        private void FinishInvalidate()
        {
            _scrollBuffer.Render(this);
            InvalidateVisual();
        }
    }
}
