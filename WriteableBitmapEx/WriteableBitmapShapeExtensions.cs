#region Header
//
//   Project:           WriteableBitmapEx - WriteableBitmap extensions
//   Description:       Collection of extension methods for the WriteableBitmap class.
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
   /// Collection of extension methods for the WriteableBitmap class.
   /// </summary>
   public
#if WPF
    unsafe
#endif
 static partial class WriteableBitmapExtensions
   {
      #region Methods

      #region DrawLine

      /// <summary>
      /// Draws a colored line by connecting two points using the Bresenham algorithm.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the start point.</param>
      /// <param name="y1">The y-coordinate of the start point.</param>
      /// <param name="x2">The x-coordinate of the end point.</param>
      /// <param name="y2">The y-coordinate of the end point.</param>
      /// <param name="color">The color for the line.</param>
      public static void DrawLineBresenham(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, Color color)
      {
         var col = ConvertColor(color);
         bmp.DrawLineBresenham(x1, y1, x2, y2, col);
      }

      /// <summary>
      /// Draws a colored line by connecting two points using the Bresenham algorithm.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the start point.</param>
      /// <param name="y1">The y-coordinate of the start point.</param>
      /// <param name="x2">The x-coordinate of the end point.</param>
      /// <param name="y2">The y-coordinate of the end point.</param>
      /// <param name="color">The color for the line.</param>
      public static void DrawLineBresenham(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int color)
      {
         using (var context = bmp.GetBitmapContext())
         {
            // Use refs for faster access (really important!) speeds up a lot!
            int w = context.Width;
            int h = context.Height;
            var pixels = context.Pixels;

            // Distance start and end point
            int dx = x2 - x1;
            int dy = y2 - y1;

            // Determine sign for direction x
            int incx = 0;
            if (dx < 0)
            {
               dx = -dx;
               incx = -1;
            }
            else if (dx > 0)
            {
               incx = 1;
            }

            // Determine sign for direction y
            int incy = 0;
            if (dy < 0)
            {
               dy = -dy;
               incy = -1;
            }
            else if (dy > 0)
            {
               incy = 1;
            }

            // Which gradient is larger
            int pdx, pdy, odx, ody, es, el;
            if (dx > dy)
            {
               pdx = incx;
               pdy = 0;
               odx = incx;
               ody = incy;
               es = dy;
               el = dx;
            }
            else
            {
               pdx = 0;
               pdy = incy;
               odx = incx;
               ody = incy;
               es = dx;
               el = dy;
            }

            // Init start
            int x = x1;
            int y = y1;
            int error = el >> 1;
            if (y < h && y >= 0 && x < w && x >= 0)
            {
               pixels[y * w + x] = color;
            }

            // Walk the line!
            for (int i = 0; i < el; i++)
            {
               // Update error term
               error -= es;

               // Decide which coord to use
               if (error < 0)
               {
                  error += el;
                  x += odx;
                  y += ody;
               }
               else
               {
                  x += pdx;
                  y += pdy;
               }

               // Set pixel
               if (y < h && y >= 0 && x < w && x >= 0)
               {
                  pixels[y * w + x] = color;
               }
            }
         }
      }

      /// <summary>
      /// Draws a colored line by connecting two points using a DDA algorithm (Digital Differential Analyzer).
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the start point.</param>
      /// <param name="y1">The y-coordinate of the start point.</param>
      /// <param name="x2">The x-coordinate of the end point.</param>
      /// <param name="y2">The y-coordinate of the end point.</param>
      /// <param name="color">The color for the line.</param>
      public static void DrawLineDDA(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, Color color)
      {
         var col = ConvertColor(color);
         bmp.DrawLineDDA(x1, y1, x2, y2, col);
      }

      /// <summary>
      /// Draws a colored line by connecting two points using a DDA algorithm (Digital Differential Analyzer).
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the start point.</param>
      /// <param name="y1">The y-coordinate of the start point.</param>
      /// <param name="x2">The x-coordinate of the end point.</param>
      /// <param name="y2">The y-coordinate of the end point.</param>
      /// <param name="color">The color for the line.</param>
      public static void DrawLineDDA(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int color)
      {
         using (var context = bmp.GetBitmapContext())
         {
            // Use refs for faster access (really important!) speeds up a lot!
            int w = context.Width;
            int h = context.Height;
            var pixels = context.Pixels;

            // Distance start and end point
            int dx = x2 - x1;
            int dy = y2 - y1;

            // Determine slope (absoulte value)
            int len = dy >= 0 ? dy : -dy;
            int lenx = dx >= 0 ? dx : -dx;
            if (lenx > len)
            {
               len = lenx;
            }

            // Prevent divison by zero
            if (len != 0)
            {
               // Init steps and start
               float incx = dx / (float)len;
               float incy = dy / (float)len;
               float x = x1;
               float y = y1;

               // Walk the line!
               for (int i = 0; i < len; i++)
               {
                  if (y < h && y >= 0 && x < w && x >= 0)
                  {
                     pixels[(int)y * w + (int)x] = color;
                  }
                  x += incx;
                  y += incy;
               }
            }
         }
      }

      /// <summary>
      /// Draws a colored line by connecting two points using an optimized DDA.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the start point.</param>
      /// <param name="y1">The y-coordinate of the start point.</param>
      /// <param name="x2">The x-coordinate of the end point.</param>
      /// <param name="y2">The y-coordinate of the end point.</param>
      /// <param name="color">The color for the line.</param>
      public static void DrawLine(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, Color color)
      {
         var col = ConvertColor(color);
         bmp.DrawLine(x1, y1, x2, y2, col);
      }

      /// <summary>
      /// Draws a colored line by connecting two points using an optimized DDA.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the start point.</param>
      /// <param name="y1">The y-coordinate of the start point.</param>
      /// <param name="x2">The x-coordinate of the end point.</param>
      /// <param name="y2">The y-coordinate of the end point.</param>
      /// <param name="color">The color for the line.</param>
      public static void DrawLine(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int color)
      {
         using (var context = bmp.GetBitmapContext())
         {
            DrawLine(context, context.Width, context.Height, x1, y1, x2, y2, color);
         }
      }

      /// <summary>
      /// Draws a colored line by connecting two points using an optimized DDA. 
      /// Uses the pixels array and the width directly for best performance.
      /// </summary>
      /// <param name="context">The context containing the pixels as int RGBA value.</param>
      /// <param name="pixelWidth">The width of one scanline in the pixels array.</param>
      /// <param name="pixelHeight">The height of the bitmap.</param>
      /// <param name="x1">The x-coordinate of the start point.</param>
      /// <param name="y1">The y-coordinate of the start point.</param>
      /// <param name="x2">The x-coordinate of the end point.</param>
      /// <param name="y2">The y-coordinate of the end point.</param>
      /// <param name="color">The color for the line.</param>
      public static void DrawLine(BitmapContext context, int pixelWidth, int pixelHeight, int x1, int y1, int x2, int y2, int color)
      {
         var pixels = context.Pixels;

         // Distance start and end point
         int dx = x2 - x1;
         int dy = y2 - y1;

         const int PRECISION_SHIFT = 8;

         // Determine slope (absoulte value)
         int lenX, lenY;
         if (dy >= 0)
         {
            lenY = dy;
         }
         else
         {
            lenY = -dy;
         }

         if (dx >= 0)
         {
            lenX = dx;
         }
         else
         {
            lenX = -dx;
         }

         if (lenX > lenY)
         { // x increases by +/- 1
            if (dx < 0)
            {
               int t = x1;
               x1 = x2;
               x2 = t;
               t = y1;
               y1 = y2;
               y2 = t;
            }

            // Init steps and start
            int incy = (dy << PRECISION_SHIFT) / dx;

            int y1s = y1 << PRECISION_SHIFT;
            int y2s = y2 << PRECISION_SHIFT;
            int hs = pixelHeight << PRECISION_SHIFT;

            if (y1 < y2)
            {
               if (y1 >= pixelHeight || y2 < 0)
               {
                  return;
               }
               if (y1s < 0)
               {
                  if (incy == 0)
                  {
                     return;
                  }
                  int oldy1s = y1s;
                  // Find lowest y1s that is greater or equal than 0.
                  y1s = incy - 1 + ((y1s + 1) % incy);
                  x1 += (y1s - oldy1s) / incy;
               }
               if (y2s >= hs)
               {
                  if (incy != 0)
                  {
                     // Find highest y2s that is less or equal than ws - 1.
                     // y2s = y1s + n * incy. Find n.
                     y2s = hs - 1 - (hs - 1 - y1s) % incy;
                     x2 = x1 + (y2s - y1s) / incy;
                  }
               }
            }
            else
            {
               if (y2 >= pixelHeight || y1 < 0)
               {
                  return;
               }
               if (y1s >= hs)
               {
                  if (incy == 0)
                  {
                     return;
                  }
                  int oldy1s = y1s;
                  // Find highest y1s that is less or equal than ws - 1.
                  // y1s = oldy1s + n * incy. Find n.
                  y1s = hs - 1 + (incy - (hs - 1 - oldy1s) % incy);
                  x1 += (y1s - oldy1s) / incy;
               }
               if (y2s < 0)
               {
                  if (incy != 0)
                  {
                     // Find lowest y2s that is greater or equal than 0.
                     // y2s = y1s + n * incy. Find n.
                     y2s = y1s % incy;
                     x2 = x1 + (y2s - y1s) / incy;
                  }
               }
            }

            if (x1 < 0)
            {
               y1s -= incy * x1;
               x1 = 0;
            }
            if (x2 >= pixelWidth)
            {
               x2 = pixelWidth - 1;
            }

            int ys = y1s;

            // Walk the line!
            int y = ys >> PRECISION_SHIFT;
            int previousY = y;
            int index = x1 + y * pixelWidth;
            int k = incy < 0 ? 1 - pixelWidth : 1 + pixelWidth;
            for (int x = x1; x <= x2; ++x)
            {
               pixels[index] = color;
               ys += incy;
               y = ys >> PRECISION_SHIFT;
               if (y != previousY)
               {
                  previousY = y;
                  index += k;
               }
               else
               {
                  ++index;
               }
            }
         }
         else
         {
            // Prevent divison by zero
            if (lenY == 0)
            {
               return;
            }
            if (dy < 0)
            {
               int t = x1;
               x1 = x2;
               x2 = t;
               t = y1;
               y1 = y2;
               y2 = t;
            }

            // Init steps and start
            int x1s = x1 << PRECISION_SHIFT;
            int x2s = x2 << PRECISION_SHIFT;
            int ws = pixelWidth << PRECISION_SHIFT;

            int incx = (dx << PRECISION_SHIFT) / dy;

            if (x1 < x2)
            {
               if (x1 >= pixelWidth || x2 < 0)
               {
                  return;
               }
               if (x1s < 0)
               {
                  if (incx == 0)
                  {
                     return;
                  }
                  int oldx1s = x1s;
                  // Find lowest x1s that is greater or equal than 0.
                  x1s = incx - 1 + ((x1s + 1) % incx);
                  y1 += (x1s - oldx1s) / incx;
               }
               if (x2s >= ws)
               {
                  if (incx != 0)
                  {
                     // Find highest x2s that is less or equal than ws - 1.
                     // x2s = x1s + n * incx. Find n.
                     x2s = ws - 1 - (ws - 1 - x1s) % incx;
                     y2 = y1 + (x2s - x1s) / incx;
                  }
               }
            }
            else
            {
               if (x2 >= pixelWidth || x1 < 0)
               {
                  return;
               }
               if (x1s >= ws)
               {
                  if (incx == 0)
                  {
                     return;
                  }
                  int oldx1s = x1s;
                  // Find highest x1s that is less or equal than ws - 1.
                  // x1s = oldx1s + n * incx. Find n.
                  x1s = ws - 1 + (incx - (ws - 1 - oldx1s) % incx);
                  y1 += (x1s - oldx1s) / incx;
               }
               if (x2s < 0)
               {
                  if (incx != 0)
                  {
                     // Find lowest x2s that is greater or equal than 0.
                     // x2s = x1s + n * incx. Find n.
                     x2s = x1s % incx;
                     y2 = y1 + (x2s - x1s) / incx;
                  }
               }
            }

            if (y1 < 0)
            {
               x1s -= incx * y1;
               y1 = 0;
            }
            if (y2 >= pixelHeight)
            {
               y2 = pixelHeight - 1;
            }

            int index = x1s + ((y1 * pixelWidth) << PRECISION_SHIFT);

            // Walk the line!
            var inc = (pixelWidth << PRECISION_SHIFT) + incx;
            for (int y = y1; y <= y2; ++y)
            {
               pixels[index >> PRECISION_SHIFT] = color;
               index += inc;
            }
         }
      }

      #endregion

      #region DrawLine Anti-aliased

      /// <summary> 
      /// Draws an anti-aliased line, using an optimized version of Gupta-Sproull algorithm 
      /// From http://nokola.com/blog/post/2010/10/14/Anti-aliased-Lines-And-Optimizing-Code-for-Windows-Phone-7e28093First-Look.aspx
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the start point.</param>
      /// <param name="y1">The y-coordinate of the start point.</param>
      /// <param name="x2">The x-coordinate of the end point.</param>
      /// <param name="y2">The y-coordinate of the end point.</param>
      /// <param name="color">The color for the line.</param>
      /// </summary> 
      public static void DrawLineAa(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, Color color)
      {
         var col = ConvertColor(color);
         bmp.DrawLineAa(x1, y1, x2, y2, col);
      }

      /// <summary> 
      /// Draws an anti-aliased line, using an optimized version of Gupta-Sproull algorithm 
      /// From http://nokola.com/blog/post/2010/10/14/Anti-aliased-Lines-And-Optimizing-Code-for-Windows-Phone-7e28093First-Look.aspx
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the start point.</param>
      /// <param name="y1">The y-coordinate of the start point.</param>
      /// <param name="x2">The x-coordinate of the end point.</param>
      /// <param name="y2">The y-coordinate of the end point.</param>
      /// <param name="color">The color for the line.</param>
      /// </summary> 
      public static void DrawLineAa(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int color)
      {
         using (var context = bmp.GetBitmapContext())
         {
            DrawLineAa(context, context.Width, context.Height, x1, y1, x2, y2, color);
         }
      }

      /// <summary> 
      /// Draws an anti-aliased line, using an optimized version of Gupta-Sproull algorithm 
      /// From http://nokola.com/blog/post/2010/10/14/Anti-aliased-Lines-And-Optimizing-Code-for-Windows-Phone-7e28093First-Look.aspx
      /// <param name="context">The context containing the pixels as int RGBA value.</param>
      /// <param name="pixelWidth">The width of one scanline in the pixels array.</param>
      /// <param name="pixelHeight">The height of the bitmap.</param>
      /// <param name="x1">The x-coordinate of the start point.</param>
      /// <param name="y1">The y-coordinate of the start point.</param>
      /// <param name="x2">The x-coordinate of the end point.</param>
      /// <param name="y2">The y-coordinate of the end point.</param>
      /// <param name="color">The color for the line.</param>
      /// </summary> 
      public static void DrawLineAa(BitmapContext context, int pixelWidth, int pixelHeight, int x1, int y1, int x2, int y2, int color)
      {
         if ((x1 == x2) && (y1 == y2)) return; // edge case causing invDFloat to overflow, found by Shai Rubinshtein

         if (x1 < 1) x1 = 1;
         if (x1 > pixelWidth - 2) x1 = pixelWidth - 2;
         if (y1 < 1) y1 = 1;
         if (y1 > pixelHeight - 2) y1 = pixelHeight - 2;

         if (x2 < 1) x2 = 1;
         if (x2 > pixelWidth - 2) x2 = pixelWidth - 2;
         if (y2 < 1) y2 = 1;
         if (y2 > pixelHeight - 2) y2 = pixelHeight - 2;

         var addr = y1 * pixelWidth + x1;
         var dx = x2 - x1;
         var dy = y2 - y1;

         int du;
         int dv;
         int u;
         int v;
         int uincr;
         int vincr;

         // Extract color
         var a = (color >> 24) & 0xFF;
         var srb = (uint)(color & 0x00FF00FF);
         var sg = (uint)((color >> 8) & 0xFF);

         // By switching to (u,v), we combine all eight octants 
         int adx = dx, ady = dy;
         if (dx < 0) adx = -dx;
         if (dy < 0) ady = -dy;

         if (adx > ady)
         {
            du = adx;
            dv = ady;
            u = x2;
            v = y2;
            uincr = 1;
            vincr = pixelWidth;
            if (dx < 0) uincr = -uincr;
            if (dy < 0) vincr = -vincr;
         }
         else
         {
            du = ady;
            dv = adx;
            u = y2;
            v = x2;
            uincr = pixelWidth;
            vincr = 1;
            if (dy < 0) uincr = -uincr;
            if (dx < 0) vincr = -vincr;
         }

         var uend = u + du;
         var d = (dv << 1) - du;        // Initial value as in Bresenham's 
         var incrS = dv << 1;    // &#916;d for straight increments 
         var incrD = (dv - du) << 1;    // &#916;d for diagonal increments

         var invDFloat = 1.0 / (4.0 * Math.Sqrt(du * du + dv * dv));   // Precomputed inverse denominator 
         var invD2DuFloat = 0.75 - 2.0 * (du * invDFloat);   // Precomputed constant

         const int PRECISION_SHIFT = 10; // result distance should be from 0 to 1 << PRECISION_SHIFT, mapping to a range of 0..1 
         const int PRECISION_MULTIPLIER = 1 << PRECISION_SHIFT;
         var invD = (int)(invDFloat * PRECISION_MULTIPLIER);
         var invD2Du = (int)(invD2DuFloat * PRECISION_MULTIPLIER * a);
         var zeroDot75 = (int)(0.75 * PRECISION_MULTIPLIER * a);

         var invDMulAlpha = invD * a;
         var duMulInvD = du * invDMulAlpha; // used to help optimize twovdu * invD 
         var dMulInvD = d * invDMulAlpha; // used to help optimize twovdu * invD 
         //int twovdu = 0;    // Numerator of distance; starts at 0 
         var twovduMulInvD = 0; // since twovdu == 0 
         var incrSMulInvD = incrS * invDMulAlpha;
         var incrDMulInvD = incrD * invDMulAlpha;

         do
         {
            AlphaBlendNormalOnPremultiplied(context, addr, (zeroDot75 - twovduMulInvD) >> PRECISION_SHIFT, srb, sg);
            AlphaBlendNormalOnPremultiplied(context, addr + vincr, (invD2Du + twovduMulInvD) >> PRECISION_SHIFT, srb, sg);
            AlphaBlendNormalOnPremultiplied(context, addr - vincr, (invD2Du - twovduMulInvD) >> PRECISION_SHIFT, srb, sg);

            if (d < 0)
            {
               // choose straight (u direction) 
               twovduMulInvD = dMulInvD + duMulInvD;
               d += incrS;
               dMulInvD += incrSMulInvD;
            }
            else
            {
               // choose diagonal (u+v direction) 
               twovduMulInvD = dMulInvD - duMulInvD;
               d += incrD;
               dMulInvD += incrDMulInvD;
               v++;
               addr += vincr;
            }
            u++;
            addr += uincr;
         } while (u < uend);
      }

      /// <summary> 
      /// Blends a specific source color on top of a destination premultiplied color 
      /// </summary> 
      /// <param name="context">Array containing destination color</param> 
      /// <param name="index">Index of destination pixel</param> 
      /// <param name="sa">Source alpha (0..255)</param> 
      /// <param name="srb">Source non-premultiplied red and blue component in the format 0x00rr00bb</param> 
      /// <param name="sg">Source green component (0..255)</param> 
      private static void AlphaBlendNormalOnPremultiplied(BitmapContext context, int index, int sa, uint srb, uint sg)
      {
         var pixels = context.Pixels;
         var destPixel = (uint)pixels[index];

         var da = (destPixel >> 24);
         var dg = ((destPixel >> 8) & 0xff);
         var drb = destPixel & 0x00FF00FF;

         // blend with high-quality alpha and lower quality but faster 1-off RGBs 
         pixels[index] = (int)(
            ((sa + ((da * (255 - sa) * 0x8081) >> 23)) << 24) | // aplha 
            (((sg - dg) * sa + (dg << 8)) & 0xFFFFFF00) | // green 
            (((((srb - drb) * sa) >> 8) + drb) & 0x00FF00FF) // red and blue 
         );
      }

      #endregion

      #region Draw Shapes

      #region Polyline, Triangle, Quad

      /// <summary>
      /// Draws a polyline. Add the first point also at the end of the array if the line should be closed.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="points">The points of the polyline in x and y pairs, therefore the array is interpreted as (x1, y1, x2, y2, ..., xn, yn).</param>
      /// <param name="color">The color for the line.</param>
      public static void DrawPolyline(this WriteableBitmap bmp, int[] points, Color color)
      {
         var col = ConvertColor(color);
         bmp.DrawPolyline(points, col);
      }

      /// <summary>
      /// Draws a polyline. Add the first point also at the end of the array if the line should be closed.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="points">The points of the polyline in x and y pairs, therefore the array is interpreted as (x1, y1, x2, y2, ..., xn, yn).</param>
      /// <param name="color">The color for the line.</param>
      public static void DrawPolyline(this WriteableBitmap bmp, int[] points, int color)
      {
         using (var context = bmp.GetBitmapContext())
         {
            // Use refs for faster access (really important!) speeds up a lot!
            var w = context.Width;
            var h = context.Height;
            var x1 = points[0];
            var y1 = points[1];

            for (var i = 2; i < points.Length; i += 2)
            {
               var x2 = points[i];
               var y2 = points[i + 1];

               DrawLine(context, w, h, x1, y1, x2, y2, color);
               x1 = x2;
               y1 = y2;
            }
         }
      }

      /// <summary>
      /// Draws a triangle.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the 1st point.</param>
      /// <param name="y1">The y-coordinate of the 1st point.</param>
      /// <param name="x2">The x-coordinate of the 2nd point.</param>
      /// <param name="y2">The y-coordinate of the 2nd point.</param>
      /// <param name="x3">The x-coordinate of the 3rd point.</param>
      /// <param name="y3">The y-coordinate of the 3rd point.</param>
      /// <param name="color">The color.</param>
      public static void DrawTriangle(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int x3, int y3, Color color)
      {
         var col = ConvertColor(color);
         bmp.DrawTriangle(x1, y1, x2, y2, x3, y3, col);
      }

      /// <summary>
      /// Draws a triangle.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the 1st point.</param>
      /// <param name="y1">The y-coordinate of the 1st point.</param>
      /// <param name="x2">The x-coordinate of the 2nd point.</param>
      /// <param name="y2">The y-coordinate of the 2nd point.</param>
      /// <param name="x3">The x-coordinate of the 3rd point.</param>
      /// <param name="y3">The y-coordinate of the 3rd point.</param>
      /// <param name="color">The color.</param>
      public static void DrawTriangle(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int x3, int y3, int color)
      {
         using (var context = bmp.GetBitmapContext())
         {
            // Use refs for faster access (really important!) speeds up a lot!
            int w = context.Width;
            int h = context.Height;

            DrawLine(context, w, h, x1, y1, x2, y2, color);
            DrawLine(context, w, h, x2, y2, x3, y3, color);
            DrawLine(context, w, h, x3, y3, x1, y1, color);
         }
      }

      /// <summary>
      /// Draws a quad.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the 1st point.</param>
      /// <param name="y1">The y-coordinate of the 1st point.</param>
      /// <param name="x2">The x-coordinate of the 2nd point.</param>
      /// <param name="y2">The y-coordinate of the 2nd point.</param>
      /// <param name="x3">The x-coordinate of the 3rd point.</param>
      /// <param name="y3">The y-coordinate of the 3rd point.</param>
      /// <param name="x4">The x-coordinate of the 4th point.</param>
      /// <param name="y4">The y-coordinate of the 4th point.</param>
      /// <param name="color">The color.</param>
      public static void DrawQuad(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4, Color color)
      {
         var col = ConvertColor(color);
         bmp.DrawQuad(x1, y1, x2, y2, x3, y3, x4, y4, col);
      }

      /// <summary>
      /// Draws a quad.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the 1st point.</param>
      /// <param name="y1">The y-coordinate of the 1st point.</param>
      /// <param name="x2">The x-coordinate of the 2nd point.</param>
      /// <param name="y2">The y-coordinate of the 2nd point.</param>
      /// <param name="x3">The x-coordinate of the 3rd point.</param>
      /// <param name="y3">The y-coordinate of the 3rd point.</param>
      /// <param name="x4">The x-coordinate of the 4th point.</param>
      /// <param name="y4">The y-coordinate of the 4th point.</param>
      /// <param name="color">The color.</param>
      public static void DrawQuad(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4, int color)
      {
         using (var context = bmp.GetBitmapContext())
         {
            // Use refs for faster access (really important!) speeds up a lot!
            int w = context.Width;
            int h = context.Height;

            DrawLine(context, w, h, x1, y1, x2, y2, color);
            DrawLine(context, w, h, x2, y2, x3, y3, color);
            DrawLine(context, w, h, x3, y3, x4, y4, color);
            DrawLine(context, w, h, x4, y4, x1, y1, color);
         }
      }

      #endregion

      #region Rectangle

      /// <summary>
      /// Draws a rectangle.
      /// x2 has to be greater than x1 and y2 has to be greater than y1.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the bounding rectangle's left side.</param>
      /// <param name="y1">The y-coordinate of the bounding rectangle's top side.</param>
      /// <param name="x2">The x-coordinate of the bounding rectangle's right side.</param>
      /// <param name="y2">The y-coordinate of the bounding rectangle's bottom side.</param>
      /// <param name="color">The color.</param>
      public static void DrawRectangle(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, Color color)
      {
         var col = ConvertColor(color);
         bmp.DrawRectangle(x1, y1, x2, y2, col);
      }

      /// <summary>
      /// Draws a rectangle.
      /// x2 has to be greater than x1 and y2 has to be greater than y1.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the bounding rectangle's left side.</param>
      /// <param name="y1">The y-coordinate of the bounding rectangle's top side.</param>
      /// <param name="x2">The x-coordinate of the bounding rectangle's right side.</param>
      /// <param name="y2">The y-coordinate of the bounding rectangle's bottom side.</param>
      /// <param name="color">The color.</param>
      public static void DrawRectangle(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int color)
      {
         using (var context = bmp.GetBitmapContext())
         {
            // Use refs for faster access (really important!) speeds up a lot!
            var w = context.Width;
            var h = context.Height;
            var pixels = context.Pixels;

            // Check boundaries
            if ((x1 < 0 && x2 < 0) || (y1 < 0 && y2 < 0)
             || (x1 >= w && x2 >= w) || (y1 >= h && y2 >= h))
            {
               return;
            }

            // Clamp boundaries
            if (x1 < 0) { x1 = 0; }
            if (y1 < 0) { y1 = 0; }
            if (x2 < 0) { x2 = 0; }
            if (y2 < 0) { y2 = 0; }
            if (x1 >= w) { x1 = w - 1; }
            if (y1 >= h) { y1 = h - 1; }
            if (x2 >= w) { x2 = w - 1; }
            if (y2 >= h) { y2 = h - 1; }

            var startY = y1 * w;
            var endY = y2 * w;

            var offset2 = endY + x1;
            var endOffset = startY + x2;
            var startYPlusX1 = startY + x1;

            // top and bottom horizontal scanlines
            for (var x = startYPlusX1; x <= endOffset; x++)
            {
               pixels[x] = color; // top horizontal line
               pixels[offset2] = color; // bottom horizontal line
               offset2++;
            }

            // offset2 == endY + x2

            // vertical scanlines
            endOffset = startYPlusX1 + w;
            offset2 -= w;

            for (var y = startY + x2 + w; y <= offset2; y += w)
            {
               pixels[y] = color; // right vertical line
               pixels[endOffset] = color; // left vertical line
               endOffset += w;
            }
         }
      }

      #endregion

      #region Ellipse

      /// <summary>
      /// A Fast Bresenham Type Algorithm For Drawing Ellipses http://homepage.smc.edu/kennedy_john/belipse.pdf 
      /// x2 has to be greater than x1 and y2 has to be greater than y1.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the bounding rectangle's left side.</param>
      /// <param name="y1">The y-coordinate of the bounding rectangle's top side.</param>
      /// <param name="x2">The x-coordinate of the bounding rectangle's right side.</param>
      /// <param name="y2">The y-coordinate of the bounding rectangle's bottom side.</param>
      /// <param name="color">The color for the line.</param>
      public static void DrawEllipse(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, Color color)
      {
         var col = ConvertColor(color);
         bmp.DrawEllipse(x1, y1, x2, y2, col);
      }

      /// <summary>
      /// A Fast Bresenham Type Algorithm For Drawing Ellipses http://homepage.smc.edu/kennedy_john/belipse.pdf 
      /// x2 has to be greater than x1 and y2 has to be greater than y1.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x1">The x-coordinate of the bounding rectangle's left side.</param>
      /// <param name="y1">The y-coordinate of the bounding rectangle's top side.</param>
      /// <param name="x2">The x-coordinate of the bounding rectangle's right side.</param>
      /// <param name="y2">The y-coordinate of the bounding rectangle's bottom side.</param>
      /// <param name="color">The color for the line.</param>
      public static void DrawEllipse(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int color)
      {
         // Calc center and radius
         int xr = (x2 - x1) >> 1;
         int yr = (y2 - y1) >> 1;
         int xc = x1 + xr;
         int yc = y1 + yr;
         bmp.DrawEllipseCentered(xc, yc, xr, yr, color);
      }

      /// <summary>
      /// A Fast Bresenham Type Algorithm For Drawing Ellipses http://homepage.smc.edu/kennedy_john/belipse.pdf
      /// Uses a different parameter representation than DrawEllipse().
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="xc">The x-coordinate of the ellipses center.</param>
      /// <param name="yc">The y-coordinate of the ellipses center.</param>
      /// <param name="xr">The radius of the ellipse in x-direction.</param>
      /// <param name="yr">The radius of the ellipse in y-direction.</param>
      /// <param name="color">The color for the line.</param>
      public static void DrawEllipseCentered(this WriteableBitmap bmp, int xc, int yc, int xr, int yr, Color color)
      {
         var col = ConvertColor(color);
         bmp.DrawEllipseCentered(xc, yc, xr, yr, col);
      }

      /// <summary>
      /// A Fast Bresenham Type Algorithm For Drawing Ellipses http://homepage.smc.edu/kennedy_john/belipse.pdf 
      /// Uses a different parameter representation than DrawEllipse().
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="xc">The x-coordinate of the ellipses center.</param>
      /// <param name="yc">The y-coordinate of the ellipses center.</param>
      /// <param name="xr">The radius of the ellipse in x-direction.</param>
      /// <param name="yr">The radius of the ellipse in y-direction.</param>
      /// <param name="color">The color for the line.</param>
      public static void DrawEllipseCentered(this WriteableBitmap bmp, int xc, int yc, int xr, int yr, int color)
      {
         // Use refs for faster access (really important!) speeds up a lot!
         using (var context = bmp.GetBitmapContext())
         {

            var pixels = context.Pixels;
            var w = context.Width;
            var h = context.Height;

            // Avoid endless loop
            if (xr < 1 || yr < 1)
            {
               return;
            }

            // Init vars
            int uh, lh, uy, ly, lx, rx;
            int x = xr;
            int y = 0;
            int xrSqTwo = (xr * xr) << 1;
            int yrSqTwo = (yr * yr) << 1;
            int xChg = yr * yr * (1 - (xr << 1));
            int yChg = xr * xr;
            int err = 0;
            int xStopping = yrSqTwo * xr;
            int yStopping = 0;

            // Draw first set of points counter clockwise where tangent line slope > -1.
            while (xStopping >= yStopping)
            {
               // Draw 4 quadrant points at once
               uy = yc + y;                  // Upper half
               ly = yc - y;                  // Lower half
               if (uy < 0) uy = 0;          // Clip
               if (uy >= h) uy = h - 1;      // ...
               if (ly < 0) ly = 0;
               if (ly >= h) ly = h - 1;
               uh = uy * w;                  // Upper half
               lh = ly * w;                  // Lower half

               rx = xc + x;
               lx = xc - x;
               if (rx < 0) rx = 0;          // Clip
               if (rx >= w) rx = w - 1;      // ...
               if (lx < 0) lx = 0;
               if (lx >= w) lx = w - 1;
               pixels[rx + uh] = color;      // Quadrant I (Actually an octant)
               pixels[lx + uh] = color;      // Quadrant II
               pixels[lx + lh] = color;      // Quadrant III
               pixels[rx + lh] = color;      // Quadrant IV

               y++;
               yStopping += xrSqTwo;
               err += yChg;
               yChg += xrSqTwo;
               if ((xChg + (err << 1)) > 0)
               {
                  x--;
                  xStopping -= yrSqTwo;
                  err += xChg;
                  xChg += yrSqTwo;
               }
            }

            // ReInit vars
            x = 0;
            y = yr;
            uy = yc + y;                  // Upper half
            ly = yc - y;                  // Lower half
            if (uy < 0) uy = 0;          // Clip
            if (uy >= h) uy = h - 1;      // ...
            if (ly < 0) ly = 0;
            if (ly >= h) ly = h - 1;
            uh = uy * w;                  // Upper half
            lh = ly * w;                  // Lower half
            xChg = yr * yr;
            yChg = xr * xr * (1 - (yr << 1));
            err = 0;
            xStopping = 0;
            yStopping = xrSqTwo * yr;

            // Draw second set of points clockwise where tangent line slope < -1.
            while (xStopping <= yStopping)
            {
               // Draw 4 quadrant points at once
               rx = xc + x;
               lx = xc - x;
               if (rx < 0) rx = 0;          // Clip
               if (rx >= w) rx = w - 1;      // ...
               if (lx < 0) lx = 0;
               if (lx >= w) lx = w - 1;
               pixels[rx + uh] = color;      // Quadrant I (Actually an octant)
               pixels[lx + uh] = color;      // Quadrant II
               pixels[lx + lh] = color;      // Quadrant III
               pixels[rx + lh] = color;      // Quadrant IV

               x++;
               xStopping += yrSqTwo;
               err += xChg;
               xChg += yrSqTwo;
               if ((yChg + (err << 1)) > 0)
               {
                  y--;
                  uy = yc + y;                  // Upper half
                  ly = yc - y;                  // Lower half
                  if (uy < 0) uy = 0;          // Clip
                  if (uy >= h) uy = h - 1;      // ...
                  if (ly < 0) ly = 0;
                  if (ly >= h) ly = h - 1;
                  uh = uy * w;                  // Upper half
                  lh = ly * w;                  // Lower half
                  yStopping -= xrSqTwo;
                  err += yChg;
                  yChg += xrSqTwo;
               }
            }
         }
      }

      #endregion

      #endregion

      #endregion
   }
}