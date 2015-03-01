using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FastWpfGrid
{
    public class FastGridBlockImpl : IFastGridCellBlock
    {
        public FastGridBlockType BlockType { get; set; }
        public Color? FontColor { get; set; }
        public bool IsItalic { get; set; }
        public bool IsBold { get; set; }
        public string TextData { get; set; }
        public string ImageSource { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
    }

    public class FastGridCellImpl  : IFastGridCell
    {
        public Color? BackgroundColor { get; set; }
        public List<FastGridBlockImpl> Blocks = new List<FastGridBlockImpl>();

        public int BlockCount
        {
            get { return Blocks.Count; }
        }

        public int RightAlignBlockCount { get; set; }

        public IFastGridCellBlock GetBlock(int blockIndex)
        {
            return Blocks[blockIndex];
        }

        public string GetEditText()
        {
            return null;
        }

        public void SetEditText(string value)
        {
        }

        public IEnumerable<FastGridBlockImpl> SetBlocks
        {
            set
            {
                Blocks.Clear();
                Blocks.AddRange(value);
            }
        }
    }
}
