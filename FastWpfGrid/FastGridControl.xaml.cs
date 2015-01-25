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
using System.Windows.Threading;

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
        private int _rowHeight;
        private int _columnWidth;
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

        private string _cellFontName = "Arial";
        private double _cellFontSize;
        private int _headerHeight;
        private int _headerWidth;
        private Dictionary<Tuple<bool, bool>, GlyphTypeface> _glyphTypeFaces = new Dictionary<Tuple<bool, bool>, GlyphTypeface>();
        private Dictionary<Color, Brush> _solidBrushes = new Dictionary<Color, Brush>();
        private Color _cellFontColor = Colors.Black;
        private double _rowHeightReserve = 5;
        private Color _headerBackground = Color.FromRgb(0xDD, 0xDD, 0xDD);
        private Color _selectedColor = Color.FromRgb(51, 153, 255);
        private Color _selectedTextColor = Colors.White;
        private Color _mouseOverRowColor = Colors.Beige;

        public FastGridControl()
        {
            InitializeComponent();
            gridCore.Grid = this;
            CellFontSize = 12;
        }

        public int HeaderHeight
        {
            get { return _headerHeight; }
            set
            {
                _headerHeight = value;
                vscroll.Margin = new Thickness
                    {
                        Top = HeaderHeight,
                    };
            }
        }

        public int HeaderWidth
        {
            get { return _headerWidth; }
            set
            {
                _headerWidth = value;
                hscroll.Margin = new Thickness
                    {
                        Left = HeaderWidth,
                    };
            }
        }

        public string CellFontName
        {
            get { return _cellFontName; }
            set
            {
                _cellFontName = value;
                RecalculateDefaultCellSize();
                InvalidateVisual();
            }
        }

        public double CellFontSize
        {
            get { return _cellFontSize; }
            set
            {
                _cellFontSize = value;
                RecalculateDefaultCellSize();
                InvalidateVisual();
            }
        }

        public double RowHeightReserve
        {
            get { return _rowHeightReserve; }
            set
            {
                _rowHeightReserve = value;
                RecalculateDefaultCellSize();
                InvalidateVisual();
            }
        }

        public Color CellFontColor
        {
            get { return _cellFontColor; }
            set
            {
                _cellFontColor = value;
                InvalidateVisual();
            }
        }

        public Color SelectedColor
        {
            get { return _selectedColor; }
            set
            {
                _selectedColor = value;
                InvalidateVisual();
            }
        }

        public Color SelectedTextColor
        {
            get { return _selectedTextColor; }
            set
            {
                _selectedTextColor = value;
                InvalidateVisual();
            }
        }

        public Color MouseOverRowColor
        {
            get { return _mouseOverRowColor; }
            set { _mouseOverRowColor = value; }
        }

        public GlyphTypeface GetGlyphTypeface(bool isBold, bool isItalic)
        {
            var key = Tuple.Create(isBold, isItalic);
            if (!_glyphTypeFaces.ContainsKey(key))
            {
                var typeFace = new Typeface(new FontFamily(CellFontName),
                                            isItalic ? FontStyles.Italic : FontStyles.Normal,
                                            isBold ? FontWeights.Bold : FontWeights.Normal,
                                            FontStretches.Normal);
                GlyphTypeface glyphTypeface;
                if (!typeFace.TryGetGlyphTypeface(out glyphTypeface))
                    throw new InvalidOperationException("No glyphtypeface found");
                _glyphTypeFaces[key] = glyphTypeface;
            }
            return _glyphTypeFaces[key];
        }

        public void ClearCaches()
        {
            _glyphTypeFaces.Clear();
        }

        public int GetTextWidth(string text, bool isBold, bool isItalic)
        {
            double size = CellFontSize;
            int totalWidth = 0;
            var glyphTypeface = GetGlyphTypeface(isBold, isItalic);

            for (int n = 0; n < text.Length; n++)
            {
                ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];
                double width = Math.Round(glyphTypeface.AdvanceWidths[glyphIndex]*size);
                totalWidth += (int) width;
            }
            return totalWidth;
        }

        private void RecalculateDefaultCellSize()
        {
            ClearCaches();
            _rowHeight = (int) (GetGlyphTypeface(false, false).Height*CellFontSize + CellPadding*2 + 2 + RowHeightReserve);
            _columnWidth = _rowHeight*4;
            HeaderWidth = GetTextWidth("0000", false, false);
            HeaderHeight = _rowHeight;
            AdjustScrollbars();
            InvalidateAll();
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

        public Color GetAlternateBackground(int row)
        {
            return _alternatingColors[row%_alternatingColors.Length];
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

        public Color HeaderBackground
        {
            get { return _headerBackground; }
            set
            {
                _headerBackground = value;
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
            hscroll.Maximum = _columnWidth*_columnCount - gridCore.ActualWidth;
            hscroll.ViewportSize = gridCore.ActualWidth;

            vscroll.Minimum = 0;
            vscroll.Maximum = _rowHeight*_rowCount - gridCore.ActualHeight;
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

        public Brush GetSolidBrush(Color color)
        {
            if (!_solidBrushes.ContainsKey(color))
            {
                _solidBrushes[color] = new SolidColorBrush(color);
            }
            return _solidBrushes[color];
        }

        public void ShowTextEditor(Rect rect, string text)
        {
            edText.Margin = new Thickness
                {
                    Left = rect.Left,
                    Top = rect.Top,
                    Right = gridCore.ActualWidth - rect.Right,
                    Bottom = gridCore.ActualHeight - rect.Bottom,
                };
            edText.Text = text;
            edText.Visibility = Visibility.Visible;
            if (edText.IsFocused)
            {
                edText.SelectAll();
            }
            else
            {
                edText.Focus();
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action) edText.SelectAll);
            }
        }
    }
}
