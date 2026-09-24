using System;

// VeriPresenX brand builder, part 2b: alpha clean-up after the key.
public static partial class BrandBuild
{
    // De-fringe: semi-transparent edge pixels still hold the light background
    // blend, which would read as a pale halo on the dark theme, so their colour is
    // replaced by the mean of the fully opaque neighbours. Bleed: artwork colour is
    // then spread outward into the transparent margin so that bicubic downscaling
    // interpolates colour-to-colour instead of colour-to-light.
    static void FinishKey(byte[] R, byte[] G, byte[] B, byte[] a1, int w, int h, ref byte[] buf, int stride)
    {
        int n = w * h;
        byte[] R2 = (byte[])R.Clone(), G2 = (byte[])G.Clone(), B2 = (byte[])B.Clone();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                int a = a1[i];
                if (a == 0 || a == 255) continue;
                int sr = 0, sg = 0, sb = 0, cnt = 0;
                for (int dy = -1; dy <= 1; dy++)
                {
                    int yy = y + dy; if (yy < 0 || yy >= h) continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int xx = x + dx; if (xx < 0 || xx >= w) continue;
                        int j = yy * w + xx;
                        if (a1[j] == 255) { sr += R[j]; sg += G[j]; sb += B[j]; cnt++; }
                    }
                }
                if (cnt > 0) { R2[i] = (byte)(sr / cnt); G2[i] = (byte)(sg / cnt); B2[i] = (byte)(sb / cnt); }
            }
        }

        for (int pass = 0; pass < 2; pass++)
        {
            byte[] dR = (byte[])R2.Clone(), dG = (byte[])G2.Clone(), dB = (byte[])B2.Clone();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (a1[i] != 0) continue;
                    int sr = 0, sg = 0, sb = 0, cnt = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int yy = y + dy; if (yy < 0 || yy >= h) continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int xx = x + dx; if (xx < 0 || xx >= w) continue;
                            int j = yy * w + xx;
                            if (a1[j] == 0) continue;
                            sr += dR[j]; sg += dG[j]; sb += dB[j]; cnt++;
                        }
                    }
                    if (cnt > 0) { dR[i] = (byte)(sr / cnt); dG[i] = (byte)(sg / cnt); dB[i] = (byte)(sb / cnt); }
                }
            }
            R2 = dR; G2 = dG; B2 = dB;
        }

        for (int y = 0; y < h; y++)
        {
            int ro = y * stride, o = y * w;
            for (int x = 0; x < w; x++)
            {
                int s = ro + x * 4;
                buf[s] = B2[o + x]; buf[s + 1] = G2[o + x]; buf[s + 2] = R2[o + x]; buf[s + 3] = a1[o + x];
            }
        }
    }

    // Morphological closing: dilate r, then erode r, with a square (separable)
    // structuring element.
    //
    // The shield's inner band is pure white -- measured rgb(255,255,255), sat 0 --
    // against a background whose saturation never exceeds 17. Colour therefore
    // cannot separate them, and the band is reachable from outside through a narrow
    // channel, so a plain flood fill hollows the badge out (correct on the light
    // theme, visibly wrong on dark). Closing seals any indentation narrower than
    // 2r, which seals that channel, while the logo's genuinely open notches are far
    // wider and survive untouched. This yields the silhouette the flood fill should
    // have been bounded by: a solid badge, faithful to the supplied artwork.
    static void CloseArt(bool[] m, int w, int h, int r)
    {
        int n = w * h;
        bool[] a = new bool[n], b = new bool[n];

        for (int y = 0; y < h; y++)
        {
            int ro = y * w;
            for (int x = 0; x < w; x++)
            {
                bool v = false;
                int x0 = Math.Max(0, x - r), x1 = Math.Min(w - 1, x + r);
                for (int k = x0; k <= x1; k++) { if (m[ro + k]) { v = true; break; } }
                a[ro + x] = v;
            }
        }
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool v = false;
                int y0 = Math.Max(0, y - r), y1 = Math.Min(h - 1, y + r);
                for (int k = y0; k <= y1; k++) { if (a[k * w + x]) { v = true; break; } }
                b[y * w + x] = v;
            }
        }
        for (int y = 0; y < h; y++)
        {
            int ro = y * w;
            for (int x = 0; x < w; x++)
            {
                bool v = (x >= r && x + r < w);
                if (v) { for (int k = x - r; k <= x + r; k++) { if (!b[ro + k]) { v = false; break; } } }
                a[ro + x] = v;
            }
        }
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool v = (y >= r && y + r < h);
                if (v) { for (int k = y - r; k <= y + r; k++) { if (!a[k * w + x]) { v = false; break; } } }
                m[y * w + x] = v;
            }
        }
    }
}
