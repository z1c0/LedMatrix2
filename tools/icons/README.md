# Icon tools

Pure-Python (stdlib only, no Pillow) generators for the 32×32 icons in
`MatrixClient/wwwroot/data/<category>/`. Every icon is drawn in code, so a set
can be regenerated, tweaked and re-run.

| File | Purpose |
|---|---|
| `png.py` | `write(path, px)` writes an RGBA PNG; `read(path)` decodes 8-bit PNGs |
| `draw.py` | 32×32 canvas `C(bg)` and pixel-mask helpers (below) |
| `sheet.py` | `python3 sheet.py out.png a.png b.png …` – 8× upscaled preview strip |
| `sets/*.py` | One script per set (or batch); `python3 sets/<set>.py <out_dir>` |

## Running a set

```bash
python3 tools/icons/sets/atari.py /tmp/atari            # writes <name>.png per icon
python3 tools/icons/sheet.py /tmp/atari-1.png $(ls /tmp/atari/*.png | head -10)
cp /tmp/atari/*.png MatrixClient/wwwroot/data/atari/
```

Output is deterministic (any randomness uses a fixed `random.seed`), so re-running
an unchanged script reproduces the committed files byte for byte.

## draw.py at a glance

Shapes are **masks**: sets of `(x, y)` pixels, combined with `|`, `&`, `-`.

- Canvas: `c = C('000000')`, `c.put(x, y, 'rrggbb')`, `c.fill(mask, 'rrggbb')`, `c.p` = pixel rows
- Shapes: `rect(x0,y0,x1,y1)` (inclusive), `disc(cx,cy,r)`, `ell(cx,cy,rx,ry)`, `poly([(x,y),…])`,
  `line(x0,y0,x1,y1,w=1)`, `polyline(pts,w=1)`, `every(lambda x,y: …)` (pixel centres at +.5)
- Mask ops: `mirror(m)` (left↔right), `dilate(m,n)`, `outline(m)` (ring outside), `edge(m)` (border inside)
- Pixel art: `art(rows, x0, y0, mir)` → `{char: mask}`; `glyph(ch, x, y, scale)` for a tiny 3×5 font (A, H, R)

Set scripts usually add small local helpers, e.g. `sprite()` / `pix()` to paint a
character grid at 2× scale with a colour map.
