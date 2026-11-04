---
name: icon-set
description: Create a new 32x32 icon set (or add icons to an existing one) for the LED matrix gallery – draw them in code with tools/icons, preview, install into MatrixClient/wwwroot/data/<category>/ and wire the category into rules.yaml. Use when asked to "add an icon set", "make N icons of X", or "add more X icons".
---

# Icon set workflow

Icons are drawn in Python with the stdlib-only helpers in `tools/icons/`
(read `tools/icons/README.md` for the `draw.py` API). No Pillow is installed – don't add it.

## 1. Plan the set
- Pick the category folder name (lowercase, e.g. `atari`) and an icon list of the requested size.
  Mix hardware/objects with recognisable motifs; check existing folders in
  `MatrixClient/wwwroot/data/` to avoid duplicates.
- File names: `<prefix>_<thing>.png`, lowercase snake_case (e.g. `atari_joystick`, `gb_pokeball`, `mario_coin`).
- Adding to an existing set: write a new script `tools/icons/sets/<set>_<theme>.py` rather than editing the old one, so committed images stay reproducible.

## 2. Write the generator
Create `tools/icons/sets/<set>.py`, copying the skeleton of an existing set (e.g. `sets/atari.py`):

```python
import os, sys; sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import sys, math, png
from draw import *
OUT=sys.argv[1]; icons={}
def icon(name):
    def deco(f): icons[name]=f; return f
    return deco

@icon('atari_joystick')
def _(c):
    c.fill(rect(5,16,26,29), '1a1a1a')
    ...

for name,f in icons.items():
    c=C(); f(c); png.write(f'{OUT}/{name}.png',c.p)
print(len(icons))
```

Design rules for a 32×32 LED panel:
- Black background (`C()` default) unless the scene needs a backdrop; black = LED off.
- Bold, high-contrast shapes that fill most of the frame; 1-px details barely read.
- Use only opaque pixels – alpha < 50 is sent as black anyway.
- Keep it deterministic: if you use `random`, call `random.seed(n)` inside the icon.
- Set-specific palettes are fine (e.g. Game Boy uses only the four DMG greens).

## 3. Generate and review
Write output to the session scratchpad, never straight into the repo:

```bash
python3 tools/icons/sets/<set>.py <scratch>/out
python3 tools/icons/sheet.py <scratch>/s1.png $(ls <scratch>/out/*.png | head -10)
```

Read each sheet image (≤10 icons per sheet) and fix anything illegible before installing.
Iterate until every icon reads clearly.

## 4. Install and schedule
- `mkdir -p MatrixClient/wwwroot/data/<set>` and copy the PNGs in.
- Add the category to the fitting rules in `MatrixClient/rules.yaml` (`categories: [...]`).
  Gaming sets usually join Gaming Night, Evening Mix, Weekend Adventure and the weekday Afternoon Mix;
  ask if the right slots are unclear. Extending an existing category needs no rule change.
- Run `dotnet test MatrixClient/MatrixClient.sln` (it validates `rules.yaml`).

## 5. Report / commit
Tell the user what was added (list the icons) and which rules changed. Commit only when asked;
commit the generator script together with the PNGs, message style:
`feat: <set> icon set (<short description>), added to <rules>` with a body listing the icons.
