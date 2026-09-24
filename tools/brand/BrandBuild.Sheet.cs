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

    // Contact sheet: the mark at five sizes on BOTH themes plus the icons, so the
    // keying quality and the icon look can be judged by eye in one image.
    static void BuildPreview(Bitmap mark, string path)
    {
        int W = 1000, H = 620, band = H / 2;
        Bitmap b = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(b))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (SolidBrush lb = new SolidBrush(Color.FromArgb(255, 245, 243, 255))) g.FillRectangle(lb, 0, 0, W, band);
            using (SolidBrush db = new SolidBrush(Color.FromArgb(255, 10, 11, 20))) g.FillRectangle(db, 0, band, W, band);

            using (Font f = new Font("Segoe UI", 9f))
            using (SolidBrush inkL = new SolidBrush(Color.FromArgb(255, 30, 41, 59)))
            using (SolidBrush inkD = new SolidBrush(Color.FromArgb(255, 226, 232, 240)))
            {
                int[] sizes = new int[] { 160, 128, 64, 40, 24 };
                string[] icons = new string[] { "icon-192.png", "icon-maskable-512.png", "favicon-32.png" };

                for (int bi = 0; bi < 2; bi++)
                {
                    int top = bi * band;
                    int cy = top + 110;
                    int labelY = top + 215;
                    string cap = bi == 0 ? "LIGHT THEME  bg #F5F3FF" : "DARK THEME  bg #0A0B14";
                    g.DrawString(cap, f, bi == 0 ? inkL : inkD, 24, top + 16);

                    int x = 24;
                    foreach (int s in sizes)
                    {
                        using (Bitmap m = Resize(mark, s, s))
                        {
                            g.DrawImage(m, x, cy - s / 2, s, s);
                        }
                        g.DrawString(s + "px", f, bi == 0 ? inkL : inkD, x, labelY);
                        x += s + 26;
                    }

                    int ix = 706;
                    foreach (string ic in icons)
                    {
                        using (Bitmap im = new Bitmap(Path.Combine(Out, ic)))
                        using (Bitmap rs = Resize(im, 80, 80))
                        {
                            g.DrawImage(rs, ix, cy - 40, 80, 80);
                        }
                        g.DrawString(ic.Replace(".png", ""), f, bi == 0 ? inkL : inkD, ix, labelY);
                        ix += 96;
                    }
                }

                using (Pen p = new Pen(Color.FromArgb(60, 128, 128, 128)))
                    g.DrawLine(p, 0, band, W, band);
            }
        }
        b.Save(path, ImageFormat.Png);
        b.Dispose();
        Log("wrote preview.png " + new FileInfo(path).Length + " bytes");
    }
}
