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
        string GetCellText(int row, int column);
        string GetRowHeader(int row);
        string GetColumnHeader(int column);
        void AttachView(IFastGridView view);
        void DetachView(IFastGridView view);
    }
}
