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
        private Dictionary<Tuple<int, int>, string> _editedCells = new Dictionary<Tuple<int, int>, string>();
        private static string[] _columnBasicNames = new[] { "", "Value:", "Long column value:" };

        public override int ColumnCount
        {
            get { return 100; }
        }

        public override int RowCount
        {
            get { return 1000; }
        }

        public override string GetCellText(int row, int column)
        {
            var key = Tuple.Create(row, column);
            if (_editedCells.ContainsKey(key)) return _editedCells[key];


            return String.Format("{0}{1},{2}", _columnBasicNames[column % _columnBasicNames.Length], row + 1, column + 1);
        }

        public override void SetCellText(int row, int column, string value)
        {
            var key = Tuple.Create(row, column);
            _editedCells[key] = value;
        }

        public override IFastGridCell GetGridHeader(IFastGridView view)
        {
            var impl = new FastGridCellImpl();
            var btn = impl.AddImageBlock(view.IsTransposed ? "/Images/flip_horizontal_small.png" : "/Images/flip_vertical_small.png");
            btn.CommandParameter = FastWpfGrid.FastGridControl.ToggleTransposedCommand;
            btn.Tooltip = "Swap rows and columns";
            return impl;
        }
    }
}
