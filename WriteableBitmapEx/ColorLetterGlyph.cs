using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace System.Windows.Media.Imaging
{
    public class ColorLetterGlyph
    {
        public struct Item
        {
            public short X;
            public short Y;
            public int Color;
        }

        public char Ch;
        public int Width;
        public int Height;

        public Item[] Items;


        public static ColorLetterGlyph CreateSpaceGluph(GlyphTypeface glyphTypeface, double size)
        {
            int spaceWidth = (int) Math.Ceiling(glyphTypeface.AdvanceWidths[glyphTypeface.CharacterToGlyphMap[' ']]*size);
            return new ColorLetterGlyph
                {
                    Ch = ' ',
                    Height = (int) Math.Ceiling(glyphTypeface.Height*size),
                    Width = spaceWidth,
                };
        }

        public static unsafe ColorLetterGlyph CreateGlyph(Typeface typeface, GlyphTypeface glyphTypeface, double size, char ch, Color fontColor, Color bgColor)
        {
            if (ch == ' ') return CreateSpaceGluph(glyphTypeface, size);

            FormattedText text = new FormattedText("" + ch,
                                                   CultureInfo.InvariantCulture,
                                                   FlowDirection.LeftToRight,
                                                   typeface,
                                                   size,
                                                   new SolidColorBrush(fontColor));

            int width = (int) Math.Ceiling(text.Width);
            int height = (int) Math.Ceiling(text.Height);
            if (width == 0 || height == 0) return null;
            int bgColorInt = WriteableBitmapExtensions.ConvertColor(bgColor);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(new SolidColorBrush(bgColor), new Pen(), new Rect(0, 0, width, height));
            drawingContext.DrawText(text, new Point(0, 0));
            //var run=new GlyphRun();
            //drawingContext.DrawGlyphRun(new SolidColorBrush(fontColor), run);
            drawingContext.Close();

            RenderTargetBitmap bmp = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);

            var res = new List<Item>();

            var pixbmp = new WriteableBitmap(bmp);
            using (var ctx = new BitmapContext(pixbmp))
            {
                var pixels = ctx.Pixels;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int color = pixels[y*width + x];

                        if (color != bgColorInt)
                        {
                            res.Add(new Item
                                {
                                    X = (short) x,
                                    Y = (short) y,
                                    Color = color,
                                });
                        }
                    }
                }
            }

            return new ColorLetterGlyph
                {
                    Width = width,
                    Height = height,
                    Ch = ch,
                    Items = res.ToArray(),
                };
        }

    }
}
