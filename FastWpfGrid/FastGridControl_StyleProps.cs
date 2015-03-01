using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FastWpfGrid
{
    partial class FastGridControl
    {
        private bool _preciseCharacterGlyphs = false;
        private Color _cellFontColor = Colors.Black;
        private Color _headerBackground = Color.FromRgb(0xF6, 0xF7, 0xF9);
        private Color _headerCurrentBackground = Color.FromRgb(190, 207, 220);
        private Color _selectedColor = Color.FromRgb(51, 153, 255);
        private Color _selectedTextColor = Colors.White;
        private Color _mouseOverRowColor = Color.FromRgb(235, 235, 255); // Colors.LemonChiffon; // Colors .Beige;
        private string _cellFontName = "Arial";
        private double _cellFontSize;
        private Color _gridLineColor = Colors.LightGray;
        private int _cellPaddingHorizontal = 2;
        private int _cellPaddingVertical = 1;
        private int _blockPadding = 2;
        private int _columnResizeTheresold = 2;

        private Color[] _alternatingColors = new Color[]
            {
                Colors.White,
                Colors.White,
                Color.FromRgb(235, 235, 235),
                Colors.White,
                Colors.White,
                Color.FromRgb(235, 245, 255)
            };

        public int ColumnResizeTheresold
        {
            get { return _columnResizeTheresold; }
            set { _columnResizeTheresold = value; }
        }

        public string CellFontName
        {
            get { return _cellFontName; }
            set
            {
                _cellFontName = value;
                RecalculateDefaultCellSize();
                RenderChanged();
            }
        }

        public int MinColumnWidth
        {
            get { return _rowSizes.DefaultSize; }
        }

        public double CellFontSize
        {
            get { return _cellFontSize; }
            set
            {
                _cellFontSize = value;
                RecalculateDefaultCellSize();
                RenderGrid();
            }
        }

        public double RowHeightReserve
        {
            get { return _rowHeightReserve; }
            set
            {
                _rowHeightReserve = value;
                RecalculateDefaultCellSize();
                RenderGrid();
            }
        }

        public Color CellFontColor
        {
            get { return _cellFontColor; }
            set
            {
                _cellFontColor = value;
                RenderGrid();
            }
        }

        public Color SelectedColor
        {
            get { return _selectedColor; }
            set
            {
                _selectedColor = value;
                RenderGrid();
            }
        }

        public Color SelectedTextColor
        {
            get { return _selectedTextColor; }
            set
            {
                _selectedTextColor = value;
                RenderGrid();
            }
        }

        public Color MouseOverRowColor
        {
            get { return _mouseOverRowColor; }
            set { _mouseOverRowColor = value; }
        }

        public Color GridLineColor
        {
            get { return _gridLineColor; }
            set
            {
                _gridLineColor = value;
                RenderChanged();
            }
        }

        public Color[] AlternatingColors
        {
            get { return _alternatingColors; }
            set
            {
                if (value.Length < 1) throw new Exception("Invalid value");
                _alternatingColors = value;
                RenderChanged();
            }
        }

        public int CellPaddingHorizontal
        {
            get { return _cellPaddingHorizontal; }
            set
            {
                _cellPaddingHorizontal = value;
                RenderChanged();
            }
        }

        public int CellPaddingVertical
        {
            get { return _cellPaddingVertical; }
            set
            {
                _cellPaddingVertical = value;
                RenderChanged();
            }
        }

        public int BlockPadding
        {
            get { return _blockPadding; }
            set
            {
                _blockPadding = value;
                RenderChanged();
            }
        }

        public Color HeaderBackground
        {
            get { return _headerBackground; }
            set
            {
                _headerBackground = value;
                RenderChanged();
            }
        }

        public Color HeaderCurrentBackground
        {
            get { return _headerCurrentBackground; }
            set
            {
                _headerCurrentBackground = value;
                RenderChanged();
            }
        }
    }
}
