# brand/

Static brand assets for VeriPresenX (app icons, mark variations, and wordmark).

## Files

| File | Size | Where it is used |
| --- | --- | --- |
| `veripresenx-wordmark.png` | 1839×349 | Navbar (wordmark only) & Splash brand wordmark. PNG is keyed from black so edges stay clean; CSS blends it (`screen` on dark / `multiply` on light) to hide any 1px fringe. |
| `mark-256-cutout.png` | 256² | **Splash screen + app icons** — `index.html` `.splash-logo`, displayed at 120 px, so 2× for retina. |
| `mark-64.png`, `mark-256.png` | 64², 256² | SOLID equivalents of the two above, kept so the navbar and splash can be flipped between variants with a one-line change. |
| `veripresenx-mark-cutout-1024.png` | 1024² | Archival mark, whites punched out. Not shipped to browsers. |
| `veripresenx-mark-1024.png` | 1024² | Archival mark, whites kept. Not shipped to browsers. |
| `favicon-32.png` | 32² | Browser tab (`<link rel="icon">`). |
| `apple-touch-icon-180.png` | 180² | iOS home screen. Opaque, because iOS composites transparency to black. |
| `icon-192.png`, `icon-512.png` | 192², 512² | PWA install, `"purpose": "any"`. |
| `icon-maskable-512.png` | 512² | PWA install, `"purpose": "maskable"` — mark at 58% so it survives the safe-zone crop. |
| `preview.png` | 760×600 | SOLID vs CUTOUT, side by side, on both themes, at 128 / 64 / 32 / 24 px. |

## Two variants: SOLID and CUTOUT

The shield's interior is pure white (`rgb(255,255,255)`) in the source, which is the same
colour as the washed background it sits on. Two renders are produced from one geometry:

- **SOLID** keeps that interior opaque, so the mark reads as a badge.
- **CUTOUT** punches every light pixel out, so the interior and the white band go
  transparent and the underlying page shows through.

On the **light** theme the two are all but indistinguishable — a transparent gap shows
`#F5F3FF`, which is what the white was sitting on anyway. On the **dark** theme CUTOUT
becomes an open, neon-like badge where SOLID is a solid one.

The navbar and splash use **CUTOUT**. The favicon and the app icons use **SOLID**,
because they sit on an opaque navy plate, where a cutout would merely reveal that plate
through the shield.

To flip the navbar and splash back to SOLID, change the two paths in `index.html` and
bump `CACHE_NAME` in `sw.js` (static assets are cache-first, so the version string is
what forces a refresh):

```text
/brand/mark-64-cutout.png   ->  /brand/mark-64.png
/brand/mark-256-cutout.png  ->  /brand/mark-256.png
```
