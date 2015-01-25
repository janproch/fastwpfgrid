using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastWpfGrid
{
    public struct FastGridCellAddress
    {
        public int? Row;
        public int? Column;

        public bool IsCell
        {
            get { return Row.HasValue && Column.HasValue; }
        }

        public bool IsRowHeader
        {
            get { return Row.HasValue && !Column.HasValue; }
        }

        public bool IsColumnHeader
        {
            get { return Column.HasValue && !Row.HasValue; }
        }

        public bool IsCell00
        {
            get { return !Row.HasValue && !Column.HasValue; }
        }

        public bool TestCell(int row, int col)
        {
            return row == Row && col == Column;
        }
    }
}
