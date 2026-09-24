# brand/

Every file here except `veripresenx-master.jpg` is **generated** by
`tools/brand/build.ps1`. Do not hand-edit the outputs — change the master and rebuild:

```
powershell -ExecutionPolicy Bypass -File tools\brand\build.ps1
```

## Files

| File | Size | Where it is used |
|---|---|---|
| `veripresenx-master.jpg` | 2816×1536 | Source artwork (shield + wordmark + tagline). The only hand-authored input. |
| `veripresenx-mark-1024.png` | 1024² | Transparent archival mark. Not shipped to browsers; source for future native / print use. |
| `mark-256.png` | 256² | Splash screen — `index.html` `.splash-logo`, displayed at 120 px, so 2× for retina. |
| `mark-64.png` | 64² | Navbar — `index.html` `.brand-logo`, displayed at 32 px, so 2× for retina. |
| `favicon-32.png` | 32² | Browser tab (`<link rel="icon">`). |
| `apple-touch-icon-180.png` | 180² | iOS home screen. Opaque, because iOS composites transparency to black. |
| `icon-192.png`, `icon-512.png` | 192², 512² | PWA install, `"purpose": "any"`. |
| `icon-maskable-512.png` | 512² | PWA install, `"purpose": "maskable"` — mark at 58% so it survives the safe-zone crop. |
| `preview.png` | 1000×620 | QA contact sheet: every size on both themes, plus the icons. |

## Why the app ships the mark, not the lockup

`index.html` renders the wordmark as **HTML text** — Space Grotesk 700 with a
teal→violet `background-clip: text` gradient — so only the shield mark is needed as an
image. The navbar stays legible at any zoom and the splash stays crisp on any DPR
without shipping a bitmap wordmark or a second asset per theme.

## Transparency

The mark is transparent and tuned to read on **both** themes (`--bg-base` #F5F3FF in
light, #0A0B14 in dark), which is why it carries no baked plate.

The PWA icons are deliberately the opposite: full-bleed and opaque on a navy gradient.
Android and iOS apply their own mask, and a pre-rounded icon gets double-cropped inside
an adaptive shape.
