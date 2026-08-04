#!/usr/bin/env python3
"""Generate Rimconemy storyteller portraits from the Gemini illustration.

Source: 1408x768 landscape illustration (character standing centrally,
RIMCONEMY logo baked into the bottom-right corner).
Targets (RimWorld StorytellerDef):
  - RimconemyLarge.png  1024x1024  (center-crop on the figure)
  - RimconemyTiny.png    128x128   (downscale of the large crop)

The square crop window is centred on the figure. `crop_center_x` is the
horizontal centre of the crop window as a fraction of the source width
(0.5 = true image centre; slightly less pulls the window left towards
the character).
"""
import os
import sys
from PIL import Image

SRC = "/home/vannon/Downloads/Gemini_Generated_Image_z0vpv3z0vpv3z0vp(1).png"
OUT_DIR = os.path.join(
    "mods", "05-Rimconemy-Infected-Automation",
    "Textures", "UI", "HeroArt", "Storytellers",
)
LARGE = 1024
TINY = 128

def main() -> int:
    crop_center_x = float(sys.argv[1]) if len(sys.argv) > 1 else 0.5

    img = Image.open(SRC).convert("RGBA")
    w, h = img.size
    side = min(w, h)  # 768 -> square crop, full height

    # Centre the square window at crop_center_x * source width.
    cx = int(w * crop_center_x)
    left = max(0, min(w - side, cx - side // 2))
    top = (h - side) // 2
    square = img.crop((left, top, left + side, top + side))

    os.makedirs(OUT_DIR, exist_ok=True)

    large = square.resize((LARGE, LARGE), Image.LANCZOS)
    large_path = os.path.join(OUT_DIR, "RimconemyLarge.png")
    large.save(large_path)

    tiny = square.resize((TINY, TINY), Image.LANCZOS)
    tiny_path = os.path.join(OUT_DIR, "RimconemyTiny.png")
    tiny.save(tiny_path)

    print(f"crop_center_x={crop_center_x}  window=({left},{top},{left+side},{top+side})")
    print(f"wrote {large_path}  {large.size}")
    print(f"wrote {tiny_path}  {tiny.size}")
    return 0

if __name__ == "__main__":
    sys.exit(main())
