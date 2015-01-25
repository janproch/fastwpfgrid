using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FastWpfGrid
{
    public interface IFastGridCell
    {
        Color? BackgroundColor { get; }

        int BlockCount { get; }
        int RightAlignBlockCount { get; }
        IFastGridCellBlock GetBlock(int blockIndex);

        string GetEditText();
        void SetEditText(string value);
    }
}
