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

                switch (cell.Decoration)
                {
                    case CellDecoration.StrikeOutHorizontal:
                        _drawBuffer.DrawLine(rect.Left, rect.Top + rect.Height/2, rect.Right, rect.Top + rect.Height/2, cell.DecorationColor ?? Colors.Black);
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
    }
}
