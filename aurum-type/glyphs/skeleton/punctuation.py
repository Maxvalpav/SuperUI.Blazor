"""
Скелеты знаков препинания для AurumType.
"""

from typing import Dict
from math_core.constants import (
    PHI, PHI_INV, CAP_HEIGHT, X_HEIGHT, STEM_REGULAR, OVERSHOOT, FIB, UPM
)
from .latin_upper import SkeletonGlyph, SkeletonPoint as P, SkeletonContour as C, phi_width


class PunctuationSkeleton:
    def __init__(self, stem: int = STEM_REGULAR):
        self.stem = stem
        self.x = X_HEIGHT
        self.cap = CAP_HEIGHT

    def glyph_period(self) -> SkeletonGlyph:
        r = self.stem * 0.35
        cx = r
        cy = 0

        contour = C([
            P(cx,       cy + r, True, True, "curve"),
            P(cx + r*0.55, cy + r, False, True, "curve"),
            P(cx + r,   cy + r*0.55, False, True, "curve"),
            P(cx + r,   cy,     True, True, "curve"),
            P(cx + r,   cy - r*0.55, False, True, "curve"),
            P(cx + r*0.55, cy - r, False, True, "curve"),
            P(cx,       cy - r, True, True, "curve"),
            P(cx - r*0.55, cy - r, False, True, "curve"),
            P(cx - r,   cy - r*0.55, False, True, "curve"),
            P(cx - r,   cy,     True, True, "curve"),
            P(cx - r,   cy + r*0.55, False, True, "curve"),
            P(cx - r*0.55, cy + r, False, True, "curve"),
        ])

        return SkeletonGlyph(
            name="period", unicode_val=0x002E,
            width=round(r * 4),
            contours=[contour],
        )

    def glyph_comma(self) -> SkeletonGlyph:
        r = self.stem * 0.35
        cx = r
        cy = 0

        dot = C([
            P(cx,       cy + r, True, True, "curve"),
            P(cx + r*0.55, cy + r, False, True, "curve"),
            P(cx + r,   cy + r*0.55, False, True, "curve"),
            P(cx + r,   cy,     True, True, "curve"),
            P(cx + r,   cy - r*0.55, False, True, "curve"),
            P(cx + r*0.55, cy - r, False, True, "curve"),
            P(cx,       cy - r, True, True, "curve"),
            P(cx - r*0.55, cy - r, False, True, "curve"),
            P(cx - r,   cy - r*0.55, False, True, "curve"),
            P(cx - r,   cy,     True, True, "curve"),
            P(cx - r,   cy + r*0.55, False, True, "curve"),
            P(cx - r*0.55, cy + r, False, True, "curve"),
        ])

        tail = C([
            P(cx - r,   cy - r*0.3),
            P(cx - r*2, cy - r*3),
        ], closed=False)

        return SkeletonGlyph(
            name="comma", unicode_val=0x002C,
            width=round(r * 5),
            contours=[dot, tail],
        )

    def glyph_hyphen(self) -> SkeletonGlyph:
        h = self.stem * 0.6
        w = h * PHI * 2

        bar = C([
            P(0,    self.x / 2),
            P(w,    self.x / 2),
        ], closed=False)

        return SkeletonGlyph(
            name="hyphen", unicode_val=0x002D,
            width=round(w + self.stem),
            contours=[bar],
        )

    def glyph_colon(self) -> SkeletonGlyph:
        r = self.stem * 0.25
        cx = r

        top_dot = C([
            P(cx,       self.x * 0.6, True, True, "curve"),
            P(cx + r,   self.x * 0.6, False, True, "curve"),
            P(cx + r,   self.x * 0.6 - r*2, True, True, "curve"),
            P(cx,       self.x * 0.6 - r*2, True, True, "curve"),
            P(cx,       self.x * 0.6, True, True, "curve"),
        ])

        bot_dot = C([
            P(cx,       self.x * 0.25, True, True, "curve"),
            P(cx + r,   self.x * 0.25, False, True, "curve"),
            P(cx + r,   self.x * 0.25 - r*2, True, True, "curve"),
            P(cx,       self.x * 0.25 - r*2, True, True, "curve"),
            P(cx,       self.x * 0.25, True, True, "curve"),
        ])

        return SkeletonGlyph(
            name="colon", unicode_val=0x003A,
            width=round(r * 4 + self.stem),
            contours=[top_dot, bot_dot],
        )

    def glyph_semicolon(self) -> SkeletonGlyph:
        r = self.stem * 0.25
        cx = r

        dot = C([
            P(cx,       self.x * 0.6, True, True, "curve"),
            P(cx + r,   self.x * 0.6, False, True, "curve"),
            P(cx + r,   self.x * 0.6 - r*2, True, True, "curve"),
            P(cx,       self.x * 0.6 - r*2, True, True, "curve"),
            P(cx,       self.x * 0.6, True, True, "curve"),
        ])

        tail = C([
            P(cx,       self.x * 0.6 - r*2),
            P(cx - r*1.5, self.x * 0.2),
        ], closed=False)

        return SkeletonGlyph(
            name="semicolon", unicode_val=0x003B,
            width=round(r * 4 + self.stem),
            contours=[dot, tail],
        )

    def glyph_exclam(self) -> SkeletonGlyph:
        w = self.stem * 0.5
        h = self.cap

        stem_line = C([P(w, 0), P(w, h)], closed=False)
        r = self.stem * 0.3
        cx = w
        cy = 0

        dot = C([
            P(cx,       cy + r, True, True, "curve"),
            P(cx + r*0.55, cy + r, False, True, "curve"),
            P(cx + r,   cy + r*0.55, False, True, "curve"),
            P(cx + r,   cy,     True, True, "curve"),
            P(cx + r,   cy - r*0.55, False, True, "curve"),
            P(cx + r*0.55, cy - r, False, True, "curve"),
            P(cx,       cy - r, True, True, "curve"),
            P(cx - r*0.55, cy - r, False, True, "curve"),
            P(cx - r,   cy - r*0.55, False, True, "curve"),
            P(cx - r,   cy,     True, True, "curve"),
            P(cx - r,   cy + r*0.55, False, True, "curve"),
            P(cx - r*0.55, cy + r, False, True, "curve"),
        ])

        return SkeletonGlyph(
            name="exclam", unicode_val=0x0021,
            width=round(w * 3),
            contours=[stem_line, dot],
        )

    def glyph_question(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        h = self.cap
        k = 0.5523

        arc = C([
            P(w * 0.2,  h * 0.5, True, True, "curve"),
            P(w * 0.2,  h * 0.8, False, True, "curve"),
            P(w * 0.4,  h + OVERSHOOT, False, True, "curve"),
            P(w * 0.6,  h + OVERSHOOT, True, True, "curve"),
            P(w * 0.8,  h + OVERSHOOT, False, True, "curve"),
            P(w,        h * 0.8, False, True, "curve"),
            P(w,        h * 0.6, True, True, "curve"),
        ], closed=False)

        stem = C([
            P(w * 0.65, h * 0.4),
            P(w * 0.65, h * 0.2),
        ], closed=False)

        r = self.stem * 0.3
        cx = w * 0.65
        cy = 0

        dot = C([
            P(cx,       cy + r, True, True, "curve"),
            P(cx + r*0.55, cy + r, False, True, "curve"),
            P(cx + r,   cy + r*0.55, False, True, "curve"),
            P(cx + r,   cy,     True, True, "curve"),
            P(cx + r,   cy - r*0.55, False, True, "curve"),
            P(cx + r*0.55, cy - r, False, True, "curve"),
            P(cx,       cy - r, True, True, "curve"),
            P(cx - r*0.55, cy - r, False, True, "curve"),
            P(cx - r,   cy - r*0.55, False, True, "curve"),
            P(cx - r,   cy,     True, True, "curve"),
            P(cx - r,   cy + r*0.55, False, True, "curve"),
            P(cx - r*0.55, cy + r, False, True, "curve"),
        ])

        return SkeletonGlyph(
            name="question", unicode_val=0x003F,
            width=round(w + self.stem * 2),
            contours=[arc, stem, dot],
        )

    def glyph_quotedbl(self) -> SkeletonGlyph:
        w = self.stem * 0.4
        gap = self.stem * 0.5

        left_bar  = C([P(0, self.cap * 0.8), P(0, self.cap)], closed=False)
        right_bar = C([P(w + gap, self.cap * 0.8), P(w + gap, self.cap)], closed=False)

        return SkeletonGlyph(
            name="quotedbl", unicode_val=0x0022,
            width=round(w * 2 + gap + self.stem),
            contours=[left_bar, right_bar],
        )

    def glyph_quotesingle(self) -> SkeletonGlyph:
        w = self.stem * 0.4
        bar = C([P(0, self.cap * 0.8), P(0, self.cap)], closed=False)

        return SkeletonGlyph(
            name="quotesingle", unicode_val=0x0027,
            width=round(w + self.stem),
            contours=[bar],
        )

    def glyph_parentleft(self) -> SkeletonGlyph:
        w = self.stem * 0.5
        h = self.cap
        k = 0.5523

        curve = C([
            P(w,    h + OVERSHOOT, True, True, "curve"),
            P(0,    h * 0.7, False, True, "curve"),
            P(0,    h * 0.3, False, True, "curve"),
            P(w,    0 - OVERSHOOT, True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="parenleft", unicode_val=0x0028,
            width=round(w + self.stem),
            contours=[curve],
        )

    def glyph_parentright(self) -> SkeletonGlyph:
        w = self.stem * 0.5
        h = self.cap

        curve = C([
            P(0,    h + OVERSHOOT, True, True, "curve"),
            P(w,    h * 0.7, False, True, "curve"),
            P(w,    h * 0.3, False, True, "curve"),
            P(0,    0 - OVERSHOOT, True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="parenright", unicode_val=0x0029,
            width=round(w + self.stem),
            contours=[curve],
        )

    def generate_all(self) -> Dict[str, SkeletonGlyph]:
        return {
            "period":    self.glyph_period(),
            "comma":     self.glyph_comma(),
            "hyphen":    self.glyph_hyphen(),
            "colon":     self.glyph_colon(),
            "semicolon": self.glyph_semicolon(),
            "exclam":    self.glyph_exclam(),
            "question":  self.glyph_question(),
            "quotedbl":  self.glyph_quotedbl(),
            "quotesingle": self.glyph_quotesingle(),
            "parenleft":   self.glyph_parentleft(),
            "parenright":  self.glyph_parentright(),
        }
