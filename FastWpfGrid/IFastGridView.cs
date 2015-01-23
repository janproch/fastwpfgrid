using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastWpfGrid
{
    public interface IFastGridView
    {
        void InvalidateAll();
        void InvalidateCell(int row, int column);
        void InvalidateRowHeader(int row);
        void InvalidateColumnHeader(int column);
        void NotifyAddedRows();
    }
}
