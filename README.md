### Project Description
Fast WPF datagrid control designed to work with large datasets, with mandatory data virtualization.

fast DataGrid control for .NET - WPF
- designed for large data sets
- both columns and rows are defined in model
- uses MVVM design pattern, but (for performance reasons) does not use classic WPF binding
- works only with data virtualization (UI virtualization is not needed as in other WPF datagrid controls)
- for rendering is used WriteableBitmapEx library

### Features

- Fast scrolling and rendering
- Excel-like mouse-drag selecting
- Hide columns/rows
- Frozen columns/rows
- Own rendering, WPF templates are not used. Supported objects - text (with - italic, bold attributes), images, image buttons

![grid1](https://raw.githubusercontent.com/dbshell/fastwpfgrid/master/FastWpfGridTest/Images/grid1.png)
![grid2](https://raw.githubusercontent.com/dbshell/fastwpfgrid/master/FastWpfGridTest/Images/grid2.png)

### References

FastWPFGrid is used in DbMouse project ( http:///www.jenasoft.com/dbmouse )

### Model implementation

Grid control is bind to model, which controls displayed data. Below is example of model implementation.
 ```c#
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
            private Dictionary<Tuple<int, int>, string> _editedCells = newDictionary<Tuple<int, int>, string>();
            private static string[] _columnBasicNames = new[] { "","Value:", "Long column value:" };
    
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
                var btn = impl.AddImageBlock (view.IsTransposed ? "/Images/flip_horizontal_small.png" : "/Images/flip_vertical_small.png");
                btn.CommandParameter = FastWpfGrid.FastGridControl. ToggleTransposedCommand;
                btn.ToolTip = "Swap rows and columns";
                return impl;
            }
        }
    }
 ```
