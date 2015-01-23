using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastWpfGrid
{
    public abstract class FastGridModelBase : IFastGridModel
    {
        private List<IFastGridView> _grids = new List<IFastGridView>();

        public abstract int ColumnCount { get; }

        public abstract int RowCount { get; }

        public virtual string GetCellText(int row, int column)
        {
            return String.Format("Row={0}, Column={1}", row + 1, column + 1);
        }

        public virtual string GetRowHeader(int row)
        {
            return (row + 1).ToString();
        }

        public virtual string GetColumnHeader(int column)
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
    }
}
