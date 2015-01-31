using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FastWpfGrid
{
    public class FastGridImage : Control
    {
        private WriteableBitmap _drawBuffer;

        public FastGridImage()
        {
        }

        protected override void OnRenderSizeChanged(System.Windows.SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (ActualWidth >= 1 && ActualHeight >= 1)
            {
                _drawBuffer = new WriteableBitmap((int)ActualWidth, (int)ActualHeight, 96.0, 96.0, PixelFormats.Pbgra32, null);
                //_drawBuffer = BitmapFactory.New((int)ActualWidth, (int)ActualHeight);
            }
            else
            {
                _drawBuffer = null;
            }
            //Source = _drawBuffer;

        }
    }
}
