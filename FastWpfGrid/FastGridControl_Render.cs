using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FastWpfGrid
{
    partial class FastGridControl
    {
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

                if (ShouldDrawGridHeader())
                {
                    RenderGridHeader();
                }

                // render frozen rows
                for (int row = 0; row < _rowSizes.FrozenCount; row++)
                {
                    for (int col = FirstVisibleColumnScrollIndex + _columnSizes.FrozenCount; col < FirstVisibleColumnScrollIndex + _columnSizes.FrozenCount + colsToRender; col++)
                    {
                        if (!ShouldDrawCell(row, col)) continue;
                        RenderCell(row, col);
                    }
                }

                // render frozen columns
                for (int row = FirstVisibleRowScrollIndex + _rowSizes.FrozenCount; row < FirstVisibleRowScrollIndex + _rowSizes.FrozenCount + rowsToRender; row++)
                {
                    for (int col = 0; col < _columnSizes.FrozenCount; col++)
                    {
                        if (!ShouldDrawCell(row, col)) continue;
                        RenderCell(row, col);
                    }
                }

                // render cells
                for (int row = FirstVisibleRowScrollIndex + _rowSizes.FrozenCount; row < FirstVisibleRowScrollIndex + _rowSizes.FrozenCount + rowsToRender; row++)
                {
                    for (int col = FirstVisibleColumnScrollIndex + _columnSizes.FrozenCount; col < FirstVisibleColumnScrollIndex + _columnSizes.FrozenCount + colsToRender; col++)
                    {
                        if (row < 0 || col < 0 || row >= _realRowCount || col >= _realColumnCount) continue;
                        if (!ShouldDrawCell(row, col)) continue;
                        RenderCell(row, col);
                    }
                }

                // render frozen row headers
                for (int row = 0; row < _rowSizes.FrozenCount; row++)
                {
                    if (!ShouldDrawRowHeader(row)) continue;
                    RenderRowHeader(row);
                }

                // render row headers
                for (int row = FirstVisibleRowScrollIndex + _rowSizes.FrozenCount; row < FirstVisibleRowScrollIndex + _rowSizes.FrozenCount + rowsToRender; row++)
                {
                    if (row < 0 || row >= _realRowCount) continue;
                    if (!ShouldDrawRowHeader(row)) continue;
                    RenderRowHeader(row);
                }

                // render frozen column headers
                for (int col = 0; col < _columnSizes.FrozenCount; col++)
                {
                    if (!ShouldDrawColumnHeader(col)) continue;
                    RenderColumnHeader(col);
                }


                // render column headers
                for (int col = FirstVisibleColumnScrollIndex + _columnSizes.FrozenCount; col < FirstVisibleColumnScrollIndex + _columnSizes.FrozenCount + colsToRender; col++)
                {
                    if (col < 0 || col >= _realColumnCount) continue;
                    if (!ShouldDrawColumnHeader(col)) continue;
                    RenderColumnHeader(col);
                }
            }
            ClearInvalidation();
        }

        private void RenderGridHeader()
        {
            if (Model == null) return;
            var cell = Model.GetGridHeader();
            var rect = GetGridHeaderRect();
            RenderCell(cell, rect, null, HeaderBackground, FastGridCellAddress.Empty);
        }

        private void RenderColumnHeader(int col)
        {
            var cell = GetColumnHeader(col);

            Color? selectedBgColor = null;
            if (col == _currentCell.Column) selectedBgColor = HeaderCurrentBackground;

            var rect = GetColumnHeaderRect(col);
            Color? cellBackground = null;
            if (cell != null) cellBackground = cell.BackgroundColor;

            Color? hoverColor = null;
            if (col == _mouseOverColumnHeader) hoverColor = MouseOverRowColor;

            RenderCell(cell, rect, null, hoverColor ?? selectedBgColor ?? cellBackground ?? HeaderBackground, new FastGridCellAddress(null, col));
        }

        private void RenderRowHeader(int row)
        {
            var cell = GetRowHeader(row);

            Color? selectedBgColor = null;
            if (row == _currentCell.Row) selectedBgColor = HeaderCurrentBackground;

            var rect = GetRowHeaderRect(row);
            Color? cellBackground = null;
            if (cell != null) cellBackground = cell.BackgroundColor;

            Color? hoverColor = null;
            if (row == _mouseOverRowHeader) hoverColor = MouseOverRowColor;

            RenderCell(cell, rect, null, hoverColor ?? selectedBgColor ?? cellBackground ?? HeaderBackground, new FastGridCellAddress(row, null));
        }

        private void RenderCell(int row, int col)
        {
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
                                                      ?? GetAlternateBackground(row),
                                                      new FastGridCellAddress(row, col));
        }

        private int GetCellContentWidth(IFastGridCell cell)
        {
            if (cell == null) return 0;
            int count = cell.BlockCount;

            int witdh = 0;
            for (int i = 0; i < count; i++)
            {
                var block = cell.GetBlock(i);
                if (block == null) continue;
                if (i > 0) witdh += BlockPadding;

                switch (block.BlockType)
                {
                    case FastGridBlockType.Text:
                        string text = block.TextData;
                        var font = GetFont(block.IsBold, block.IsItalic);
                        witdh += font.GetTextWidth(text);
                        break;
                    case FastGridBlockType.Image:
                    case FastGridBlockType.ImageButton:
                        witdh += block.ImageWidth;
                        break;
                }

            }
            return witdh;
        }

        private int RenderBlock(int leftPos, int rightPos, Color? selectedTextColor, Color bgColor, IntRect rectContent, IFastGridCellBlock block, FastGridCellAddress cellAddr, bool leftAlign)
        {
            bool renderBlock = true;
            if (block.ShowOnMouseHover)
            {
                if (cellAddr.IsCell) renderBlock = cellAddr == _mouseOverCell;
                if (cellAddr.IsRowHeader) renderBlock = cellAddr.Row == _mouseOverRowHeader;
                if (cellAddr.IsColumnHeader) renderBlock = cellAddr.Column == _mouseOverColumnHeader;
            }

            switch (block.BlockType)
            {
                case FastGridBlockType.Text:
                    string text = block.TextData;
                    var font = GetFont(block.IsBold, block.IsItalic);
                    int textHeight = font.TextHeight;
                    int textWidth = font.GetTextWidth(text);
                    var textOrigin = new IntPoint(leftAlign ? leftPos : rightPos - textWidth, rectContent.Top + (int) Math.Round(rectContent.Height/2.0 - textHeight/2.0));
                    if (renderBlock)
                    {
                        textWidth = _drawBuffer.DrawString(textOrigin.X, textOrigin.Y, rectContent, selectedTextColor ?? block.FontColor ?? CellFontColor, UseClearType ? bgColor : (Color?) null,
                                                           font,
                                                           text);
                    }
                    return textWidth;
                case FastGridBlockType.Image:
                case FastGridBlockType.ImageButton:
                    var imgOrigin = new IntPoint(leftAlign ? leftPos : rightPos - block.ImageWidth,
                                                 rectContent.Top + (int) Math.Round(rectContent.Height/2.0 - block.ImageHeight/2.0));
                    var wbmp = GetImage(block.ImageSource);
                    if (renderBlock)
                    {
                        _drawBuffer.Blit(new Rect(imgOrigin.X, imgOrigin.Y, block.ImageWidth, block.ImageHeight), wbmp, new Rect(0, 0, block.ImageWidth, block.ImageHeight),
                                         WriteableBitmapExtensions.BlendMode.Alpha);
                    }
                    return block.ImageWidth;
            }
            return 0;
        }

        private void RenderCell(IFastGridCell cell, IntRect rect, Color? selectedTextColor, Color bgColor, FastGridCellAddress cellAddr)
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

            for (int i = count - 1; i >= count - rightCount; i--)
            {
                var block = cell.GetBlock(i);
                if (block == null) continue;
                if (i < count - 1) rightPos -= BlockPadding;
                int blockWi = RenderBlock(leftPos, rightPos, selectedTextColor,bgColor, rectContent, block, cellAddr, false);
                rightPos -= blockWi;
            }

            for (int i = 0; i < leftCount && leftPos < rightPos; i++)
            {
                var block = cell.GetBlock(i);
                if (block == null) continue;
                if (i > 0) leftPos += BlockPadding;
                int blockWi = RenderBlock(leftPos, rightPos, selectedTextColor, bgColor, rectContent, block, cellAddr, true);
                leftPos += blockWi;
            }
            switch (cell.Decoration)
            {
                case CellDecoration.StrikeOutHorizontal:
                    _drawBuffer.DrawLine(rect.Left, rect.Top + rect.Height/2, rect.Right, rect.Top + rect.Height/2, cell.DecorationColor ?? Colors.Black);
                    break;
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

        private void ScrollContent(int row, int column)
        {
            if (row == FirstVisibleRowScrollIndex && column == FirstVisibleColumnScrollIndex)
            {
                return;
            }

            if (row != FirstVisibleRowScrollIndex && !_isInvalidated && column == FirstVisibleColumnScrollIndex
                && Math.Abs(row - FirstVisibleRowScrollIndex) * 2 < VisibleRowCount)
            {
                using (var ctx = CreateInvalidationContext())
                {
                    int scrollY = _rowSizes.GetScroll(FirstVisibleRowScrollIndex, row);
                    _rowSizes.InvalidateAfterScroll(FirstVisibleRowScrollIndex, row, InvalidateRow, GridScrollAreaHeight);
                    FirstVisibleRowScrollIndex = row;

                    _drawBuffer.ScrollY(scrollY, GetScrollRect());
                    _drawBuffer.ScrollY(scrollY, GetRowHeadersScrollRect());
                    if (_columnSizes.FrozenCount > 0) _drawBuffer.ScrollY(scrollY, GetFrozenColumnsRect());
                }
                return;
            }

            if (column != FirstVisibleColumnScrollIndex && !_isInvalidated && row == FirstVisibleRowScrollIndex
                && Math.Abs(column - FirstVisibleColumnScrollIndex) * 2 < VisibleColumnCount)
            {
                using (var ctx = CreateInvalidationContext())
                {
                    int scrollX = _columnSizes.GetScroll(FirstVisibleColumnScrollIndex, column);
                    _columnSizes.InvalidateAfterScroll(FirstVisibleColumnScrollIndex, column, InvalidateColumn, GridScrollAreaWidth);
                    FirstVisibleColumnScrollIndex = column;

                    _drawBuffer.ScrollX(scrollX, GetScrollRect());
                    _drawBuffer.ScrollX(scrollX, GetColumnHeadersScrollRect());
                    if (_rowSizes.FrozenCount > 0) _drawBuffer.ScrollX(scrollX, GetFrozenRowsRect());
                }
                return;
            }


            // render all
            using (var ctx = CreateInvalidationContext())
            {
                FirstVisibleRowScrollIndex = row;
                FirstVisibleColumnScrollIndex = column;
                InvalidateAll();
            }
        }
    }
}
