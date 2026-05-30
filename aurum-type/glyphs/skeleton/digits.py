"""
Скелеты цифр 0–9.
Высота цифр = Cap-height (610 ед.), с оптическим выступом для круглых форм.
"""

from typing import Dict
from math_core.constants import (
    PHI, PHI_INV, CAP_HEIGHT, X_HEIGHT, STEM_REGULAR, OVERSHOOT, FIB
)
from .latin_upper import SkeletonGlyph, SkeletonPoint as P, SkeletonContour as C, phi_width, golden_split


class DigitSkeleton:
    def __init__(self, stem: int = STEM_REGULAR):
        self.stem = stem
        self.h = CAP_HEIGHT

    def glyph_0(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 2.0)
        h = self.h + OVERSHOOT * 2
        cx, cy = w / 2, self.h / 2
        rx = w * 0.45
        ry = (self.h + OVERSHOOT) / 2
        kx = rx * 0.5523
        ky = ry * 0.5523

        contour = C([
            P(cx,       cy + ry, True, True, "curve"),
            P(cx + kx,  cy + ry, False, True, "curve"),
            P(cx + rx,  cy + ky, False, True, "curve"),
            P(cx + rx,  cy,      True, True, "curve"),
            P(cx + rx,  cy - ky, False, True, "curve"),
            P(cx + kx,  cy - ry, False, True, "curve"),
            P(cx,       cy - ry, True, True, "curve"),
            P(cx - kx,  cy - ry, False, True, "curve"),
            P(cx - rx,  cy - ky, False, True, "curve"),
            P(cx - rx,  cy,      True, True, "curve"),
            P(cx - rx,  cy + ky, False, True, "curve"),
            P(cx - kx,  cy + ry, False, True, "curve"),
        ])

        return SkeletonGlyph(
            name="zero", unicode_val=0x0030,
            width=round(w + self.stem),
            contours=[contour],
            anchors={"center": (cx, cy)}
        )

    def glyph_1(self) -> SkeletonGlyph:
        w = self.stem
        h = self.h

        stem_line = C([P(w / 2, 0), P(w / 2, h + OVERSHOOT)], closed=False)

        return SkeletonGlyph(
            name="one", unicode_val=0x0031,
            width=w + round(self.stem * PHI_INV * 2),
            contours=[stem_line],
            anchors={"top": (w / 2, h + OVERSHOOT)}
        )

    def glyph_2(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        h = self.h
        mid_y, _ = golden_split(h)
        k = 0.5523

        arc = C([
            P(w * 0.1,  mid_y, True, True, "curve"),
            P(w * 0.1,  h - OVERSHOOT, False, True, "curve"),
            P(w * 0.3,  h + OVERSHOOT,  False, True, "curve"),
            P(w * 0.5,  h + OVERSHOOT,  True,  True, "curve"),
            P(w * 0.7,  h + OVERSHOOT,  False, True, "curve"),
            P(w * 0.9,  h - OVERSHOOT,  False, True, "curve"),
            P(w * 0.9,  mid_y, True, True, "curve"),
        ], closed=False)

        diag = C([
            P(w * 0.9,  mid_y),
            P(w * 0.1,  0),
        ], closed=False)

        bottom = C([
            P(w * 0.1,  0),
            P(w * 0.9,  0),
        ], closed=False)

        return SkeletonGlyph(
            name="two", unicode_val=0x0032,
            width=round(w + self.stem),
            contours=[arc, diag, bottom],
            anchors={}
        )

    def glyph_3(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        h = self.h
        mid_y, _ = golden_split(h)
        k = 0.5523

        top_arc = C([
            P(w * 0.1,  mid_y, True, True, "curve"),
            P(w * 0.1,  h - OVERSHOOT, False, True, "curve"),
            P(w * 0.3,  h + OVERSHOOT,  False, True, "curve"),
            P(w * 0.5,  h + OVERSHOOT,  True,  True, "curve"),
            P(w * 0.7,  h + OVERSHOOT,  False, True, "curve"),
            P(w * 0.9,  h - OVERSHOOT,  False, True, "curve"),
            P(w * 0.9,  mid_y, True, True, "curve"),
        ], closed=False)

        bot_arc = C([
            P(w * 0.9,  mid_y, True, True, "curve"),
            P(w * 0.9,  OVERSHOOT, False, True, "curve"),
            P(w * 0.7,  0 - OVERSHOOT, False, True, "curve"),
            P(w * 0.5,  0 - OVERSHOOT, True,  True, "curve"),
            P(w * 0.3,  0 - OVERSHOOT, False, True, "curve"),
            P(w * 0.1,  OVERSHOOT, False, True, "curve"),
            P(w * 0.1,  mid_y, True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="three", unicode_val=0x0033,
            width=round(w + self.stem),
            contours=[top_arc, bot_arc],
            anchors={}
        )

    def glyph_4(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        h = self.h
        mid_y, _ = golden_split(h)

        left_diag  = C([P(w * 0.3, h + OVERSHOOT), P(0, mid_y)], closed=False)
        top_bar    = C([P(0, mid_y), P(w, mid_y)], closed=False)
        right_stem = C([P(w, 0), P(w, h + OVERSHOOT)], closed=False)

        return SkeletonGlyph(
            name="four", unicode_val=0x0034,
            width=round(w + self.stem),
            contours=[left_diag, top_bar, right_stem],
            anchors={}
        )

    def glyph_5(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        h = self.h
        mid_y, _ = golden_split(h)
        k = 0.5523

        top_bar = C([P(0, h + OVERSHOOT), P(w * 0.7, h + OVERSHOOT)], closed=False)
        left_bar = C([P(0, h + OVERSHOOT), P(0, mid_y)], closed=False)

        bot_arc = C([
            P(0,        mid_y, True, True, "curve"),
            P(0,        OVERSHOOT, False, True, "curve"),
            P(w * 0.3,  0 - OVERSHOOT, False, True, "curve"),
            P(w * 0.5,  0 - OVERSHOOT, True,  True, "curve"),
            P(w * 0.7,  0 - OVERSHOOT, False, True, "curve"),
            P(w * 0.9,  OVERSHOOT, False, True, "curve"),
            P(w * 0.9,  mid_y, True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="five", unicode_val=0x0035,
            width=round(w + self.stem),
            contours=[top_bar, left_bar, bot_arc],
            anchors={}
        )

    def glyph_6(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 2.0)
        h = self.h + OVERSHOOT * 2
        cx, cy = w / 2, self.h * 0.35
        rx = w * 0.4
        ry = self.h * 0.35
        kx = rx * 0.5523
        ky = ry * 0.5523

        bowl = C([
            P(cx,       cy + ry, True, True, "curve"),
            P(cx + kx,  cy + ry, False, True, "curve"),
            P(cx + rx,  cy + ky, False, True, "curve"),
            P(cx + rx,  cy,      True, True, "curve"),
            P(cx + rx,  cy - ky, False, True, "curve"),
            P(cx + kx,  cy - ry, False, True, "curve"),
            P(cx,       cy - ry, True, True, "curve"),
            P(cx - kx,  cy - ry, False, True, "curve"),
            P(cx - rx,  cy - ky, False, True, "curve"),
            P(cx - rx,  cy,      True, True, "curve"),
            P(cx - rx,  cy + ky, False, True, "curve"),
            P(cx - kx,  cy + ry, False, True, "curve"),
        ], closed=False)

        neck = C([
            P(cx - rx,  cy + ry),
            P(cx - rx,  h - OVERSHOOT),
        ], closed=False)

        return SkeletonGlyph(
            name="six", unicode_val=0x0036,
            width=round(w + self.stem),
            contours=[bowl, neck],
            anchors={}
        )

    def glyph_7(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        h = self.h

        top_bar = C([P(0, h + OVERSHOOT), P(w, h + OVERSHOOT)], closed=False)
        diag    = C([P(w, h + OVERSHOOT), P(w * 0.3, 0)], closed=False)

        return SkeletonGlyph(
            name="seven", unicode_val=0x0037,
            width=round(w + self.stem),
            contours=[top_bar, diag],
            anchors={}
        )

    def glyph_8(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        h = self.h + OVERSHOOT * 2
        mid_y, _ = golden_split(self.h)

        k = 0.5523
        r_top = (self.h - mid_y) / 2
        r_bot = mid_y / 2

        top_oval = C([
            P(w / 2,        mid_y + r_top, True, True, "curve"),
            P(w / 2 + r_top, mid_y, False, True, "curve"),
            P(w / 2,        mid_y - r_top, True, True, "curve"),
            P(w / 2 - r_top, mid_y, False, True, "curve"),
            P(w / 2,        mid_y + r_top, True, True, "curve"),
        ], closed=False)

        bot_oval = C([
            P(w / 2,        mid_y, True, True, "curve"),
            P(w / 2 + r_bot, mid_y - r_bot, False, True, "curve"),
            P(w / 2,        mid_y - r_bot*2, True, True, "curve"),
            P(w / 2 - r_bot, mid_y - r_bot, False, True, "curve"),
            P(w / 2,        mid_y, True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="eight", unicode_val=0x0038,
            width=round(w + self.stem),
            contours=[top_oval, bot_oval],
            anchors={"center": (w / 2, self.h / 2)}
        )

    def glyph_9(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 2.0)
        h = self.h + OVERSHOOT * 2
        cx, cy = w / 2, self.h * 0.65
        rx = w * 0.4
        ry = self.h * 0.35
        kx = rx * 0.5523
        ky = ry * 0.5523

        bowl = C([
            P(cx,       cy + ry, True, True, "curve"),
            P(cx + kx,  cy + ry, False, True, "curve"),
            P(cx + rx,  cy + ky, False, True, "curve"),
            P(cx + rx,  cy,      True, True, "curve"),
            P(cx + rx,  cy - ky, False, True, "curve"),
            P(cx + kx,  cy - ry, False, True, "curve"),
            P(cx,       cy - ry, True, True, "curve"),
            P(cx - kx,  cy - ry, False, True, "curve"),
            P(cx - rx,  cy - ky, False, True, "curve"),
            P(cx - rx,  cy,      True, True, "curve"),
            P(cx - rx,  cy + ky, False, True, "curve"),
            P(cx - kx,  cy + ry, False, True, "curve"),
        ], closed=False)

        neck = C([
            P(cx + rx,  cy - ry),
            P(cx + rx,  0),
        ], closed=False)

        return SkeletonGlyph(
            name="nine", unicode_val=0x0039,
            width=round(w + self.stem),
            contours=[bowl, neck],
            anchors={}
        )

    def generate_all(self) -> Dict[str, SkeletonGlyph]:
        return {
            "zero":  self.glyph_0(), "one":   self.glyph_1(),
            "two":   self.glyph_2(), "three": self.glyph_3(),
            "four":  self.glyph_4(), "five":  self.glyph_5(),
            "six":   self.glyph_6(), "seven": self.glyph_7(),
            "eight": self.glyph_8(), "nine":  self.glyph_9(),
        }
