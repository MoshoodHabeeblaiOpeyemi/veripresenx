using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

// VeriPresenX brand builder, part 3/3: app icons and the visual QA contact sheet.
public static partial class BrandBuild
{
    // Full-bleed square icon: navy gradient plate + soft violet glow + the mark.
    // Full-bleed (no self-rounding) is deliberate: Android/iOS apply their own
    // mask, so a pre-rounded icon would get double-cropped inside an adaptive shape.
    static void ComposeIcon(Bitmap mark, int size, float frac, string name)
    {
        Bitmap b = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(b))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (LinearGradientBrush br = new LinearGradientBrush(
                new Rectangle(0, 0, size, size),
                Color.FromArgb(255, 33, 39, 74),
                Color.FromArgb(255, 8, 9, 17),
                135f))
            {
                g.FillRectangle(br, 0, 0, size, size);
            }

            float gr = size * 0.66f;
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddEllipse(new RectangleF((size - gr) / 2f, size * 0.08f, gr, gr));
                using (PathGradientBrush pg = new PathGradientBrush(gp))
                {
                    pg.CenterColor = Color.FromArgb(88, 124, 108, 240);
                    pg.SurroundColors = new Color[] { Color.FromArgb(0, 124, 108, 240) };
                    g.FillPath(pg, gp);
                }
            }

            int ms = (int)Math.Round(size * frac);
            g.DrawImage(mark, new Rectangle((size - ms) / 2, (size - ms) / 2, ms, ms));
        }
        b.SetResolution(96f, 96f);
        string p = Path.Combine(Out, name);
        b.Save(p, ImageFormat.Png);
        b.Dispose();
        Log("wrote " + name + " " + new FileInfo(p).Length + " bytes");
    }

    // Decision sheet: SOLID vs CUTOUT side by side, on BOTH themes. Both variants
    // are built from the same geometry, so the outlines register exactly and the
    // only thing that changes across the columns is what happens to the shield's
    // white interior.
    static void BuildPreview(Bitmap solid, Bitmap cutout, string path)
    {
        int cellW = 380, cellH = 300;
        int W = cellW * 2, H = cellH * 2;
        Bitmap b = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(b))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (Font f = new Font("Segoe UI", 9f))
            using (Font ft = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (SolidBrush inkL = new SolidBrush(Color.FromArgb(255, 30, 41, 59)))
            using (SolidBrush inkD = new SolidBrush(Color.FromArgb(255, 226, 232, 240)))
            using (SolidBrush lbl = new SolidBrush(Color.FromArgb(255, 100, 116, 139)))
            {
                int[] sizes = new int[] { 128, 64, 32, 24 };
                string[] captions = new string[] { "SOLID  (white interior kept)", "CUTOUT  (whites transparent)" };

                for (int row = 0; row < 2; row++)
                {
                    bool dark = row == 1;
                    using (SolidBrush bg = new SolidBrush(dark
                        ? Color.FromArgb(255, 10, 11, 20)
                        : Color.FromArgb(255, 245, 243, 255)))
                    {
                        g.FillRectangle(bg, 0, row * cellH, W, cellH);
                    }

                    for (int col = 0; col < 2; col++)
                    {
                        Bitmap m = col == 0 ? solid : cutout;
                        int ox = col * cellW, oy = row * cellH;
                        g.DrawString((dark ? "DARK   " : "LIGHT   ") + captions[col], ft,
                            dark ? inkD : inkL, ox + 20, oy + 16);

                        int x = ox + 20;
                        foreach (int s in sizes)
                        {
                            using (Bitmap r = Resize(m, s, s))
                            {
                                g.DrawImage(r, x, oy + 76, s, s);
                            }
                            g.DrawString(s + "px", f, lbl, x, oy + 212);
                            x += s + 22;
                        }
                    }
                }

                using (Pen p = new Pen(Color.FromArgb(90, 128, 128, 128)))
                {
                    g.DrawLine(p, cellW, 0, cellW, H);
                    g.DrawLine(p, 0, cellH, W, cellH);
                }
            }
        }
        b.Save(path, ImageFormat.Png);
        b.Dispose();
        Log("wrote preview.png " + new FileInfo(path).Length + " bytes");
    }
}
