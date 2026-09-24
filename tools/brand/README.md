# Brand asset pipeline

Rebuilds every image in `brand/` from a single source image — offline, deterministic,
with no npm / ImageMagick / external tooling. Windows PowerShell 5.1 + .NET
`System.Drawing`.

```
powershell -ExecutionPolicy Bypass -File tools\brand\build.ps1
```

## Input

`brand/veripresenx-master.jpg` (2816×1536) — the supplied lockup: shield mark on the
left, `VeriPresenX` wordmark, tagline, sitting on a soft grey wash with faint blueprint
grid lines and a subtle drop shadow. This is the only hand-authored file in `brand/`.

## Output

Every file listed in `brand/README.md`, overwritten on each run.

## What it actually does

`BrandBuild.Run` is the entry point.

1. **Locate the mark** (`FindMarkBox`) — column projection. The mark is the first dense
   saturated block scanning left to right, and the scan stops at the first blank gap
   wider than 1.5% of the width, which is the space before the wordmark. Measured on the
   master: mark at x=259..757, so the wordmark is excluded without a magic crop number.

2. **Remove the background** (`KeyBackground`) — classification then connectivity, *not*
   a colour tolerance:
   - Classify `artwork = sat > 34 || lum < 165`. Measured: the background never exceeds
     sat 17, and the shield's violet edge jumps sat 21 → 67 inside a single pixel, so
     this is a wide and safe margin.
   - Flood fill from the border across non-artwork pixels. Having **no tolerance term**
     is the entire point: the blueprint grid lines dip to lum 203 against a 244–250 wash,
     and a drift-based fill is *walled in* by them, abandoning the white plate behind the
     shield. That is precisely the artefact the first two attempts at this pipeline
     produced.

3. **Bound by the silhouette** (`CloseArt`) — the shield's inner band is pure white
   (`rgb(255,255,255)`, sat 0) against a background of sat ≤ 17, so colour *cannot*
   separate them, and that band is reachable from outside through a narrow channel. A
   plain fill therefore hollows the badge out. Morphological closing (dilate r=10 then
   erode r=10, square SE) seals any indentation narrower than 20 px, sealing the channel
   while leaving the logo's genuinely open notches untouched. Erode treats
   out-of-bounds as background, which is why the source crop is padded 24 px.

4. **Clean the alpha** (`FinishKey`) — a separable 1-2-1 feather for antialiasing, then
   de-fringe (semi-transparent edge pixels still hold the light background blend, which
   reads as a pale halo on the dark theme, so their colour is replaced with the mean of
   the opaque neighbours), then bleed artwork colour 2 px outward so bicubic downscaling
   interpolates colour-to-colour rather than colour-to-light.

5. **Compose** (`ComposeIcon`) — square-pad with a 9% margin, then emit the mark ladder
   (1024 / 256 / 64), the favicon, the four app icons, and the QA contact sheet.

## Adapting this to a different logo

1. Drop the new artwork in as `brand/veripresenx-master.jpg`.
2. Run it and read the diagnostics it prints: `artwork px`, `silhouette after closing`,
   `components`. A healthy run reports **`components 1`** (one solid mark) and a `tight`
   box that does **not** start at 0,0.
3. Look at `brand/preview.png` on both themes before committing. Sizes below ~40 px are
   where a bad key shows up first.
4. If the mark hollows out, raise the `CloseArt` radius in `BrandBuild.Keying.cs`; if it
   bloats or loses notches, lower it.

## Files

| File | Role |
|---|---|
| `build.ps1` | Runner |
| `BrandBuild.cs` | `Run` orchestration, pixel IO, mark detection, alpha bounding, square pad, resize |
| `BrandBuild.Keying.cs` | `KeyBackground` — classification + border flood fill, alpha build |
| `BrandBuild.Finish.cs` | `FinishKey` (de-fringe, bleed) and `CloseArt` (morphological closing) |
| `BrandBuild.Sheet.cs` | `ComposeIcon` and `BuildPreview` (QA contact sheet) |
