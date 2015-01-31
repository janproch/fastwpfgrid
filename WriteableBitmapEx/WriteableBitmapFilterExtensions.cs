#region Header
//
//   Project:           WriteableBitmapEx - WriteableBitmap extensions
//   Description:       Collection of transformation extension methods for the WriteableBitmap class.
//
//   Changed by:        $Author$
//   Changed on:        $Date$
//   Changed in:        $Revision$
//   Project:           $URL$
//   Id:                $Id$
//
//
//   Copyright © 2009-2012 Rene Schulte and WriteableBitmapEx Contributors
//
//   This Software is weak copyleft open source. Please read the License.txt for details.
//
#endregion

using System;

#if NETFX_CORE
namespace Windows.UI.Xaml.Media.Imaging
#else
namespace System.Windows.Media.Imaging
#endif
{
   /// <summary>
   /// Collection of filter / convolution extension methods for the WriteableBitmap class.
   /// </summary>
   public
#if WPF
    unsafe
#endif
 static partial class WriteableBitmapExtensions
   {
      #region Kernels

      ///<summary>
      /// Gaussian blur kernel with the size 5x5
      ///</summary>
      public static int[,] KernelGaussianBlur5x5 = {
                                                       {1,  4,  7,  4, 1},
                                                       {4, 16, 26, 16, 4},
                                                       {7, 26, 41, 26, 7},
                                                       {4, 16, 26, 16, 4},
                                                       {1,  4,  7,  4, 1}
                                                 };

      ///<summary>
      /// Gaussian blur kernel with the size 3x3
      ///</summary>
      public static int[,] KernelGaussianBlur3x3 = {
                                                       {16, 26, 16},
                                                       {26, 41, 26},
                                                       {16, 26, 16}
                                                    };

      ///<summary>
      /// Sharpen kernel with the size 3x3
      ///</summary>
      public static int[,] KernelSharpen3x3 = {
                                                 { 0, -2,  0},
                                                 {-2, 11, -2},
                                                 { 0, -2,  0}
                                              };

      #endregion

      #region Methods

      #region Convolute

      /// <summary>
      /// Creates a new filtered WriteableBitmap.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="kernel">The kernel used for convolution.</param>
      /// <returns>A new WriteableBitmap that is a filtered version of the input.</returns>
      public static WriteableBitmap Convolute(this WriteableBitmap bmp, int[,] kernel)
      {
         var kernelFactorSum = 0;
         foreach (var b in kernel)
         {
            kernelFactorSum += b;
         }
         return bmp.Convolute(kernel, kernelFactorSum, 0);
      }

      /// <summary>
      /// Creates a new filtered WriteableBitmap.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="kernel">The kernel used for convolution.</param>
      /// <param name="kernelFactorSum">The factor used for the kernel summing.</param>
      /// <param name="kernelOffsetSum">The offset used for the kernel summing.</param>
      /// <returns>A new WriteableBitmap that is a filtered version of the input.</returns>
      public static WriteableBitmap Convolute(this WriteableBitmap bmp, int[,] kernel, int kernelFactorSum, int kernelOffsetSum)
      {
         var kh = kernel.GetUpperBound(0) + 1;
         var kw = kernel.GetUpperBound(1) + 1;

         if ((kw & 1) == 0)
         {
            throw new InvalidOperationException("Kernel width must be odd!");
         }
         if ((kh & 1) == 0)
         {
            throw new InvalidOperationException("Kernel height must be odd!");
         }

         using (var srcContext = bmp.GetBitmapContext(ReadWriteMode.ReadOnly))
         {
            var w = srcContext.Width;
            var h = srcContext.Height;
            var result = BitmapFactory.New(w, h);

            using (var resultContext = result.GetBitmapContext())
            {
               var pixels = srcContext.Pixels;
               var resultPixels = resultContext.Pixels;
               var index = 0;
               var kwh = kw >> 1;
               var khh = kh >> 1;

               for (var y = 0; y < h; y++)
               {
                  for (var x = 0; x < w; x++)
                  {
                     var a = 0;
                     var r = 0;
                     var g = 0;
                     var b = 0;

                     for (var kx = -kwh; kx <= kwh; kx++)
                     {
                        var px = kx + x;
                        // Repeat pixels at borders
                        if (px < 0)
                        {
                           px = 0;
                        }
                        else if (px >= w)
                        {
                           px = w - 1;
                        }

                        for (var ky = -khh; ky <= khh; ky++)
                        {
                           var py = ky + y;
                           // Repeat pixels at borders
                           if (py < 0)
                           {
                              py = 0;
                           }
                           else if (py >= h)
                           {
                              py = h - 1;
                           }

                           var col = pixels[py * w + px];
                           var k = kernel[ky + kwh, kx + khh];
                           a += ((col >> 24) & 0x000000FF) * k;
                           r += ((col >> 16) & 0x000000FF) * k;
                           g += ((col >> 8) & 0x000000FF) * k;
                           b += ((col) & 0x000000FF) * k;
                        }
                     }

                     var ta = ((a / kernelFactorSum) + kernelOffsetSum);
                     var tr = ((r / kernelFactorSum) + kernelOffsetSum);
                     var tg = ((g / kernelFactorSum) + kernelOffsetSum);
                     var tb = ((b / kernelFactorSum) + kernelOffsetSum);

                     // Clamp to byte boundaries
                     var ba = (byte)((ta > 255) ? 255 : ((ta < 0) ? 0 : ta));
                     var br = (byte)((tr > 255) ? 255 : ((tr < 0) ? 0 : tr));
                     var bg = (byte)((tg > 255) ? 255 : ((tg < 0) ? 0 : tg));
                     var bb = (byte)((tb > 255) ? 255 : ((tb < 0) ? 0 : tb));

                     resultPixels[index++] = (ba << 24) | (br << 16) | (bg << 8) | (bb);
                  }
               }
               return result;
            }
         }
      }

      #endregion

      #region Invert

      /// <summary>
      /// Creates a new inverted WriteableBitmap and returns it.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <returns>The new inverted WriteableBitmap.</returns>
      public static WriteableBitmap Invert(this WriteableBitmap bmp)
      {
         using (var srcContext = bmp.GetBitmapContext())
         {
            var result = BitmapFactory.New(srcContext.Width, srcContext.Height);
            using (var resultContext = result.GetBitmapContext())
            {
               var rp = resultContext.Pixels;
               var p = srcContext.Pixels;
               var length = srcContext.Length;

               for (var i = 0; i < length; i++)
               {
                  // Extract
                  var c = p[i];
                  var a = (c >> 24) & 0x000000FF;
                  var r = (c >> 16) & 0x000000FF;
                  var g = (c >> 8) & 0x000000FF;
                  var b = (c) & 0x000000FF;

                  // Invert
                  r = 255 - r;
                  g = 255 - g;
                  b = 255 - b;

                  // Set
                  rp[i] = (a << 24) | (r << 16) | (g << 8) | b;
               }

               return result;
            }
         }
      }
      #endregion

      #endregion
   }
}