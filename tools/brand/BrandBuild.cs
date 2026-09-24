// VeriPresenX brand asset builder.
// Toolchain: Windows PowerShell 5.1 Add-Type + .NET Framework System.Drawing.
// Deterministic, offline, no npm/ImageMagick. C# 5 compatible (no interpolation, no ?., no tuples).
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public static partial class BrandBuild
{
    static string Out;
    static void Log(string s) { Console.WriteLine("[brand] " + s); }

    public static void Run(string root)
    {
        Out = Path.Combine(root, "brand");
        Directory.CreateDirectory(Out);
        string master = Path.Combine(Out, "veripresenx-master.jpg");
        Log("master exists=" + File.Exists(master));

        Bitmap src = Load32(master);
        Log("master dims " + src.Width + "x" + src.Height);

        Rectangle box = FindMarkBox(src);
        Log("mark box x=" + box.X + " y=" + box.Y + " w=" + box.Width + " h=" + box.Height);

        // Padding must exceed the closing radius (see CloseArt): the erode pass
        // treats out-of-bounds as background, so a tighter crop would let it clip
        // the mark's silhouette against the crop edge.
        int pad = 24;
        Rectangle crop = ClampRect(Rectangle.Inflate(box, pad, pad), src.Width, src.Height);
        Bitmap mark = Crop(src, crop);
        src.Dispose();
        Log("crop " + crop.Width + "x" + crop.Height);

        KeyBackground(mark);
        Rectangle tight = AlphaBox(mark);
        Log("tight x=" + tight.X + " y=" + tight.Y + " w=" + tight.Width + " h=" + tight.Height);
        Bitmap tightBmp = Crop(mark, tight);
        mark.Dispose();

        Bitmap square = PadSquare(tightBmp, 0.09f);
        tightBmp.Dispose();

        Bitmap mark1024 = Resize(square, 1024, 1024);
        SavePng(mark1024, "veripresenx-mark-1024.png");
        Bitmap mark256 = Resize(square, 256, 256);
        SavePng(mark256, "mark-256.png");
        Bitmap mark64 = Resize(mark256, 64, 64);
        SavePng(mark64, "mark-64.png");
        square.Dispose(); mark256.Dispose(); mark64.Dispose();

        ComposeIcon(mark1024, 512, 0.72f, "icon-512.png");
        ComposeIcon(mark1024, 192, 0.72f, "icon-192.png");
        ComposeIcon(mark1024, 512, 0.58f, "icon-maskable-512.png");
        ComposeIcon(mark1024, 180, 0.72f, "apple-touch-icon-180.png");
        ComposeIcon(mark1024, 32, 0.74f, "favicon-32.png");

        BuildPreview(mark1024, Path.Combine(Out, "preview.png"));
        mark1024.Dispose();
        Log("done");
    }

    // ---------- io ----------

    static Bitmap Load32(string path)
    {
        using (Bitmap raw = new Bitmap(path))
        {
            Bitmap b = new Bitmap(raw.Width, raw.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(raw, new Rectangle(0, 0, raw.Width, raw.Height));
            }
            return b;
        }
    }

    static Bitmap Crop(Bitmap src, Rectangle r)
    {
        r = ClampRect(r, src.Width, src.Height);
        Bitmap b = new Bitmap(r.Width, r.Height, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(b))
        {
            g.CompositingMode = CompositingMode.SourceCopy;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(src, new Rectangle(0, 0, r.Width, r.Height), r, GraphicsUnit.Pixel);
        }
        b.SetResolution(96f, 96f);
        return b;
    }

    static Rectangle ClampRect(Rectangle r, int w, int h)
    {
        int x0 = Math.Max(0, r.X), y0 = Math.Max(0, r.Y);
        int x1 = Math.Min(w, r.X + r.Width), y1 = Math.Min(h, r.Y + r.Height);
        return Rectangle.FromLTRB(x0, y0, Math.Max(x0 + 1, x1), Math.Max(y0 + 1, y1));
    }

    static void SavePng(Bitmap b, string name)
    {
        string p = Path.Combine(Out, name);
        b.SetResolution(96f, 96f);
        b.Save(p, ImageFormat.Png);
        FileInfo fi = new FileInfo(p);
        Log("wrote " + name + " " + fi.Length + " bytes");
    }

    static Bitmap Resize(Bitmap src, int w, int h)
    {
        Bitmap b = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(b))
        {
            g.CompositingMode = CompositingMode.SourceCopy;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(src, new Rectangle(0, 0, w, h), new Rectangle(0, 0, src.Width, src.Height), GraphicsUnit.Pixel);
        }
        b.SetResolution(96f, 96f);
        return b;
    }

    // ---------- pixel access ----------

    static void ReadPixels(Bitmap bmp, out byte[] R, out byte[] G, out byte[] B, out byte[] A, out int w, out int h)
    {
        w = bmp.Width; h = bmp.Height;
        int n = w * h;
        R = new byte[n]; G = new byte[n]; B = new byte[n]; A = new byte[n];
        BitmapData d = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            int stride = d.Stride;
            byte[] buf = new byte[stride * h];
            Marshal.Copy(d.Scan0, buf, 0, buf.Length);
            for (int y = 0; y < h; y++)
            {
                int ro = y * stride, o = y * w;
                for (int x = 0; x < w; x++)
                {
                    int s = ro + x * 4;
                    B[o + x] = buf[s]; G[o + x] = buf[s + 1]; R[o + x] = buf[s + 2]; A[o + x] = buf[s + 3];
                }
            }
        }
        finally { bmp.UnlockBits(d); }
    }

    static int Sat(byte r, byte g, byte b)
    {
        int mx = Math.Max(r, Math.Max(g, b));
        int mn = Math.Min(r, Math.Min(g, b));
        return mx - mn;
    }

    static int Lum(byte r, byte g, byte b)
    {
        return (r * 299 + g * 587 + b * 114) / 1000;
    }

    // Locate the shield mark by column projection: the mark is the first dense
    // saturated block reading left-to-right; we stop at the first wide blank gap,
    // which is the space between the shield and the "VeriPresenX" wordmark.
    static Rectangle FindMarkBox(Bitmap bmp)
    {
        byte[] R, G, B, A; int w, h;
        ReadPixels(bmp, out R, out G, out B, out A, out w, out h);
        int n = w * h;
        int[] cols = new int[w];
        for (int i = 0; i < n; i++) if (Sat(R[i], G[i], B[i]) > 60) cols[i % w]++;
        int thresh = Math.Max(3, h / 220);
        int start = -1;
        for (int x = 0; x < w; x++) { if (cols[x] > thresh) { start = x; break; } }
        if (start < 0) throw new InvalidOperationException("FindMarkBox: no saturated mark found");
        int gapMax = Math.Max(8, (int)(w * 0.015));
        int gap = 0, end = start;
        for (int x = start; x < w; x++)
        {
            if (cols[x] > thresh) { end = x; gap = 0; }
            else { gap++; if (gap > gapMax) break; }
        }
        int top = -1, bot = -1;
        for (int y = 0; y < h; y++)
        {
            int ro = y * w; bool hit = false;
            for (int x = start; x <= end; x++)
            {
                int i = ro + x;
                if (Sat(R[i], G[i], B[i]) > 60) { hit = true; break; }
            }
            if (hit) { if (top < 0) top = y; bot = y; }
        }
        return Rectangle.FromLTRB(start, top, end + 1, bot + 1);
    }

    static Rectangle AlphaBox(Bitmap bmp)
    {
        byte[] R, G, B, A; int w, h;
        ReadPixels(bmp, out R, out G, out B, out A, out w, out h);
        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (int y = 0; y < h; y++)
        {
            int ro = y * w;
            for (int x = 0; x < w; x++)
            {
                if (A[ro + x] > 8)
                {
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                }
            }
        }
        if (maxX < 0) throw new InvalidOperationException("AlphaBox: nothing opaque");
        return Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
    }

    static Bitmap PadSquare(Bitmap src, float margin)
    {
        int side = (int)Math.Ceiling(Math.Max(src.Width, src.Height) * (1f + 2f * margin));
        Bitmap b = new Bitmap(side, side, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(b))
        {
            g.CompositingMode = CompositingMode.SourceCopy;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            int x = (side - src.Width) / 2, y = (side - src.Height) / 2;
            g.DrawImage(src, new Rectangle(x, y, src.Width, src.Height));
        }
        b.SetResolution(96f, 96f);
        return b;
    }
}
