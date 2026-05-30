"""
Скелеты строчных латинских букв a–z.
x-height = 377 ед., выносные элементы до Ascender (987) и Descender (-233).
"""

from typing import Dict, List, Tuple
from math_core.constants import (
    PHI, PHI_INV, FIB,
    CAP_HEIGHT, X_HEIGHT, ASCENDER, DESCENDER,
    STEM_REGULAR, SERIF_LENGTH, OVERSHOOT,
)
from .latin_upper import SkeletonGlyph, SkeletonPoint as P, SkeletonContour as C, phi_width, golden_split


class LowercaseSkeleton:
    def __init__(self, stem: int = STEM_REGULAR):
        self.stem = stem
        self.x = X_HEIGHT
        self.asc = ASCENDER
        self.desc = DESCENDER

    def glyph_o(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        cx, cy = w / 2, self.x / 2
        rx = w / 2
        ry = self.x / 2
        kx = rx * 0.5523
        ky = ry * 0.5523

        contour = C([
            P(cx,       cy + ry, True,  True, "curve"),
            P(cx + kx,  cy + ry, False, True, "curve"),
            P(cx + rx,  cy + ky, False, True, "curve"),
            P(cx + rx,  cy,      True,  True, "curve"),
            P(cx + rx,  cy - ky, False, True, "curve"),
            P(cx + kx,  cy - ry, False, True, "curve"),
            P(cx,       cy - ry, True,  True, "curve"),
            P(cx - kx,  cy - ry, False, True, "curve"),
            P(cx - rx,  cy - ky, False, True, "curve"),
            P(cx - rx,  cy,      True,  True, "curve"),
            P(cx - rx,  cy + ky, False, True, "curve"),
            P(cx - kx,  cy + ry, False, True, "curve"),
        ])

        return SkeletonGlyph(
            name="o", unicode_val=0x006F,
            width=round(w + self.stem),
            contours=[contour],
            anchors={"center": (cx, cy)}
        )

    def glyph_a(self) -> SkeletonGlyph:
        o = self.glyph_o()
        # Добавить левую вертикаль
        left_stem = C([P(0, 0), P(0, self.x)], closed=False)
        o.contours.insert(0, left_stem)
        o.name = "a"
        o.unicode_val = 0x0061
        return o

    def glyph_b(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        cx, cy = w / 2, self.x / 2
        rx = w * 0.4
        ry = self.x * 0.45
        kx = rx * 0.5523
        ky = ry * 0.5523

        stem = C([P(w * 0.3, 0), P(w * 0.3, self.asc)], closed=False)
        bowl = C([
            P(w * 0.3,  cy + ry, True, True, "curve"),
            P(w * 0.3 + kx, cy + ry, False, True, "curve"),
            P(w * 0.3 + rx, cy + ky, False, True, "curve"),
            P(w * 0.3 + rx, cy,      True, True, "curve"),
            P(w * 0.3 + rx, cy - ky, False, True, "curve"),
            P(w * 0.3 + kx, cy - ry, False, True, "curve"),
            P(w * 0.3,  cy - ry, True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="b", unicode_val=0x0062,
            width=round(w + self.stem),
            contours=[stem, bowl],
            anchors={}
        )

    def glyph_d(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        cx, cy = w / 2, self.x / 2
        rx = w * 0.4
        ry = self.x * 0.45
        kx = rx * 0.5523
        ky = ry * 0.5523

        bowl = C([
            P(w * 0.7,  cy + ry, True, True, "curve"),
            P(w * 0.7 - kx, cy + ry, False, True, "curve"),
            P(w * 0.7 - rx, cy + ky, False, True, "curve"),
            P(w * 0.7 - rx, cy,      True, True, "curve"),
            P(w * 0.7 - rx, cy - ky, False, True, "curve"),
            P(w * 0.7 - kx, cy - ry, False, True, "curve"),
            P(w * 0.7,  cy - ry, True, True, "curve"),
        ], closed=False)
        stem = C([P(w * 0.7, 0), P(w * 0.7, self.asc)], closed=False)

        return SkeletonGlyph(
            name="d", unicode_val=0x0064,
            width=round(w + self.stem),
            contours=[bowl, stem],
            anchors={}
        )

    def glyph_e(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        cx, cy = w / 2, self.x / 2
        rx = w * 0.4
        ry = self.x * 0.45
        kx = rx * 0.5523
        ky = ry * 0.5523

        bowl = C([
            P(w * 0.7,  cy,      True, True, "curve"),
            P(w * 0.7,  cy + ky, False, True, "curve"),
            P(w * 0.7 - kx, cy + ry, False, True, "curve"),
            P(cx,       cy + ry, True, True, "curve"),
            P(cx - kx,  cy + ry, False, True, "curve"),
            P(cx - rx,  cy + ky, False, True, "curve"),
            P(cx - rx,  cy,      True, True, "curve"),
        ], closed=False)

        bar = C([
            P(cx - rx, cy, True, True, "curve"),
            P(w * 0.7, cy, True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="e", unicode_val=0x0065,
            width=round(w + self.stem),
            contours=[bowl, bar],
            anchors={}
        )

    def glyph_c(self) -> SkeletonGlyph:
        o = self.glyph_o()
        # Оставить только правую половину
        w = phi_width(self.stem, 1.5)
        cx, cy = w / 2, self.x / 2
        rx = w * 0.4
        ry = self.x * 0.45
        kx = rx * 0.5523
        ky = ry * 0.5523

        curve = C([
            P(cx + rx,  cy,      True, True, "curve"),
            P(cx + rx,  cy + ky, False, True, "curve"),
            P(cx + kx,  cy + ry, False, True, "curve"),
            P(cx,       cy + ry, True, True, "curve"),
            P(cx - kx,  cy + ry, False, True, "curve"),
            P(cx - rx,  cy + ky, False, True, "curve"),
            P(cx - rx,  cy,      True, True, "curve"),
            P(cx - rx,  cy - ky, False, True, "curve"),
            P(cx - kx,  cy - ry, False, True, "curve"),
            P(cx,       cy - ry, True, True, "curve"),
            P(cx + kx,  cy - ry, False, True, "curve"),
            P(cx + rx,  cy - ky, False, True, "curve"),
            P(cx + rx,  cy,      True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="c", unicode_val=0x0063,
            width=round(w + self.stem),
            contours=[curve],
            anchors={"center": (cx, cy)}
        )

    def glyph_s(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        k = 0.5523
        mid_y, _ = golden_split(self.x)

        arc = C([
            P(w / 2,        self.x,      True, True, "curve"),
            P(w / 2 + w*k*0.3, self.x,   False, True, "curve"),
            P(w,            mid_y + (self.x-mid_y)*0.5, False, True, "curve"),
            P(w / 2,        mid_y,       True, True, "curve"),
            P(0,            mid_y - mid_y*0.5, False, True, "curve"),
            P(w / 2 - w*k*0.3, 0,        False, True, "curve"),
            P(w / 2,        0,           True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="s", unicode_val=0x0073,
            width=round(w + self.stem),
            contours=[arc],
            anchors={"inflection": (w / 2, mid_y)}
        )

    def glyph_n(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        rx = w * 0.35
        ry = self.x * 0.45
        kx = rx * 0.5523
        ky = ry * 0.5523

        left_stem  = C([P(0, 0), P(0, self.x)], closed=False)
        arch = C([
            P(0,        self.x, True, True, "curve"),
            P(0 + kx,   self.x, False, True, "curve"),
            P(rx,       self.x - ky, False, True, "curve"),
            P(rx,       ry,     True, True, "curve"),
            P(rx,       0,      True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="n", unicode_val=0x006E,
            width=round(w + self.stem),
            contours=[left_stem, arch],
            anchors={}
        )

    def glyph_m(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 2.0)
        rx = w * 0.25
        ry = self.x * 0.45
        kx = rx * 0.5523
        ky = ry * 0.5523

        left_stem = C([P(0, 0), P(0, self.x)], closed=False)
        arch1 = C([
            P(0,        self.x, True, True, "curve"),
            P(0 + kx,   self.x, False, True, "curve"),
            P(rx,       self.x - ky, False, True, "curve"),
            P(rx,       ry,     True, True, "curve"),
            P(rx,       0,      True, True, "curve"),
        ], closed=False)
        arch2 = C([
            P(rx,       0,      True, True, "curve"),
            P(rx,       self.x, True, True, "curve"),
            P(w - rx,   self.x, True, True, "curve"),
        ], closed=False)
        right_stem = C([P(w, 0), P(w, self.x)], closed=False)

        return SkeletonGlyph(
            name="m", unicode_val=0x006D,
            width=round(w + self.stem),
            contours=[left_stem, arch1, arch2, right_stem],
            anchors={}
        )

    def glyph_h(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        rx = w * 0.35
        ry = self.x * 0.45
        kx = rx * 0.5523
        ky = ry * 0.5523

        stem = C([P(0, 0), P(0, self.asc)], closed=False)
        arch = C([
            P(0,        self.x, True, True, "curve"),
            P(0 + kx,   self.x, False, True, "curve"),
            P(rx,       self.x - ky, False, True, "curve"),
            P(rx,       ry,     True, True, "curve"),
            P(rx,       0,      True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="h", unicode_val=0x0068,
            width=round(w + self.stem),
            contours=[stem, arch],
            anchors={}
        )

    def glyph_l(self) -> SkeletonGlyph:
        w = self.stem
        stem = C([P(w / 2, 0), P(w / 2, self.asc)], closed=False)

        return SkeletonGlyph(
            name="l", unicode_val=0x006C,
            width=w + SERIF_LENGTH,
            contours=[stem],
            anchors={}
        )

    def glyph_i(self) -> SkeletonGlyph:
        w = self.stem
        stem  = C([P(w / 2, 0), P(w / 2, self.x)], closed=False)
        dot_y = self.asc * 0.85
        dot_r = self.stem * 0.3

        return SkeletonGlyph(
            name="i", unicode_val=0x0069,
            width=w + SERIF_LENGTH,
            contours=[stem],
            anchors={"dot_center": (w / 2, dot_y)}
        )

    def glyph_j(self) -> SkeletonGlyph:
        w = self.stem
        stem  = C([P(w * 0.3, self.desc), P(w * 0.3, self.x)], closed=False)
        dot_y = self.asc * 0.85

        return SkeletonGlyph(
            name="j", unicode_val=0x006A,
            width=w + SERIF_LENGTH + 5,
            contours=[stem],
            anchors={"dot_center": (w * 0.3, dot_y)}
        )

    def glyph_u(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        rx = w * 0.35
        ry = self.x * 0.45
        kx = rx * 0.5523
        ky = ry * 0.5523

        right_stem = C([P(w, 0), P(w, self.x)], closed=False)
        arch = C([
            P(w,        0,      True, True, "curve"),
            P(w,        ry,     True, True, "curve"),
            P(w - kx,   self.x, False, True, "curve"),
            P(w - rx,   self.x, True, True, "curve"),
            P(0,        self.x, True, True, "curve"),
        ], closed=False)
        left_stem  = C([P(0, 0), P(0, self.x)], closed=False)

        return SkeletonGlyph(
            name="u", unicode_val=0x0075,
            width=round(w + self.stem),
            contours=[right_stem, arch, left_stem],
            anchors={}
        )

    def glyph_f(self) -> SkeletonGlyph:
        w = self.stem * 2
        asc = self.asc

        stem = C([P(w * 0.5, 0), P(w * 0.5, asc)], closed=False)
        top  = C([P(w * 0.5 - w/2, asc), P(w * 0.5 + w/2, asc)], closed=False)

        return SkeletonGlyph(
            name="f", unicode_val=0x0066,
            width=w + SERIF_LENGTH,
            contours=[stem, top],
            anchors={}
        )

    def glyph_g(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        cx, cy = w / 2, self.x / 2
        rx = w * 0.4
        ry = self.x * 0.45
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

        tail = C([
            P(cx,       cy - ry, True, True, "curve"),
            P(cx - rx,  cy - ry - (self.x * 0.3), False, True, "curve"),
            P(cx - rx,  self.desc * 0.7, True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="g", unicode_val=0x0067,
            width=round(w + self.stem),
            contours=[bowl, tail],
            anchors={}
        )

    def glyph_p(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        cx, cy = w / 2, self.x / 2
        rx = w * 0.4
        ry = self.x * 0.45
        kx = rx * 0.5523
        ky = ry * 0.5523

        stem = C([P(w * 0.3, self.desc), P(w * 0.3, self.x)], closed=False)
        bowl = C([
            P(w * 0.3,  cy + ry, True, True, "curve"),
            P(w * 0.3 + kx, cy + ry, False, True, "curve"),
            P(w * 0.3 + rx, cy + ky, False, True, "curve"),
            P(w * 0.3 + rx, cy,      True, True, "curve"),
            P(w * 0.3 + rx, cy - ky, False, True, "curve"),
            P(w * 0.3 + kx, cy - ry, False, True, "curve"),
            P(w * 0.3,  cy - ry, True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="p", unicode_val=0x0070,
            width=round(w + self.stem),
            contours=[stem, bowl],
            anchors={}
        )

    def glyph_q(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        cx, cy = w / 2, self.x / 2
        rx = w * 0.4
        ry = self.x * 0.45
        kx = rx * 0.5523
        ky = ry * 0.5523

        bowl = C([
            P(w * 0.7,  cy + ry, True, True, "curve"),
            P(w * 0.7 - kx, cy + ry, False, True, "curve"),
            P(w * 0.7 - rx, cy + ky, False, True, "curve"),
            P(w * 0.7 - rx, cy,      True, True, "curve"),
            P(w * 0.7 - rx, cy - ky, False, True, "curve"),
            P(w * 0.7 - kx, cy - ry, False, True, "curve"),
            P(w * 0.7,  cy - ry, True, True, "curve"),
        ], closed=False)

        stem = C([P(w * 0.7, self.desc), P(w * 0.7, self.x)], closed=False)

        return SkeletonGlyph(
            name="q", unicode_val=0x0071,
            width=round(w + self.stem),
            contours=[bowl, stem],
            anchors={}
        )

    def glyph_t(self) -> SkeletonGlyph:
        w = self.stem * 2
        stem = C([P(w * 0.5, 0), P(w * 0.5, self.asc)], closed=False)
        bar  = C([P(0, self.x * 0.8), P(w, self.x * 0.8)], closed=False)

        return SkeletonGlyph(
            name="t", unicode_val=0x0074,
            width=w + SERIF_LENGTH,
            contours=[stem, bar],
            anchors={}
        )

    def glyph_v(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        x = self.x

        contour = C([
            P(0,    x),
            P(w/2,  0),
            P(w,    x),
        ])

        return SkeletonGlyph(
            name="v", unicode_val=0x0076,
            width=round(w + self.stem),
            contours=[contour],
            anchors={}
        )

    def glyph_w(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 2.0)
        x = self.x
        q1 = w / 4
        q3 = 3 * w / 4

        contour = C([
            P(0,   x),
            P(q1,  0),
            P(w/2, x),
            P(q3,  0),
            P(w,   x),
        ])

        return SkeletonGlyph(
            name="w", unicode_val=0x0077,
            width=round(w + self.stem),
            contours=[contour],
            anchors={}
        )

    def glyph_x(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        x = self.x

        d1 = C([P(0, x), P(w, 0)], closed=False)
        d2 = C([P(0, 0), P(w, x)], closed=False)

        return SkeletonGlyph(
            name="x", unicode_val=0x0078,
            width=round(w + self.stem),
            contours=[d1, d2],
            anchors={}
        )

    def glyph_y(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        x = self.x

        d1 = C([P(0, x), P(w/2, 0)], closed=False)
        d2 = C([P(w, x), P(w/2, 0)], closed=False)
        d3 = C([P(w/2, 0), P(w/2, self.desc * 0.7)], closed=False)

        return SkeletonGlyph(
            name="y", unicode_val=0x0079,
            width=round(w + self.stem),
            contours=[d1, d2, d3],
            anchors={}
        )

    def glyph_z(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        x = self.x

        top_bar    = C([P(0, x), P(w, x)], closed=False)
        diagonal   = C([P(w, x), P(0, 0)], closed=False)
        bottom_bar = C([P(0, 0), P(w, 0)], closed=False)

        return SkeletonGlyph(
            name="z", unicode_val=0x007A,
            width=round(w + self.stem),
            contours=[top_bar, diagonal, bottom_bar],
            anchors={}
        )

    def glyph_k(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        x = self.x
        mid_y, _ = golden_split(x)

        stem = C([P(0, 0), P(0, x)], closed=False)
        top_diag = C([P(0, x), P(w, mid_y)], closed=False)
        bot_diag = C([P(0, mid_y), P(w, 0)], closed=False)

        return SkeletonGlyph(
            name="k", unicode_val=0x006B,
            width=round(w + self.stem),
            contours=[stem, top_diag, bot_diag],
            anchors={"knee": (0, mid_y)}
        )

    def glyph_r(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        x = self.x
        rx = w * 0.35
        ry = x * 0.45
        kx = rx * 0.5523
        ky = ry * 0.5523

        stem = C([P(0, 0), P(0, x)], closed=False)
        arch = C([
            P(0,    x, True, True, "curve"),
            P(kx,   x, False, True, "curve"),
            P(rx,   x - ky, False, True, "curve"),
            P(rx,   ry, True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="r", unicode_val=0x0072,
            width=round(w + self.stem),
            contours=[stem, arch],
            anchors={}
        )

    def generate_all(self) -> Dict[str, SkeletonGlyph]:
        return {
            "a": self.glyph_a(), "b": self.glyph_b(), "c": self.glyph_c(),
            "d": self.glyph_d(), "e": self.glyph_e(), "f": self.glyph_f(),
            "g": self.glyph_g(), "h": self.glyph_h(), "i": self.glyph_i(),
            "j": self.glyph_j(), "k": self.glyph_k(), "l": self.glyph_l(),
            "m": self.glyph_m(), "n": self.glyph_n(), "o": self.glyph_o(),
            "p": self.glyph_p(), "q": self.glyph_q(), "r": self.glyph_r(),
            "s": self.glyph_s(), "t": self.glyph_t(), "u": self.glyph_u(),
            "v": self.glyph_v(), "w": self.glyph_w(), "x": self.glyph_x(),
            "y": self.glyph_y(), "z": self.glyph_z(),
        }
