"""
Компиляция TTF/OTF из UFO через fontTools.
"""

import subprocess
from pathlib import Path
from fontTools.ttLib import TTFont
from math_core.constants import nearest_fibonacci


def compile_ttf(ufo_path: Path, output_dir: Path, autohint: bool = True) -> Path:
    output_dir.mkdir(parents=True, exist_ok=True)

    cmd = [
        "fontmake",
        "-u", str(ufo_path),
        "-o", "ttf",
        "--output-dir", str(output_dir),
        "--no-production-names",
    ]
    if autohint:
        cmd += ["--autohint"]

    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        raise RuntimeError(f"fontmake error:\n{result.stderr}")

    ttf_files = list(output_dir.glob("*.ttf"))
    if not ttf_files:
        raise FileNotFoundError("fontmake did not create .ttf file")

    ttf_path = ttf_files[0]

    font = TTFont(str(ttf_path))
    _apply_phi_metrics(font)
    _set_name_table(font)
    font.save(str(ttf_path))

    print(f"[OK] TTF compiled: {ttf_path}")
    return ttf_path


def _apply_phi_metrics(font: TTFont) -> None:
    os2 = font["OS/2"]
    hhea = font["hhea"]

    for attr in ["sTypoAscender", "sTypoDescender", "sxHeight", "sCapHeight"]:
        val = getattr(os2, attr, None)
        if val is not None:
            fib_val = nearest_fibonacci(abs(val)) * (1 if val >= 0 else -1)
            setattr(os2, attr, fib_val)

    print("  Metrics phi-aligned")


def _set_name_table(font: TTFont) -> None:
    name_table = font["name"]

    records = {
        0:  "Copyright 2024 AurumType. Built on phi = 1.6180339887",
        5:  "Version 1.0; AurumType",
        13: "This font is built using natural mathematical laws: "
            "Golden Ratio (phi), Fibonacci sequence, Logarithmic spiral.",
    }

    for name_id, string in records.items():
        name_table.setName(string, name_id, 3, 1, 0x0409)
