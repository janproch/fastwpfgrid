using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FastWpfGrid
{
    partial class FastGridControl
    {
        public int FirstVisibleColumnScrollIndex;
        public int FirstVisibleRowScrollIndex;
        private int _modelRowCount;
        private int _modelColumnCount;
        private int _realRowCount;
        private int _realColumnCount;
        private int _frozenRowCount;
        private int _frozenColumnCount;

        private SeriesSizes _rowSizes = new SeriesSizes();
        private SeriesSizes _columnSizes = new SeriesSizes();

        public int VisibleRowCount
        {
            get { return _rowSizes.GetVisibleScrollCount(FirstVisibleRowScrollIndex, GridScrollAreaHeight); }
        }

        public int VisibleColumnCount
        {
            get { return _columnSizes.GetVisibleScrollCount(FirstVisibleColumnScrollIndex, GridScrollAreaWidth); }
        }

        private int GetRowTop(int row)
        {
            if (row < _rowSizes.FrozenCount) return _rowSizes.GetFrozenPosition(row) + HeaderHeight;
            return _rowSizes.GetSizeSum(FirstVisibleRowScrollIndex, row - _rowSizes.FrozenCount) + HeaderHeight + FrozenHeight;
            //return (row - FirstVisibleRow) * RowHeight + HeaderHeight;
        }

        private int GetColumnLeft(int column)
        {
            if (column < _columnSizes.FrozenCount) return _columnSizes.GetFrozenPosition(column) + HeaderWidth;
            return _columnSizes.GetSizeSum(FirstVisibleColumnScrollIndex, column - _columnSizes.FrozenCount) + HeaderWidth + FrozenWidth;
            //return (column - FirstVisibleColumn) * ColumnWidth + HeaderWidth;
        }

        private IntRect GetCellRect(int row, int column)
        {
            return new IntRect(new IntPoint(GetColumnLeft(column), GetRowTop(row)), new IntSize(_columnSizes.GetSizeByRealIndex(column) + 1, _rowSizes.GetSizeByRealIndex(row) + 1));
        }

        private IntRect GetContentRect(IntRect rect)
        {
            return rect.GrowSymmetrical(-CellPaddingHorizontal, -CellPaddingVertical);
        }

        private IntRect GetRowHeaderRect(int row)
        {
            return new IntRect(new IntPoint(0, GetRowTop(row)), new IntSize(HeaderWidth + 1, _rowSizes.GetSizeByRealIndex(row) + 1));
        }

        private IntRect GetColumnHeaderRect(int column)
        {
            return new IntRect(new IntPoint(GetColumnLeft(column), 0), new IntSize(_columnSizes.GetSizeByRealIndex(column) + 1, HeaderHeight + 1));
        }

        private IntRect GetColumnHeadersScrollRect()
        {
            return new IntRect(new IntPoint(HeaderWidth + FrozenWidth, 0), new IntSize(GridScrollAreaWidth, HeaderHeight + 1));
        }

        private IntRect GetRowHeadersScrollRect()
        {
            return new IntRect(new IntPoint(0, HeaderHeight + FrozenHeight), new IntSize(HeaderWidth + 1, GridScrollAreaHeight));
        }

        private IntRect GetFrozenColumnsRect()
        {
            return new IntRect(new IntPoint(HeaderWidth, HeaderHeight), new IntSize(_columnSizes.FrozenSize + 1, GridScrollAreaHeight));
        }

        private IntRect GetFrozenRowsRect()
        {
            return new IntRect(new IntPoint(HeaderWidth, HeaderHeight), new IntSize(GridScrollAreaHeight, _rowSizes.FrozenSize + 1));
        }

        public Rect GetColumnHeaderRectangle(int column)
        {
            var rect = GetColumnHeaderRect(column).ToRect();
            var pt = image.PointToScreen(rect.TopLeft);
            return new Rect(pt, rect.Size);
        }

        public int? GetResizingColumn(Point pt)
        {
            if (pt.Y > HeaderHeight) return null;

            int frozenWidth = FrozenWidth;
            if ((int) pt.X - HeaderWidth <= frozenWidth + ColumnResizeTheresold)
            {
                if ((int) pt.X - HeaderWidth >= frozenWidth - ColumnResizeTheresold && (int) pt.X - HeaderWidth <= FrozenWidth - ColumnResizeTheresold)
                {
                    return _columnSizes.FrozenCount - 1;
                }
                int index = _columnSizes.GetFrozenIndexOnPosition((int) pt.X - HeaderWidth);
                int begin = _columnSizes.GetPositionByRealIndex(index) + HeaderWidth;
                int end = begin + _columnSizes.GetSizeByRealIndex(index);
                if (pt.X >= begin - ColumnResizeTheresold && pt.X <= begin + ColumnResizeTheresold) return index - 1;
                if (pt.X >= end - ColumnResizeTheresold && pt.X <= end + ColumnResizeTheresold) return index;
            }
            else
            {
                int index = _columnSizes.GetScrollIndexOnPosition((int) pt.X - HeaderWidth - frozenWidth + _columnSizes.GetPositionByScrollIndex(FirstVisibleColumnScrollIndex));
                int begin = _columnSizes.GetPositionByScrollIndex(index) + HeaderWidth + frozenWidth;
                int end = begin + _columnSizes.GetSizeByScrollIndex(index);
                if (pt.X >= begin - ColumnResizeTheresold && pt.X <= begin + ColumnResizeTheresold) return index - 1 + _columnSizes.FrozenCount;
                if (pt.X >= end - ColumnResizeTheresold && pt.X <= end + ColumnResizeTheresold) return index + _columnSizes.FrozenCount;
            }
            return null;
        }

        private int? GetSeriesIndexOnPosition(double position, int headerSize, SeriesSizes series, int firstVisible)
        {
            if (position <= headerSize) return null;
            int frozenSize = series.FrozenSize;
            if (position <= headerSize + frozenSize) return series.GetFrozenIndexOnPosition((int) Math.Round(position - headerSize));
            return series.GetScrollIndexOnPosition(
                (int) Math.Round(position - headerSize - frozenSize) + series.GetPositionByScrollIndex(firstVisible)
                       ) + series.FrozenCount;
        }

        public FastGridCellAddress GetCellAddress(Point pt)
        {
            if (pt.X <= HeaderWidth && pt.Y < HeaderHeight)
            {
                return FastGridCellAddress.GridHeader;
            }
            if (pt.X >= GridScrollAreaWidth + HeaderWidth + FrozenWidth)
            {
                return FastGridCellAddress.Empty;
            }
            if (pt.Y >= GridScrollAreaHeight + HeaderHeight + FrozenHeight)
            {
                return FastGridCellAddress.Empty;
            }

            int? row = GetSeriesIndexOnPosition(pt.Y, HeaderHeight, _rowSizes, FirstVisibleRowScrollIndex);
            int? col = GetSeriesIndexOnPosition(pt.X, HeaderWidth, _columnSizes, FirstVisibleColumnScrollIndex);

            return new FastGridCellAddress(row, col);
        }

        public void ScrollCurrentCellIntoView()
        {
            ScrollIntoView(_currentCell);
        }

        public void ScrollIntoView(FastGridCellAddress cell)
        {
            if (cell.Row.HasValue)
            {
                if (cell.Row.Value >= _rowSizes.FrozenCount)
                {
                    int newRow = _rowSizes.ScrollInView(FirstVisibleRowScrollIndex, cell.Row.Value - _rowSizes.FrozenCount, GridScrollAreaHeight);
                    ScrollContent(newRow, FirstVisibleColumnScrollIndex);
                }
            }

            if (cell.Column.HasValue)
            {
                if (cell.Column.Value >= _columnSizes.FrozenCount)
                {
                    int newColumn = _columnSizes.ScrollInView(FirstVisibleColumnScrollIndex, cell.Column.Value - _columnSizes.FrozenCount, GridScrollAreaWidth);
                    ScrollContent(FirstVisibleRowScrollIndex, newColumn);
                }
            }

            AdjustScrollBarPositions();
        }

        public FastGridCellAddress CurrentCell
        {
            get { return _currentCell; }
            set { MoveCurrentCell(value.Row, value.Column); }
        }

        public int? CurrentRow
        {
            get { return _currentCell.IsCell ? _currentCell.Row : null; }
            set { CurrentCell = new FastGridCellAddress(value, CurrentColumn); }
        }

        public int? CurrentColumn
        {
            get { return _currentCell.IsCell ? _currentCell.Column : null; }
            set { CurrentCell = new FastGridCellAddress(CurrentRow, value); }
        }

        public void NotifyColumnArrangeChanged()
        {
            UpdateSeriesCounts();
            FixCurrentCellAndSetSelectionToCurrentCell();
            AdjustScrollbars();
            SetScrollbarMargin();
            InvalidateAll();
        }

        public void NotifyRowArrangeChanged()
        {
            UpdateSeriesCounts();
            FixCurrentCellAndSetSelectionToCurrentCell();
            AdjustScrollbars();
            SetScrollbarMargin();
            InvalidateAll();
        }

        private void UpdateSeriesCounts()
        {
            _rowSizes.Count = IsTransposed ? _modelColumnCount : _modelRowCount;
            _columnSizes.Count = IsTransposed ? _modelRowCount : _modelColumnCount;

            if (_model != null)
            {
                if (IsTransposed)
                {
                    _columnSizes.SetExtraordinaryIndexes(_model.GetHiddenRows(), _model.GetFrozenRows());
                    _rowSizes.SetExtraordinaryIndexes(_model.GetHiddenColumns(), _model.GetFrozenColumns());
                }
                else
                {
                    _rowSizes.SetExtraordinaryIndexes(_model.GetHiddenRows(), _model.GetFrozenRows());
                    _columnSizes.SetExtraordinaryIndexes(_model.GetHiddenColumns(), _model.GetFrozenColumns());
                }
            }

            _realRowCount = _rowSizes.RealCount;
            _realColumnCount = _columnSizes.RealCount;
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
                Exchange(ref FirstVisibleColumnScrollIndex, ref FirstVisibleRowScrollIndex);
                if (_currentCell.IsCell) _currentCell = new FastGridCellAddress(_currentCell.Column, _currentCell.Row);
                UpdateSeriesCounts();
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
                SetScrollbarMargin();
            }
        }

        public int HeaderWidth
        {
            get { return _headerWidth; }
            set
            {
                _headerWidth = value;
                SetScrollbarMargin();
            }
        }

        private void SetScrollbarMargin()
        {
            vscroll.Margin = new Thickness
                {
                    Top = HeaderHeight + FrozenHeight,
                };
            hscroll.Margin = new Thickness
                {
                    Left = HeaderWidth + FrozenWidth,
                };
        }

        public int FrozenWidth
        {
            get { return _columnSizes.FrozenSize; }
        }

        public int FrozenHeight
        {
            get { return _rowSizes.FrozenSize; }
        }

        private IntRect GetScrollRect()
        {
            return new IntRect(new IntPoint(HeaderWidth + FrozenWidth, HeaderHeight + FrozenHeight), new IntSize(GridScrollAreaWidth, GridScrollAreaHeight));
        }

        private IntRect GetGridHeaderRect()
        {
            return new IntRect(new IntPoint(0, 0), new IntSize(HeaderWidth + 1, HeaderHeight + 1));
        }

        private FastGridCellAddress RealToModel(FastGridCellAddress address)
        {
            if (IsTransposed)
            {
                return new FastGridCellAddress(
                    _currentCell.Column.HasValue ? _columnSizes.RealToModel(_currentCell.Column.Value) : (int?)null,
                    _currentCell.Row.HasValue ? _rowSizes.RealToModel(_currentCell.Row.Value) : (int?)null
                    );
            }
            else
            {
                return new FastGridCellAddress(
                    _currentCell.Row.HasValue ? _rowSizes.RealToModel(_currentCell.Row.Value) : (int?) null,
                    _currentCell.Column.HasValue ? _columnSizes.RealToModel(_currentCell.Column.Value) : (int?) null
                    );

            }
        }
    }
}
