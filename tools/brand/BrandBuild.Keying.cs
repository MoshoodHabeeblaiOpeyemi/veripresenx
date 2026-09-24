using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// VeriPresenX brand builder, part 2a: background removal (flood-fill key).
public static partial class BrandBuild
{
    // Segment by CLASSIFICATION + CONNECTIVITY, never by tolerance.
    //
    // Measured off the master: the background -- including the faint blueprint
    // grid lines, which dip to lum 203 against a 244-250 wash -- is always
    // unsaturated (sat <= 25) and light (lum >= 203). The artwork is not: the
    // shield's violet outline jumps from sat 21 to sat 67 inside a single pixel.
    //
    // So classify first, then flood fill from the border across the background
    // class alone. Having no tolerance term is exactly what lets the fill cross
    // the grid lines: a drift-based fill is walled in by them and abandons the
    // white plate behind the shield. Everything the fill cannot reach is the mark
    // -- which is also how the shield's enclosed pale ring survives, sealed in by
    // the violet outline, where a global near-white key would have destroyed it.
    //
    // `cutout` picks the alpha policy. false keeps the enclosed whites opaque, so
    // the shield reads as a solid badge. true punches every non-artwork pixel out,
    // so the white band and the pale interior go transparent and the page
    // background shows through, leaving only the violet/teal artwork.
    static void KeyBackground(Bitmap bmp, bool cutout)
    {
        int w = bmp.Width, h = bmp.Height, n = w * h;
        BitmapData d = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            int stride = d.Stride;
            byte[] buf = new byte[stride * h];
            Marshal.Copy(d.Scan0, buf, 0, buf.Length);

            byte[] R = new byte[n], G = new byte[n], B = new byte[n];
            for (int y = 0; y < h; y++)
            {
                int ro = y * stride, o = y * w;
                for (int x = 0; x < w; x++)
                {
                    int s = ro + x * 4;
                    B[o + x] = buf[s]; G[o + x] = buf[s + 1]; R[o + x] = buf[s + 2];
                }
            }

            const int satArt = 34, lumArt = 165;
            bool[] art = new bool[n];
            int artCount = 0;
            for (int i = 0; i < n; i++)
            {
                if (Sat(R[i], G[i], B[i]) > satArt || Lum(R[i], G[i], B[i]) < lumArt) { art[i] = true; artCount++; }
            }
            Log("artwork px " + artCount + " of " + n);

            bool[] artRaw = (bool[])art.Clone(); // classification before closing
            CloseArt(art, w, h, 10);
            int closedCount = 0;
            for (int i = 0; i < n; i++) { if (art[i]) closedCount++; }
            Log("silhouette after closing r=10: " + closedCount + " px");

            bool[] outside = new bool[n];
            int[] stack = new int[n];
            int sp = 0;
            int sc = 0;
            int[] seeds = new int[2 * w + 2 * h];
            for (int x = 0; x < w; x++) { seeds[sc++] = x; seeds[sc++] = (h - 1) * w + x; }
            for (int y = 0; y < h; y++) { seeds[sc++] = y * w; seeds[sc++] = y * w + w - 1; }
            for (int k = 0; k < sc; k++)
            {
                int i = seeds[k];
                if (art[i] || outside[i]) continue;
                outside[i] = true; stack[sp++] = i;
            }
            while (sp > 0)
            {
                int i = stack[--sp];
                int x = i % w, y = i / w;
                if (x > 0) { int j = i - 1; if (!art[j] && !outside[j]) { outside[j] = true; stack[sp++] = j; } }
                if (x < w - 1) { int j = i + 1; if (!art[j] && !outside[j]) { outside[j] = true; stack[sp++] = j; } }
                if (y > 0) { int j = i - w; if (!art[j] && !outside[j]) { outside[j] = true; stack[sp++] = j; } }
                if (y < h - 1) { int j = i + w; if (!art[j] && !outside[j]) { outside[j] = true; stack[sp++] = j; } }
            }

            byte[] a0 = new byte[n];
            if (cutout)
            {
                int cutOpaque = 0;
                for (int i = 0; i < n; i++) { if (artRaw[i]) { a0[i] = 255; cutOpaque++; } }
                Log("cutout: opaque " + cutOpaque + " px, whites punched out");
            }
            else
            {
                int outCount = 0;
                for (int i = 0; i < n; i++) { if (outside[i]) { a0[i] = 0; outCount++; } else a0[i] = 255; }
                Log("background reached from border " + outCount + " px; opaque " + (n - outCount));
            }

            // Safety net: drop specks. A grid-line fragment in a darker corner of
            // the master could classify as artwork and survive as an opaque fleck.
            int[] comp = new int[n];
            int[] area = new int[4096];
            int cid = 0, maxArea = 0;
            for (int s = 0; s < n; s++)
            {
                if (a0[s] != 255 || comp[s] != 0) continue;
                cid++;
                sp = 0; stack[sp++] = s; comp[s] = cid;
                int acc = 0;
                while (sp > 0)
                {
                    int i = stack[--sp]; acc++;
                    int x = i % w, y = i / w;
                    if (x > 0 && a0[i - 1] == 255 && comp[i - 1] == 0) { comp[i - 1] = cid; stack[sp++] = i - 1; }
                    if (x < w - 1 && a0[i + 1] == 255 && comp[i + 1] == 0) { comp[i + 1] = cid; stack[sp++] = i + 1; }
                    if (y > 0 && a0[i - w] == 255 && comp[i - w] == 0) { comp[i - w] = cid; stack[sp++] = i - w; }
                    if (y < h - 1 && a0[i + w] == 255 && comp[i + w] == 0) { comp[i + w] = cid; stack[sp++] = i + w; }
                }
                if (cid < 4096) area[cid] = acc;
                if (acc > maxArea) maxArea = acc;
            }
            int keepMin = Math.Max(40, maxArea / 200);
            int dropped = 0;
            for (int i = 0; i < n; i++)
            {
                if (a0[i] == 255 && comp[i] < 4096 && area[comp[i]] < keepMin) { a0[i] = 0; dropped++; }
            }
            Log("components " + cid + ", largest " + maxArea + ", dropped " + dropped + " px (keepMin " + keepMin + ")");

            // 1px feather: separable 1-2-1 blur of the alpha channel only.
            byte[] tmp = new byte[n], a1 = new byte[n];
            for (int y = 0; y < h; y++)
            {
                int ro = y * w;
                for (int x = 0; x < w; x++)
                {
                    int acc = a0[ro + x] * 2, wsum = 2;
                    if (x > 0) { acc += a0[ro + x - 1]; wsum++; }
                    if (x < w - 1) { acc += a0[ro + x + 1]; wsum++; }
                    tmp[ro + x] = (byte)((acc + wsum / 2) / wsum);
                }
            }
            for (int y = 0; y < h; y++)
            {
                int ro = y * w;
                for (int x = 0; x < w; x++)
                {
                    int acc = tmp[ro + x] * 2, wsum = 2;
                    if (y > 0) { acc += tmp[ro - w + x]; wsum++; }
                    if (y < h - 1) { acc += tmp[ro + w + x]; wsum++; }
                    a1[ro + x] = (byte)((acc + wsum / 2) / wsum);
                }
            }
            for (int i = 0; i < n; i++) { if (a1[i] < 10) a1[i] = 0; else if (a1[i] > 246) a1[i] = 255; }

            FinishKey(R, G, B, a1, w, h, ref buf, stride);

            Marshal.Copy(buf, 0, d.Scan0, buf.Length);
        }
        finally { bmp.UnlockBits(d); }
    }
}
