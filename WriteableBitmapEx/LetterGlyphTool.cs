using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace System.Windows.Media.Imaging
{
    public static class LetterGlyphTool
    {
        public static Dictionary<Tuple<Typeface, double>, GlyphFont> FontCache = new Dictionary<Tuple<Typeface, double>, GlyphFont>();

        public static unsafe void DrawLetter(this WriteableBitmap bmp, int x0, int y0, IntRect cliprect, Color fontColor, LetterGlyph glyph)
        {
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

                fixed (LetterGlyph.Item* items = glyph.Items)
                {
                    int itemCount = glyph.Items.Length;
                    LetterGlyph.Item* currentItem = items;
                    for (int i = 0; i < itemCount; i++, currentItem++)
                    {
                        int x = x0 + currentItem->X;
                        int y = y0 + currentItem->Y;
                        int alpha = currentItem->Alpha;
                        if (x < xmin || y < ymin && x > xmax || y > ymax) continue;

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

        public static int DrawString(this WriteableBitmap bmp, int x0, int y0, IntRect cliprect, Color fontColor, GlyphFont font, string text)
        {
            int dx = 0;
            foreach (char ch in text)
            {
                if (x0 + dx > cliprect.Right) break;
                var letter = font.GetLetter(ch);
                if (letter == null) continue;
                bmp.DrawLetter(x0 + dx, y0, cliprect, fontColor, letter);
                dx += letter.Width;
            }
            return dx;
        }

        public static int DrawString(this WriteableBitmap bmp, int x0, int y0, IntRect cliprect, Color fontColor, Typeface typeface, double emsize, string text)
        {
            var font = GetFont(typeface, emsize);
            return bmp.DrawString(x0, y0, cliprect, fontColor, font, text);
        }

        public static GlyphFont GetFont(Typeface typeface, double emsize)
        {
            var key = Tuple.Create(typeface, emsize);
            lock (FontCache)
            {
                if (FontCache.ContainsKey(key)) return FontCache[key];
            }
            var font = new GlyphFont
                {
                    Typeface = typeface,
                    EmSize = emsize,
                };
            typeface.TryGetGlyphTypeface(out font.GlyphTypeface);
            lock (FontCache)
            {
                FontCache[key] = font;
            }
            return font;
        }
    }

    public class GlyphFont
    {
        public Dictionary<char, LetterGlyph> Glyphs = new Dictionary<char, LetterGlyph>();
        public Typeface Typeface;
        public double EmSize;
        public GlyphTypeface GlyphTypeface;

        public LetterGlyph GetLetter(char ch)
        {
            lock (Glyphs)
            {
                if (!Glyphs.ContainsKey(ch))
                {
                    Glyphs[ch] = LetterGlyph.CreateGlyph(Typeface, EmSize, ch);
                }
                return Glyphs[ch];
            }
        }

        public int GetTextWidth(string text)
        {
            var res = 0;
            foreach (var ch in text)
            {
                var letter = GetLetter(ch);
                if (letter == null) continue;
                res += letter.Width;
            }
            return res;
        }

        public int TextHeight
        {
            get { return (int)Math.Ceiling(GlyphTypeface.Height*EmSize); }
        }
    }
}
