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

        public Rect GetColumnHeaderRectangle(int column)
        {
            var rect = GetColumnHeaderRect(column).ToRect();
            var pt = image.PointToScreen(rect.TopLeft);
            return new Rect(pt, rect.Size);
        }

        public int? GetResizingColumn(Point pt)
        {
            if (pt.Y > HeaderHeight) return null;
            int index = _columnSizes.GetIndexOnPosition((int)pt.X - HeaderWidth + _columnSizes.GetPosition(FirstVisibleColumn));
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
                return new FastGridCellAddress(_rowSizes.GetIndexOnPosition((int)pt.Y - HeaderHeight + _rowSizes.GetPosition(FirstVisibleRow)), null);
            }
            if (pt.Y < HeaderHeight)
            {
                return new FastGridCellAddress(null, _columnSizes.GetIndexOnPosition((int)pt.X - HeaderWidth + _columnSizes.GetPosition(FirstVisibleColumn)));
            }
            return new FastGridCellAddress(
                _rowSizes.GetIndexOnPosition((int)pt.Y - HeaderHeight + _rowSizes.GetPosition(FirstVisibleRow)),
                _columnSizes.GetIndexOnPosition((int)pt.X - HeaderWidth + _columnSizes.GetPosition(FirstVisibleColumn)));
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

    }
}
