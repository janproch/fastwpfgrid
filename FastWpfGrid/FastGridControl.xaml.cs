using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FastWpfGrid
{
    /// <summary>
    /// Interaction logic for FastGridControl.xaml
    /// </summary>
    public partial class FastGridControl : UserControl, IFastGridView
    {
        //private double _lastvscroll;
        private IFastGridModel _model;

        private int _rowCount;
        private int _columnCount;
        //private double[] _columnWidths = new double[0];
        private int _rowHeight = 22;
        private int _columnWidth = 150;
        private Color _gridLineColor = Colors.LightGray;
        private int _cellPadding = 1;
        private Color[] _alternatingColors = new Color[]
            {
                Colors.White, 
                Colors.White, 
                Color.FromRgb(235, 235, 235), 
                Colors.White, 
                Colors.White, 
                Color.FromRgb(235, 245, 255)
            };
        private Brush[] _alternatingBrushes;
        private string _cellFontName = "Arial";
        private double _cellFontSize = 12;

        public FastGridControl()
        {
            InitializeComponent();
            gridCore.Grid = this;
        }

        public string CellFontName
        {
            get { return _cellFontName; }
            set
            {
                _cellFontName = value;
                InvalidateVisual();
            }
        }

        public double CellFontSize
        {
            get { return _cellFontSize; }
            set
            {
                _cellFontSize = value;
                InvalidateVisual();
            }
        }

        public int RowHeight
        {
            get { return _rowHeight; }
        }

        public int ColumnWidth
        {
            get { return _columnWidth; }
        }

        private void ScrollChanged()
        {
            int rowIndex = (int) ((vscroll.Value + _rowHeight/2.0)/_rowHeight);
            int columnIndex = (int) ((hscroll.Value + _columnWidth/2.0)/_columnWidth);
            gridCore.ScrollContent(rowIndex, columnIndex);
        }

        public Brush GetAlternateBackground(int row)
        {
            if (_alternatingBrushes == null)
            {
                _alternatingBrushes = _alternatingColors.Select(x => new SolidColorBrush(x)).ToArray();
            }
            return _alternatingBrushes[row%_alternatingColors.Length];
        }

        private void hscroll_Scroll(object sender, ScrollEventArgs e)
        {
            ScrollChanged();
            //gridCore.FirstVisibleColumn = (int)hscroll.Value;
            //gridCore.InvalidateVisual();
        }

        private void vscroll_Scroll(object sender, ScrollEventArgs e)
        {
            ScrollChanged();
            //gridCore.ScrollContent(0, vscroll.Value - _lastvscroll);
            //_lastvscroll = vscroll.Value;


            //gridCore.FirstVisibleRow = (int)vscroll.Value;
            //gridCore.InvalidateVisual();
        }

        public IFastGridModel Model
        {
            get { return _model; }
            set
            {
                if (_model != null) _model.DetachView(this);
                _model = value;
                if (_model != null) _model.AttachView(this);
                InvalidateAll();
            }
        }

        public Color GridLineColor
        {
            get { return _gridLineColor; }
            set
            {
                _gridLineColor = value;
                InvalidateVisual();
            }
        }

        public Color[] AlternatingColors
        {
            get { return _alternatingColors; }
            set
            {
                if (value.Length < 1) throw new Exception("Invalid value");
                _alternatingColors = value;
                _alternatingBrushes = null;
                InvalidateVisual();
            }
        }

        public int CellPadding
        {
            get { return _cellPadding; }
            set
            {
                _cellPadding = value;
                InvalidateVisual();
            }
        }

        //private void CreateColumnWidths()
        //{
        //    _columnWidths = new double[_columnCount];
        //    for (int i = 0; i < _columnCount; i++)
        //    {
        //        _columnWidths[i] = 200;
        //    }
        //}

        private void AdjustScrollbars()
        {
            hscroll.Minimum = 0;
            hscroll.Maximum = _columnWidth * _columnCount - gridCore.ActualWidth;
            hscroll.ViewportSize = gridCore.ActualWidth;

            vscroll.Minimum = 0;
            vscroll.Maximum = _rowHeight * _rowCount - gridCore.ActualHeight;
            vscroll.ViewportSize = gridCore.ActualHeight;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            AdjustScrollbars();
        }

        public void InvalidateAll()
        {
            _rowCount = 0;
            _columnCount = 0;
            if (_model != null)
            {
                _rowCount = _model.RowCount;
                _columnCount = _model.ColumnCount;
            }
            AdjustScrollbars();
            InvalidateVisual();
        }

        public void InvalidateCell(int row, int column)
        {
            InvalidateAll();
        }

        public void InvalidateRowHeader(int row)
        {
            InvalidateAll();
        }

        public void InvalidateColumnHeader(int column)
        {
            InvalidateAll();
        }

        public void NotifyAddedRows()
        {
            InvalidateAll();
        }
    }
}
