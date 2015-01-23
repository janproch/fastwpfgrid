using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastWpfGrid;

namespace FastWpfGridTest
{
    public class GridModel1 : FastGridModelBase
    {
        public override int ColumnCount
        {
            get { return 100; }
        }

        public override int RowCount
        {
            get { return 1000; }
        }

        //public override string GetCellText(int row, int column)
        //{
        //    return String.Format("{0},{1}", row + 1, column + 1);
        //}
    }
}
