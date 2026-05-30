"""
Proof sheet render and visualization via Pillow.
"""

import math
from pathlib import Path
from typing import List, Optional

from PIL import Image, ImageDraw

from math_core.constants import (
    PHI, PHI_INV, FIB,
    UPM, CAP_HEIGHT, X_HEIGHT, DESCENDER,
    STEM_REGULAR, OVERSHOOT
)
from math_core.spiral import GoldenSpiral

COLOR_BG       = (250, 248, 240)
COLOR_FG       = (20,  18,  10)
COLOR_GOLD     = (180, 140,  40)
COLOR_GRID     = (200, 195, 180)
COLOR_SPIRAL   = (160, 100,  20)
COLOR_METRICS  = (100, 150, 200)


def render_golden_spiral_diagram(width: int = 800, height: int = 500) -> Image.Image:
    img = Image.new("RGB", (width, height), COLOR_BG)
    draw = ImageDraw.Draw(img)

    x, y = width // 4, height // 4
    w = min(width, height) // 2

    directions = [(1, 0), (0, 1), (-1, 0), (0, -1)]
    cur_x, cur_y = x, y

    for i in range(8):
        f = FIB[i + 1]
        size = round(w * f / FIB[9])
        dx, dy = directions[i % 4]

        rect = (cur_x, cur_y, cur_x + size, cur_y + size)
        draw.rectangle(rect, outline=COLOR_GRID, width=1)

        if size > 20:
            draw.text(
                (cur_x + size // 2, cur_y + size // 2),
                str(f), fill=COLOR_GOLD, anchor="mm"
            )

        if dx == 1: cur_x += size
        elif dx == -1: cur_x -= size
        elif dy == 1: cur_y += size
        elif dy == -1: cur_y -= size

    spiral = GoldenSpiral(scale=w * 0.15, center=(x + w * 0.45, y + w * 0.28))
    pts = spiral.arc_points(-2 * math.pi, 4 * math.pi, 512)
    visible = [(int(p[0]), int(p[1])) for p in pts
               if 0 <= p[0] < width and 0 <= p[1] < height]
    if len(visible) > 2:
        draw.line(visible, fill=COLOR_SPIRAL, width=2)

    return img


def render_glyph_anatomy(width: int = 600, height: int = 700) -> Image.Image:
    img = Image.new("RGB", (width, height), COLOR_BG)
    draw = ImageDraw.Draw(img)

    margin = 60
    scale = (height - margin * 2) / (UPM + abs(DESCENDER))

    def uy(y_upm: float) -> int:
        return int(height - margin - (y_upm - DESCENDER) * scale)

    lines = [
        (CAP_HEIGHT, "Cap Height", COLOR_GOLD),
        (X_HEIGHT,   "x-Height",   COLOR_GOLD),
        (0,          "Baseline",   COLOR_FG),
    ]

    for y_upm, label, color in lines:
        y_px = uy(y_upm)
        draw.line([(margin, y_px), (width - margin, y_px)], fill=color, width=1)
        draw.text((margin - 5, y_px), label, fill=color, anchor="rm")
        draw.text((width - margin + 5, y_px), str(y_upm), fill=color, anchor="lm")

    stem_px = round(STEM_REGULAR * scale)
    glyph_w = round(CAP_HEIGHT * PHI_INV * scale)
    glyph_x = (width - glyph_w) // 2

    draw.rectangle([
        glyph_x, uy(CAP_HEIGHT),
        glyph_x + stem_px, uy(0)
    ], fill=COLOR_FG)

    draw.rectangle([
        glyph_x + glyph_w - stem_px, uy(CAP_HEIGHT),
        glyph_x + glyph_w, uy(0)
    ], fill=COLOR_FG)

    cross_y = uy(X_HEIGHT)
    cross_thickness = round(stem_px * PHI_INV)
    draw.rectangle([
        glyph_x, cross_y - cross_thickness // 2,
        glyph_x + glyph_w, cross_y + cross_thickness // 2
    ], fill=COLOR_FG)

    return img


def render_fibonacci_grid(width: int = 800, height: int = 600) -> Image.Image:
    img = Image.new("RGB", (width, height), COLOR_BG)
    draw = ImageDraw.Draw(img)

    base = 13

    x = 0
    col = 0
    while x < width:
        fib_size = FIB[min(col % 6 + 5, len(FIB) - 1)]
        step = round(fib_size * base / 13)
        draw.line([(x, 0), (x, height)], fill=COLOR_GRID, width=1)
        x += step
        col += 1

    y = 0
    row = 0
    while y < height:
        fib_size = FIB[min(row % 5 + 5, len(FIB) - 1)]
        step = round(fib_size * base / 13)
        draw.line([(0, y), (width, y)], fill=COLOR_GRID, width=1)
        y += step
        row += 1

    draw.text((20, 20), f"Fibonacci grid | base={base} units", fill=COLOR_GOLD)
    return img


def render_proof_sheet(
    font_weight: int = 400,
    text: str = "HAMBURGEVONS",
    sizes: List[int] = None,
    output_path: Path = Path("output/proof.png")
) -> None:
    if sizes is None:
        sizes = [8, 12, 16, 24, 32, 48, 64, 96]

    W, H = 1600, 2000
    sheet = Image.new("RGB", (W, H), COLOR_BG)
    draw = ImageDraw.Draw(sheet)

    draw.rectangle([(0, 0), (W, 80)], fill=COLOR_FG)
    draw.text((W // 2, 40),
              f"AurumType — Proof Sheet  |  Weight: {font_weight}  |  φ={PHI:.6f}",
              fill=COLOR_BG, anchor="mm")

    spiral_img = render_golden_spiral_diagram(800, 400)
    sheet.paste(spiral_img, (0, 90))

    anatomy_img = render_glyph_anatomy(600, 500)
    sheet.paste(anatomy_img, (800, 90))

    grid_img = render_fibonacci_grid(800, 300)
    sheet.paste(grid_img, (0, 500))

    draw.rectangle([(800, 590), (W, 800)], fill=(240, 236, 220))
    constants = [
        ("φ",           f"{PHI:.10f}"),
        ("1/φ",         f"{1/PHI:.10f}"),
        ("UPM",         str(987)),
        ("Cap Height",  f"{CAP_HEIGHT} = F₁₅"),
        ("x-Height",    f"{X_HEIGHT} = F₁₄"),
        ("Descender",   f"{DESCENDER} = -F₁₃"),
        ("Stem",        f"{STEM_REGULAR} = F₉"),
        ("Overshoot",   f"{OVERSHOOT} = F₇"),
    ]
    for i, (label, value) in enumerate(constants):
        col = i % 2
        row = i // 2
        x = 820 + col * 380
        y = 610 + row * 45
        draw.text((x, y), f"{label:15s} {value}", fill=COLOR_FG)

    from math_core.constants import phi_scale
    scale_pts = phi_scale(16, 4)
    y_cur = 830
    draw.text((40, y_cur), "TYPOGRAPHIC SCALE (phi-progression, base=16pt):", fill=COLOR_GOLD)
    y_cur += 40

    for pt in sorted(scale_pts):
        bar_w = min(int(pt * 8), W - 200)
        draw.rectangle([(150, y_cur), (150 + bar_w, y_cur + max(int(pt * 0.6), 4))],
                       fill=COLOR_FG)
        draw.text((10, y_cur + 2), f"{pt:>7.2f} pt", fill=COLOR_GOLD)
        y_cur += int(pt * 0.8) + 8
        if y_cur > H - 100:
            break

    output_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(str(output_path), dpi=(300, 300))
    print(f"[OK] Proof sheet saved: {output_path} ({W}x{H}px @ 300dpi)")
