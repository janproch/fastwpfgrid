using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
            var flipVerticalImg = GridModelFunctions.PathFromOutputDir("flip_vertical_small.png", "Images");
            var flipHorizontalImg = GridModelFunctions.PathFromOutputDir("flip_horizontal_small.png", "Images");
            var primaryKeyImg = GridModelFunctions.PathFromOutputDir("primary_keysmall.png", "Images");
            var foreignKeyImg = GridModelFunctions.PathFromOutputDir("foreign_keysmall.png", "Images");

            var impl = new FastGridCellImpl();

            var btn = impl.AddImageBlock(
                view.IsTransposed ?
                    flipHorizontalImg :
                    flipVerticalImg);

            btn.CommandParameter = FastWpfGrid.FastGridControl.ToggleTransposedCommand;
            btn.ToolTip = "Swap rows and columns";
            impl.AddImageBlock(foreignKeyImg).CommandParameter = "FK";
            impl.AddImageBlock(primaryKeyImg).CommandParameter = "PK";
            return impl;
        }

        public override void HandleCommand(IFastGridView view, FastGridCellAddress address, object commandParameter, ref bool handled)
        {
            base.HandleCommand(view, address, commandParameter, ref handled);
            if (commandParameter is string) MessageBox.Show(commandParameter.ToString());
        }

        public override int? SelectedRowCountLimit
        {
            get { return 100; }
        }

        public override void HandleSelectionCommand(IFastGridView view, string command)
        {
            MessageBox.Show(command);
        }
    }
}
