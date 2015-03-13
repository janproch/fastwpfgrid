using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using FastWpfGrid;

namespace FastWpfGridTest
{
    public class GridModel3 : FastGridModelBase
    {
        public override int ColumnCount
        {
            get { return 3; }
        }

        public override int RowCount
        {
            get { return 1000; }
        }

        public override IFastGridCell GetCell(IFastGridView view, int row, int column)
        {
            if (column != 2) return base.GetCell(view, row, column);

            var cell = new FastGridCellImpl();
            var lines = new List<string>();
            for (int i = 0; i <= row%5; i++)
            {
                lines.Add(String.Format("Line {0}", i));
            }

            if (view.FlexibleRows) cell.AddTextBlock(String.Join("\n", lines));
            else cell.AddTextBlock(String.Format("({0} lines)", lines.Count)).FontColor = Colors.Gray;
            return cell;
        }
    }
}
