using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FastWpfGrid
{
    public abstract class FastGridModelBase : IFastGridModel, IFastGridCell, IFastGridCellBlock
    {
        private List<IFastGridView> _grids = new List<IFastGridView>();
        private int? _requestedRow;
        private int? _requestedColumn;

        public abstract int ColumnCount { get; }

        public abstract int RowCount { get; }

        public virtual string GetCellText(int row, int column)
        {
            return String.Format("Row={0}, Column={1}", row + 1, column + 1);
        }

        public virtual void SetCellText(int row, int column, string value)
        {
        }

        public virtual IFastGridCell GetCell(int row, int column)
        {
            _requestedRow = row;
            _requestedColumn = column;
            return this;
        }

        public virtual string GetRowHeaderText(int row)
        {
            return (row + 1).ToString();
        }

        public virtual IFastGridCell GetRowHeader(int row)
        {
            _requestedRow = row;
            _requestedColumn = null;
            return this;
        }

        public virtual IFastGridCell GetColumnHeader(int column)
        {
            _requestedColumn = column;
            _requestedRow = null;
            return this;
        }

        public virtual string GetColumnHeaderText(int column)
        {
            return "Column " + (column + 1).ToString();
        }

        public virtual void AttachView(IFastGridView view)
        {
            _grids.Add(view);
        }

        public virtual void DetachView(IFastGridView view)
        {
            _grids.Remove(view);
        }

        public void InvalidateAll()
        {
            _grids.ForEach(x => x.InvalidateAll());
        }

        public void InvalidateCell(int row, int column)
        {
            _grids.ForEach(x => x.InvalidateCell(row, column));
        }

        public void InvalidateRowHeader(int row)
        {
            _grids.ForEach(x => x.InvalidateRowHeader(row));
        }

        public void InvalidateColumnHeader(int column)
        {
            _grids.ForEach(x => x.InvalidateColumnHeader(column));
        }

        public void NotifyAddedRows()
        {
            _grids.ForEach(x => x.NotifyAddedRows());
        }

        public virtual Color? BackgroundColor
        {
            get { return null; }
        }

        public virtual int BlockCount
        {
            get { return 1; }
        }

        public virtual int RightAlignBlockCount
        {
            get { return 0; }
        }

        public virtual  IFastGridCellBlock GetBlock(int blockIndex)
        {
            return this;
        }

        public virtual string GetEditText()
        {
            return GetCellText(_requestedRow.Value, _requestedColumn.Value);
        }

        public virtual void SetEditText(string value)
        {
            SetCellText(_requestedRow.Value, _requestedColumn.Value, value);
        }

        public virtual FastGridBlockType BlockType
        {
            get { return FastGridBlockType.Text; }
        }

        public virtual bool IsItalic
        {
            get { return false; }
        }

        public virtual bool IsBold
        {
            get { return false; }
        }

        public virtual Color? FontColor
        {
            get { return null; }
        }

        public virtual string TextData
        {
            get
            {
                if (_requestedColumn == null && _requestedRow == null) return null;
                if (_requestedColumn != null && _requestedRow != null) return GetCellText(_requestedRow.Value, _requestedColumn.Value);
                if (_requestedColumn != null) return GetColumnHeaderText(_requestedColumn.Value);
                if (_requestedRow != null) return GetRowHeaderText(_requestedRow.Value);
                return null;
            }
        }

        public virtual string ImageSource
        {
            get { return null; }
        }

        public virtual int ImageWidth
        {
            get { return 16; }
        }

        public virtual int ImageHeight
        {
            get { return 16; }
        }
    }
}
