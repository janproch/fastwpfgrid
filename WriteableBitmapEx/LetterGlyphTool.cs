using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace System.Windows.Media.Imaging
{
    public static class LetterGlyphTool
    {
        public static Dictionary<PortableFontDesc, GlyphFont> FontCache = new Dictionary<PortableFontDesc, GlyphFont>();

        public static unsafe void DrawLetter(this WriteableBitmap bmp, int x0, int y0, IntRect cliprect, Color fontColor, GrayScaleLetterGlyph glyph)
        {
            if (glyph.Items == null) return;

            using (var context = bmp.GetBitmapContext())
            {
                // Use refs for faster access (really important!) speeds up a lot!
                int w = context.Width;
                int h = context.Height;
                var pixels = context.Pixels;

                int fr = fontColor.R;
                int fg = fontColor.G;
                int fb = fontColor.B;

                int xmin = cliprect.Left;
                int ymin = cliprect.Top;
                int xmax = cliprect.Right;
                int ymax = cliprect.Bottom;

                if (xmin < 0) xmin = 0;
                if (ymin < 0) ymin = 0;
                if (xmax >= w) xmax = w - 1;
                if (ymax >= h) ymax = h - 1;

                fixed (GrayScaleLetterGlyph.Item* items = glyph.Items)
                {
                    int itemCount = glyph.Items.Length;
                    GrayScaleLetterGlyph.Item* currentItem = items;
                    for (int i = 0; i < itemCount; i++, currentItem++)
                    {
                        int x = x0 + currentItem->X;
                        int y = y0 + currentItem->Y;
                        int alpha = currentItem->Alpha;
                        if (x < xmin || y < ymin || x > xmax || y > ymax) continue;

                        int color = pixels[y*w + x];
                        int r = ((color >> 16) & 0xFF);
                        int g = ((color >> 8) & 0xFF);
                        int b = ((color) & 0xFF);

                        r = (((r << 12) + (fr - r)*alpha) >> 12) & 0xFF;
                        g = (((g << 12) + (fg - g)*alpha) >> 12) & 0xFF;
                        b = (((b << 12) + (fb - b)*alpha) >> 12) & 0xFF;

                        pixels[y*w + x] = (0xFF << 24) | (r << 16) | (g << 8) | (b);
                    }
                }
            }
        }

        public static unsafe void DrawLetter(this WriteableBitmap bmp, int x0, int y0, IntRect cliprect, ClearTypeLetterGlyph glyph)
        {
            if (glyph.Items == null) return;

            using (var context = bmp.GetBitmapContext())
            {
                // Use refs for faster access (really important!) speeds up a lot!
                int w = context.Width;
                int h = context.Height;
                var pixels = context.Pixels;

                int xmin = cliprect.Left;
                int ymin = cliprect.Top;
                int xmax = cliprect.Right;
                int ymax = cliprect.Bottom;

                if (xmin < 0) xmin = 0;
                if (ymin < 0) ymin = 0;
                if (xmax >= w) xmax = w - 1;
                if (ymax >= h) ymax = h - 1;

                fixed (ClearTypeLetterGlyph.Item* items = glyph.Items)
                {
                    int itemCount = glyph.Items.Length;
                    ClearTypeLetterGlyph.Item* currentItem = items;
                    for (int i = 0; i < itemCount; i++, currentItem++)
                    {
                        int x = x0 + currentItem->X;
                        int y = y0 + currentItem->Y;
                        int color = currentItem->Color;
                        if (x < xmin || y < ymin || x > xmax || y > ymax) continue;

                        pixels[y*w + x] = color;
                    }
                }
            }
        }

        public static int DrawString(this WriteableBitmap bmp, int x0, int y0, IntRect cliprect, Color fontColor, GlyphFont font, string text)
        {
            return DrawString(bmp, x0, y0, cliprect, fontColor, null, font, text);
        }

        public static int DrawString(this WriteableBitmap bmp, int x0, int y0, IntRect cliprect, Color fontColor, Color? bgColor, GlyphFont font, string text)
        {
            if (text == null) return 0;
            int dx = 0;
            foreach (char ch in text)
            {
                if (x0 + dx > cliprect.Right) break;
                if (font.IsClearType)
                {
                    if (!bgColor.HasValue) throw new Exception("Clear type fonts must have background specified");
                    var letter = font.GetClearTypeLetter(ch, fontColor, bgColor.Value);
                    if (letter == null) continue;
                    bmp.DrawLetter(x0 + dx, y0, cliprect, letter);
                    dx += letter.Width;
                }
                else
                {
                    var letter = font.GetGrayScaleLetter(ch);
                    if (letter == null) continue;
                    bmp.DrawLetter(x0 + dx, y0, cliprect, fontColor, letter);
                    dx += letter.Width;
                }
            }
            return dx;
        }

        public static int DrawString(this WriteableBitmap bmp, int x0, int y0, IntRect cliprect, Color fontColor, PortableFontDesc typeface, string text)
        {
            var font = GetFont(typeface);
            return bmp.DrawString(x0, y0, cliprect, fontColor, font, text);
        }

        public static int DrawString(this WriteableBitmap bmp, int x0, int y0, Color fontColor, PortableFontDesc typeface, string text)
        {
            var font = GetFont(typeface);
            return bmp.DrawString(x0, y0, new IntRect(new IntPoint(0, 0), new IntSize(bmp.PixelWidth, bmp.PixelHeight)), fontColor, font, text);
        }

        public static int DrawString(this WriteableBitmap bmp, int x0, int y0, Color fontColor, Color? bgColor, PortableFontDesc typeface, string text)
        {
            var font = GetFont(typeface);
            return bmp.DrawString(x0, y0, new IntRect(new IntPoint(0, 0), new IntSize(bmp.PixelWidth, bmp.PixelHeight)), fontColor, bgColor, font, text);
        }

        public static GlyphFont GetFont(PortableFontDesc typeface)
        {
            lock (FontCache)
            {
                if (FontCache.ContainsKey(typeface)) return FontCache[typeface];
            }
            var fontFlags = System.Drawing.FontStyle.Regular;
            if (typeface.IsItalic) fontFlags |= System.Drawing.FontStyle.Italic;
            if (typeface.IsBold) fontFlags |= System.Drawing.FontStyle.Bold;
            var font = new GlyphFont
                {
                    Typeface = new Typeface(new FontFamily(typeface.FontName),
                                            typeface.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                                            typeface.IsBold ? FontWeights.Bold : FontWeights.Normal,
                                            FontStretches.Normal),
                    EmSize = typeface.EmSize,
                    Font = new Font(typeface.FontName, typeface.EmSize*76.0f/92.0f, fontFlags),
                    IsClearType = typeface.IsClearType,
                };
            font.Typeface.TryGetGlyphTypeface(out font.GlyphTypeface);
            lock (FontCache)
            {
                FontCache[typeface] = font;
            }
            return font;
        }
    }

    public class GlyphFont
    {
        public Dictionary<char, GrayScaleLetterGlyph> Glyphs = new Dictionary<char, GrayScaleLetterGlyph>();
        public Dictionary<Tuple<Color, Color, char>, ClearTypeLetterGlyph> ColorGlyphs = new Dictionary<Tuple<Color, Color, char>, ClearTypeLetterGlyph>();
        public Typeface Typeface;
        public double EmSize;
        public GlyphTypeface GlyphTypeface;
        public System.Drawing.Font Font;
        public bool IsClearType;

        public GrayScaleLetterGlyph GetGrayScaleLetter(char ch)
        {
            lock (Glyphs)
            {
                if (!Glyphs.ContainsKey(ch))
                {
                    Glyphs[ch] = GrayScaleLetterGlyph.CreateGlyph(Typeface, GlyphTypeface, EmSize, ch);
                }
                return Glyphs[ch];
            }
        }

        public ClearTypeLetterGlyph GetClearTypeLetter(char ch, Color fontColor, Color bgColor)
        {
            lock (ColorGlyphs)
            {
                var key = Tuple.Create(fontColor, bgColor, ch);
                if (!ColorGlyphs.ContainsKey(key))
                {
                    ColorGlyphs[key] = ClearTypeLetterGlyph.CreateGlyph(GlyphTypeface, Font, EmSize, ch, fontColor, bgColor);
                }
                return ColorGlyphs[key];
            }
        }

        public int GetTextWidth(string text)
        {
            var res = 0;
            if (text == null) return 0;
            foreach (var ch in text)
            {
                if (IsClearType)
                {
                    var letter = GetClearTypeLetter(ch, Colors.Black, Colors.White);
                    if (letter == null) continue;
                    res += letter.Width;
                }
                else
                {
                    var letter = GetGrayScaleLetter(ch);
                    if (letter == null) continue;
                    res += letter.Width;
                }
            }
            return res;
        }

        public int TextHeight
        {
            get { return (int) Math.Ceiling(GlyphTypeface.Height*EmSize); }
        }
    }
}
