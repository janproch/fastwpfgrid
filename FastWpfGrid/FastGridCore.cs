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
        private List<int> _invalidatedRows = new List<int>();
        private List<int> _invalidatedColumns = new List<int>();

        public FastGridControl Grid;

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            var emptyPen = new Pen();
            var cellPen = new Pen(new SolidColorBrush(Grid.GridLineColor), 1);
            var typeFace = new Typeface(Grid.CellFontName);

            var start = DateTime.Now;

            //Typeface typeface = new Typeface(new FontFamily("Arial"),
            //        FontStyles.Italic,
            //        FontWeights.Normal,
            //        FontStretches.Normal);

            GlyphTypeface glyphTypeface;
            if (!typeFace.TryGetGlyphTypeface(out glyphTypeface))
                throw new InvalidOperationException("No glyphtypeface found");

            double textHeight = glyphTypeface.Height*Grid.CellFontSize;

            dc.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
            try
            {

                dc.DrawRectangle(Brushes.White, emptyPen, new Rect(0, 0, ActualWidth, ActualHeight));

                if (_scrollY != 0)
                {
                    dc.DrawImage(_scrollBuffer, new Rect(0, _scrollY, _scrollBuffer.Width, _scrollBuffer.Height));
                }

                if (_scrollX != 0)
                {
                    dc.DrawImage(_scrollBuffer, new Rect(_scrollX, 0, _scrollBuffer.Width, _scrollBuffer.Height));
                }

                if (Grid == null || Grid.Model == null) return;
                int colsToRender = VisibleColumnCount;
                int rowsToRender = VisibleRowCount;

                for (int row = FirstVisibleRow; row < FirstVisibleRow + rowsToRender; row++)
                {
                    for (int col = FirstVisibleColumn; col < FirstVisibleColumn + colsToRender; col++)
                    {
                        if (!ShouldDrawCell(row, col)) continue;
                        var rect = GetCellRect(row, col);
                        dc.DrawRectangle(Grid.GetAlternateBackground(row), cellPen, rect);

                        //var text = new FormattedText(Grid.Model.GetCellText(row, col),
                        //                             CultureInfo.InvariantCulture,
                        //                             FlowDirection.LeftToRight,
                        //                             typeFace,
                        //                             12,
                        //                             System.Windows.Media.Brushes.Black);

                        var rectContent = GetCellContentRect(row, col);

                        //dc.PushClip(new RectangleGeometry(rectContent));
                        RenderText(glyphTypeface, Grid.Model.GetCellText(row, col), dc, new Point(rectContent.Left, rectContent.Top + (int)(rectContent.Height / 2 - textHeight / 2)));
                        //dc.DrawText(text, new Point(rectContent.Left, rectContent.Top + (int)(rectContent.Height / 2 - text.Height / 2)));
                        //dc.Pop();
                    }
                }
            }
            finally
            {
                dc.Pop();
                ClearInvalidation();
            }

            Debug.WriteLine((DateTime.Now - start).TotalMilliseconds);
        }

        private void RenderText(GlyphTypeface glyphTypeface, string text, DrawingContext dc, Point origin)
        {
            double textHeight = glyphTypeface.Height * Grid.CellFontSize;

            double size = Grid.CellFontSize;

            ushort[] glyphIndexes = new ushort[text.Length];
            double[] advanceWidths = new double[text.Length];

            double totalWidth = 0;

            for (int n = 0; n < text.Length; n++)
            {
                ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];
                glyphIndexes[n] = glyphIndex;

                double width = Math.Round(glyphTypeface.AdvanceWidths[glyphIndex] * size);
                advanceWidths[n] = width;

                totalWidth += width;
            }

            origin = new Point((int) origin.X, (int)(origin.Y + glyphTypeface.Height * size));
            GlyphRun glyphRun = new GlyphRun(glyphTypeface, 0, false, size,
                                             glyphIndexes, origin, advanceWidths, null, null, null, null,
                                             null, null);

            dc.DrawGlyphRun(Brushes.Black, glyphRun);

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
            _scrollX = 0;
            _scrollY = 0;
        }

        private bool ShouldDrawCell(int row, int column)
        {
            if (_invalidatedRows.Any())
            {
                return _invalidatedRows.Contains(row);
            }
            if (_invalidatedColumns.Any())
            {
                return _invalidatedColumns.Contains(column);
            }
            return true;
        }

        private int VisibleRowCount
        {
            get { return (int) (ActualHeight/Grid.RowHeight) + 1; }
        }

        private int VisibleColumnCount
        {
            get { return (int) (ActualWidth/Grid.ColumnWidth) + 1; }
        }

        private Rect GetCellRect(int row, int column)
        {
            return new Rect((column - FirstVisibleColumn)*Grid.ColumnWidth, (row - FirstVisibleRow)*Grid.RowHeight, Grid.ColumnWidth, Grid.RowHeight);
        }

        private Rect GetCellContentRect(int row, int column)
        {
            return new Rect((column - FirstVisibleColumn)*Grid.ColumnWidth + Grid.CellPadding,
                            (row - FirstVisibleRow)*Grid.RowHeight + Grid.CellPadding,
                            Grid.ColumnWidth - 2*Grid.CellPadding,
                            Grid.RowHeight - 2*Grid.CellPadding);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            _scrollBuffer = new RenderTargetBitmap((int) sizeInfo.NewSize.Width + 1, (int) sizeInfo.NewSize.Height + 1, 96, 96, PixelFormats.Pbgra32);
        }

        private bool IsInvalidated
        {
            get
            {
                if (_scrollX != 0) return true;
                if (_scrollY != 0) return true;
                return false;
            }
        }

        public void ScrollContent(int row, int column)
        {
            //_scrollDraw = true;
            if (row == FirstVisibleRow && column == FirstVisibleColumn)
            {
                return;
            }

            if (row != FirstVisibleRow && !IsInvalidated && column == FirstVisibleColumn
                && Math.Abs(row - FirstVisibleRow) * 2 < VisibleRowCount)
            {
                _scrollBuffer.Render(this);
                _scrollY = (FirstVisibleRow - row) * Grid.RowHeight;
                if (row > FirstVisibleRow)
                {
                    for (int i = row + VisibleRowCount; i >= FirstVisibleRow + VisibleRowCount - 1; i--)
                    {
                        _invalidatedRows.Add(i);
                    }
                }
                else
                {
                    for (int i = row; i <= FirstVisibleRow; i++)
                    {
                        _invalidatedRows.Add(i);
                    }
                }
                FirstVisibleRow = row;
                InvalidateVisual();
                return;
            }

            if (column != FirstVisibleColumn && !IsInvalidated && row == FirstVisibleRow
                && Math.Abs(column - FirstVisibleColumn) * 2 < VisibleColumnCount)
            {
                _scrollBuffer.Render(this);
                _scrollX = (FirstVisibleColumn - column) * Grid.ColumnWidth;
                if (column > FirstVisibleColumn)
                {
                    for (int i = column + VisibleColumnCount; i >= FirstVisibleColumn + VisibleColumnCount - 1; i--)
                    {
                        _invalidatedColumns.Add(i);
                    }
                }
                else
                {
                    for (int i = column; i <= FirstVisibleColumn; i++)
                    {
                        _invalidatedColumns.Add(i);
                    }
                }
                FirstVisibleColumn = column;
                InvalidateVisual();
                return;
            }


            // render all
            ClearInvalidation();
            FirstVisibleRow = row;
            FirstVisibleColumn = column;
            InvalidateVisual();
        }
    }
}
