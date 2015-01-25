using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastWpfGrid
{
    public interface IFastGridModel
    {
        int ColumnCount { get; }
        int RowCount { get; }
        IFastGridCell GetCell(int row, int column);
        IFastGridCell GetRowHeader(int row);
        IFastGridCell GetColumnHeader(int column);
        void AttachView(IFastGridView view);
        void DetachView(IFastGridView view);
    }
}
