using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FastWpfGrid
{
    public enum FastGridBlockType
    {
        Text,
        Image,
    }

    public interface IFastGridCellBlock
    {
        FastGridBlockType BlockType { get; }

        Color? FontColor { get; }
        bool IsItalic { get; }
        bool IsBold { get; }
        string TextData { get; }

        string ImageSource { get; }
        int ImageWidth { get; }
        int ImageHeight { get; }

        bool ShowOnMouseHover { get; }
        object CommandParameter { get; }
    }
}
