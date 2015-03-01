using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using FastWpfGrid;

namespace FastWpfGridTest
{
    public class GridModel2 : FastGridModelBase
    {
        public override int ColumnCount
        {
            get { return 5; }
        }

        public override int RowCount
        {
            get { return 100; }
        }

        public override IFastGridCell GetColumnHeader(int column)
        {
            if (column==0)
            {
                return new FastGridCellImpl
                {
                    SetBlocks = new[]
                        {
                            new FastGridBlockImpl
                                {
                                    BlockType = FastGridBlockType.Image,
                                    ImageWidth = 16,
                                    ImageHeight = 16,
                                    ImageSource = "/Images/primary_keysmall.png",
                                },

                            new FastGridBlockImpl
                                {
                                    IsBold = true,
                                    TextData = String.Format("Column {0}", column),
                                },
                        }
                };
            }

            return new FastGridCellImpl
                {
                    SetBlocks = new[]
                        {
                            new FastGridBlockImpl
                                {
                                    IsBold = true,
                                    TextData = String.Format("Column {0}", column)
                                }
                        }
                };
        }

        public override IFastGridCell GetCell(int row, int column)
        {
            if ((row+column) % 4 == 0)
            {
                return new FastGridCellImpl
                {
                    SetBlocks = new[]
                        {
                            new FastGridBlockImpl
                                {
                                    IsItalic = true,
                                    FontColor = Colors.Gray,
                                    TextData = "(NULL)",
                                }
                        }
                };
            }
            return base.GetCell(row, column);
        }

        public override string GetCellText(int row, int column)
        {
            return String.Format("{0}{1}", row + 1, column + 1);
        }
    }
}
