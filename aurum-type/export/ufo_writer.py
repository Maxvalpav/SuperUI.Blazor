"""
Запись шрифта в формат UFO 3 (Unified Font Object).
UFO — исходный формат, из которого компилируются TTF/OTF/WOFF2.
"""

from pathlib import Path
from typing import Optional

import ufoLib2

from math_core.constants import (
    UPM, ASCENDER, DESCENDER, CAP_HEIGHT, X_HEIGHT,
    STEM_REGULAR, STEMS, PHI
)
from glyphs.skeleton.latin_upper import UppercaseSkeleton, SkeletonGlyph
from glyphs.skeleton.latin_lower import LowercaseSkeleton
from glyphs.skeleton.digits import DigitSkeleton
from glyphs.skeleton.punctuation import PunctuationSkeleton
from glyphs.outlines.builder import OutlineBuilder
from metrics.kerning import build_kern_table, sidebearing, GLYPH_CLASSES


def create_font_info(font: ufoLib2.Font, weight: int = 400) -> None:
    font.info.familyName         = "AurumType"
    font.info.styleName          = weight_to_style_name(weight)
    font.info.unitsPerEm         = UPM
    font.info.ascender           = ASCENDER
    font.info.descender          = DESCENDER
    font.info.capHeight          = CAP_HEIGHT
    font.info.xHeight            = X_HEIGHT
    font.info.italicAngle        = 0
    font.info.postscriptUnderlinePosition  = -round(UPM * 0.1)
    font.info.postscriptUnderlineThickness = STEMS.get(weight, STEM_REGULAR)
    font.info.postscriptStemSnapH = [STEMS.get(weight, STEM_REGULAR)]
    font.info.postscriptStemSnapV = [STEMS.get(weight, STEM_REGULAR)]
    font.info.openTypeOS2WinAscent  = ASCENDER
    font.info.openTypeOS2WinDescent = abs(DESCENDER)
    font.info.openTypeOS2TypoAscender  = ASCENDER
    font.info.openTypeOS2TypoDescender = DESCENDER
    font.info.openTypeOS2TypoLineGap   = 0
    font.info.openTypeOS2WeightClass = weight
    font.info.openTypeNameDesigner = "AurumType - Natural Law Typography"
    font.info.copyright = "Copyright 2024 AurumType. Built on phi = 1.6180339887"
    font.info.note      = (
        "Built on natural laws: golden ratio, Fibonacci, logarithmic spiral."
    )


def weight_to_style_name(weight: int) -> str:
    names = {
        100: "Thin", 200: "ExtraLight", 300: "Light",
        400: "Regular", 500: "Medium", 600: "SemiBold",
        700: "Bold", 800: "ExtraBold", 900: "Black",
    }
    return names.get(weight, "Regular")


def add_glyph(font: ufoLib2.Font, skeleton: SkeletonGlyph, builder: OutlineBuilder) -> None:
    glyph = font.newGlyph(skeleton.name)
    glyph.width = skeleton.width
    if skeleton.unicode_val:
        glyph.unicodes = [skeleton.unicode_val]

    pen = glyph.getPen()
    builder.build(skeleton, pen)

    for anchor_name, (ax, ay) in skeleton.anchors.items():
        from ufoLib2.objects import Anchor
        glyph.anchors.append(Anchor(name=anchor_name, x=int(ax), y=int(ay)))


def add_kerning(font: ufoLib2.Font, glyph_names: list, stem: int) -> None:
    kern_table = build_kern_table(glyph_names, stem)
    for (left, right), value in kern_table.items():
        if left in font and right in font:
            font.kerning[(left, right)] = value


def generate_ufo(
    output_dir: str = "output",
    weight: int = 400,
    has_serifs: bool = True
) -> Path:
    stem = STEMS.get(weight, STEM_REGULAR)
    style = weight_to_style_name(weight)
    ufo_path = Path(output_dir) / f"AurumType-{style}.ufo"

    font = ufoLib2.Font()
    create_font_info(font, weight)

    builder = OutlineBuilder(stem=stem, has_serifs=has_serifs, weight=weight)

    upper_gen = UppercaseSkeleton(stem=stem)
    for char, skel in upper_gen.generate_all().items():
        add_glyph(font, skel, builder)

    lower_gen = LowercaseSkeleton(stem=stem)
    for char, skel in lower_gen.generate_all().items():
        add_glyph(font, skel, builder)

    digit_gen = DigitSkeleton(stem=stem)
    for name, skel in digit_gen.generate_all().items():
        add_glyph(font, skel, builder)

    punct_gen = PunctuationSkeleton(stem=stem)
    for name, skel in punct_gen.generate_all().items():
        add_glyph(font, skel, builder)

    all_names = list(upper_gen.generate_all().keys())
    all_names += list(lower_gen.generate_all().keys())
    all_names += list(digit_gen.generate_all().keys())
    all_names += list(punct_gen.generate_all().keys())
    add_kerning(font, all_names, stem)

    seen = set()
    for class_name, members in GLYPH_CLASSES.items():
        existing = [m for m in members if m in font and m not in seen]
        for m in existing:
            seen.add(m)
        if existing:
            font.groups[f"public.kern1.{class_name}"] = existing
            font.groups[f"public.kern2.{class_name}"] = existing

    font.save(str(ufo_path), overwrite=True)
    print(f"[OK] UFO saved: {ufo_path}")
    return ufo_path
