using System.Drawing;
using System.Drawing.Imaging;

namespace MathOCRTeacherPro;

public static class FigureCleaner
{
    public enum CleanMode
    {
        Off,
        Light,
        Strong
    }

    public static Bitmap Clean(Bitmap source, CleanMode mode)
    {
        if (mode == CleanMode.Off)
            return new Bitmap(source);

        var src = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(src))
            g.DrawImage(source, 0, 0, source.Width, source.Height);

        var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);

        // First pass:
        // 1) remove colored pen/highlighter pixels
        // 2) whiten gray paper background
        // 3) preserve dark printed/diagram strokes
        for (int y = 0; y < src.Height; y++)
        {
            for (int x = 0; x < src.Width; x++)
            {
                var c = src.GetPixel(x, y);

                int max = Math.Max(c.R, Math.Max(c.G, c.B));
                int min = Math.Min(c.R, Math.Min(c.G, c.B));
                int chroma = max - min;
                int lum = (int)(0.299 * c.R + 0.587 * c.G + 0.114 * c.B);

                bool coloredInk = chroma > (mode == CleanMode.Strong ? 24 : 34)
                                  && max > 70;

                // Highlighters are usually bright and saturated.
                bool highlighter = chroma > 18 && lum > 155;

                if (coloredInk || highlighter)
                {
                    dst.SetPixel(x, y, Color.White);
                    continue;
                }

                int whiteThreshold = mode == CleanMode.Strong ? 205 : 220;
                int blackThreshold = mode == CleanMode.Strong ? 145 : 120;

                if (lum >= whiteThreshold)
                {
                    dst.SetPixel(x, y, Color.White);
                }
                else if (lum <= blackThreshold)
                {
                    // Preserve actual printed lines/text.
                    int v = mode == CleanMode.Strong ? Math.Max(0, lum - 20) : lum;
                    dst.SetPixel(x, y, Color.FromArgb(v, v, v));
                }
                else
                {
                    // Stretch mid-tones toward white to remove pencil/scan dirt.
                    double t = (lum - blackThreshold) / (double)(whiteThreshold - blackThreshold);
                    int v = (int)(lum + (255 - lum) * t * (mode == CleanMode.Strong ? 0.90 : 0.65));
                    v = Math.Clamp(v, 0, 255);
                    dst.SetPixel(x, y, Color.FromArgb(v, v, v));
                }
            }
        }

        if (mode == CleanMode.Strong)
            RemoveSmallSpeckles(dst);

        src.Dispose();
        return dst;
    }

    private static void RemoveSmallSpeckles(Bitmap bmp)
    {
        // Conservative 3x3 isolated-dark-pixel cleaner.
        // It only removes a pixel when almost all neighbors are white,
        // so graph curves and printed characters are kept.
        var copy = new Bitmap(bmp);
        for (int y = 1; y < bmp.Height - 1; y++)
        {
            for (int x = 1; x < bmp.Width - 1; x++)
            {
                var c = copy.GetPixel(x, y);
                if (c.R > 125) continue;

                int white = 0;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (copy.GetPixel(x + dx, y + dy).R > 235)
                            white++;
                    }
                }

                if (white >= 7)
                    bmp.SetPixel(x, y, Color.White);
            }
        }
        copy.Dispose();
    }
}
