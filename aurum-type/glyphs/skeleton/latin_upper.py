"""
Скелеты заглавных латинских букв A–Z.
Координаты в UPM (987 ед.), Origin = (0, 0) — baseline левый угол.
"""

import math
from typing import List, Dict, Tuple, Optional
from dataclasses import dataclass, field

from math_core.constants import (
    PHI, PHI_INV, FIB,
    CAP_HEIGHT, X_HEIGHT, ASCENDER, DESCENDER,
    STEM_REGULAR, SERIF_LENGTH,
    OVERSHOOT, ANGLE_36, DIAGONAL_ANGLE
)
from math_core.spiral import LogarithmicSpiral, GoldenSpiral
from math_core.proportions import GLYPH_WIDTH_TABLE


@dataclass
class SkeletonPoint:
    x: float
    y: float
    on_curve: bool = True
    smooth: bool = False
    type: str = "line"

@dataclass
class SkeletonContour:
    points: List[SkeletonPoint] = field(default_factory=list)
    closed: bool = True

@dataclass
class SkeletonGlyph:
    name: str
    unicode_val: int
    width: int
    contours: List[SkeletonContour] = field(default_factory=list)
    anchors: Dict[str, Tuple[float, float]] = field(default_factory=dict)

P = SkeletonPoint
C = SkeletonContour


def phi_width(stem: int = STEM_REGULAR, power: float = 1.5) -> int:
    return round(stem * (PHI ** power))

def golden_split(total: float) -> Tuple[float, float]:
    large = total / PHI
    small = total - large
    return (large, small)


class UppercaseSkeleton:
    def __init__(self, stem: int = STEM_REGULAR):
        self.stem = stem
        self.cap = CAP_HEIGHT
        self.overshoot = OVERSHOOT

    def glyph_A(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        h = self.cap
        half = w / 2
        apex_y = h + self.overshoot
        apex_x = half
        crossbar_y, _ = golden_split(h)

        contour = C([
            P(0, 0),
            P(apex_x, apex_y),
            P(w, 0),
        ])
        crossbar = C([
            P(half - self.stem * PHI_INV, crossbar_y),
            P(half + self.stem * PHI_INV, crossbar_y),
        ], closed=False)

        return SkeletonGlyph(
            name="A", unicode_val=0x0041,
            width=round(w + self.stem),
            contours=[contour, crossbar],
            anchors={"apex": (apex_x, apex_y), "crossbar": (half, crossbar_y)}
        )

    def glyph_O(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 2.0)
        h = self.cap + self.overshoot * 2
        cx, cy = w / 2, self.cap / 2
        rx = w / 2
        ry = (self.cap + self.overshoot) / 2
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
            name="O", unicode_val=0x004F,
            width=round(w + self.stem),
            contours=[contour],
            anchors={"center": (cx, cy)}
        )

    def glyph_H(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        h = self.cap
        cross_y, _ = golden_split(h)

        left_stem  = C([P(0, 0), P(0, h)], closed=False)
        right_stem = C([P(w, 0), P(w, h)], closed=False)
        crossbar   = C([P(0, cross_y), P(w, cross_y)], closed=False)

        return SkeletonGlyph(
            name="H", unicode_val=0x0048,
            width=round(w + self.stem),
            contours=[left_stem, right_stem, crossbar],
            anchors={"crossbar_left": (0, cross_y), "crossbar_right": (w, cross_y)}
        )

    def glyph_B(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        h = self.cap
        mid_y, _ = golden_split(h)

        left_stem = C([P(0, 0), P(0, h)], closed=False)

        r_top = (h - mid_y) / 2
        cy_top = mid_y + r_top
        k_top = r_top * 0.5523

        top_bump = C([
            P(0,        cy_top + r_top, True, True, "curve"),
            P(k_top,    cy_top + r_top, False, True, "curve"),
            P(w - self.stem, cy_top,    False, True, "curve"),
            P(w - self.stem, cy_top,    True,  True, "curve"),
        ], closed=False)

        r_bot = mid_y / 2
        cy_bot = r_bot
        k_bot = r_bot * 0.5523

        bot_bump = C([
            P(0,        cy_bot + r_bot, True, True, "curve"),
            P(k_bot,    cy_bot + r_bot, False, True, "curve"),
            P(w,        cy_bot,         False, True, "curve"),
            P(w,        cy_bot,         True,  True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="B", unicode_val=0x0042,
            width=round(w + self.stem * 2),
            contours=[left_stem, top_bump, bot_bump],
            anchors={"mid": (0, mid_y)}
        )

    def glyph_E(self) -> SkeletonGlyph:
        h = self.cap
        w_full = phi_width(self.stem, 1.0)
        w_mid  = round(w_full * PHI_INV)
        mid_y, _ = golden_split(h)

        left_stem  = C([P(0, 0),      P(0, h)],      closed=False)
        top_bar    = C([P(0, h),      P(w_full, h)],  closed=False)
        mid_bar    = C([P(0, mid_y),  P(w_mid, mid_y)], closed=False)
        bot_bar    = C([P(0, 0),      P(w_full, 0)],  closed=False)

        return SkeletonGlyph(
            name="E", unicode_val=0x0045,
            width=round(w_full + self.stem * 2),
            contours=[left_stem, top_bar, mid_bar, bot_bar],
            anchors={"mid": (0, mid_y)}
        )

    def glyph_S(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        h = self.cap + self.overshoot
        mid_y, _ = golden_split(h)
        k = 0.5523

        top_arc = C([
            P(w / 2,        h,              True,  True, "curve"),
            P(w / 2 + w*k*0.3, h,           False, True, "curve"),
            P(w,            mid_y + (h-mid_y)*0.5, False, True, "curve"),
            P(w / 2,        mid_y,          True,  True, "curve"),
            P(0,            mid_y - (mid_y)*0.5, False, True, "curve"),
            P(w / 2 - w*k*0.3, self.overshoot, False, True, "curve"),
            P(w / 2,        self.overshoot, True,  True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="S", unicode_val=0x0053,
            width=round(w + self.stem),
            contours=[top_arc],
            anchors={"inflection": (w / 2, mid_y)}
        )

    def glyph_I(self) -> SkeletonGlyph:
        w = self.stem
        h = self.cap
        stem_line = C([P(w / 2, 0), P(w / 2, h)], closed=False)

        return SkeletonGlyph(
            name="I", unicode_val=0x0049,
            width=w + SERIF_LENGTH * 2,
            contours=[stem_line],
            anchors={}
        )

    def glyph_N(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        h = self.cap

        left_stem  = C([P(0, 0),   P(0, h)],   closed=False)
        right_stem = C([P(w, 0),   P(w, h)],   closed=False)
        diagonal   = C([P(0, h),   P(w, 0)],   closed=False)

        return SkeletonGlyph(
            name="N", unicode_val=0x004E,
            width=round(w + self.stem),
            contours=[left_stem, right_stem, diagonal],
            anchors={}
        )

    def glyph_M(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 2.5)
        h = self.cap
        mid_x = w / 2
        _, v_depth = golden_split(h)

        left_stem   = C([P(0,    0), P(0,    h)], closed=False)
        right_stem  = C([P(w,    0), P(w,    h)], closed=False)
        left_diag   = C([P(0,    h), P(mid_x, h - v_depth)], closed=False)
        right_diag  = C([P(mid_x, h - v_depth), P(w, h)],    closed=False)

        return SkeletonGlyph(
            name="M", unicode_val=0x004D,
            width=round(w + self.stem),
            contours=[left_stem, right_stem, left_diag, right_diag],
            anchors={"apex": (mid_x, h - v_depth)}
        )

    def glyph_T(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        h = self.cap
        half = self.stem / 2

        top_bar  = C([P(-half, h), P(w + half, h)], closed=False)
        stem     = C([P(w / 2, 0), P(w / 2, h)],    closed=False)

        return SkeletonGlyph(
            name="T", unicode_val=0x0054,
            width=round(w + self.stem * 2),
            contours=[top_bar, stem],
            anchors={"top_center": (w / 2, h)}
        )

    def glyph_L(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        h = self.cap

        vert  = C([P(0, 0), P(0, h)], closed=False)
        horiz = C([P(0, 0), P(w, 0)], closed=False)

        return SkeletonGlyph(
            name="L", unicode_val=0x004C,
            width=round(w + self.stem),
            contours=[vert, horiz],
            anchors={}
        )

    def glyph_F(self) -> SkeletonGlyph:
        h = self.cap
        w_full = phi_width(self.stem, 1.0)
        mid_y, _ = golden_split(h)

        left_stem  = C([P(0, 0),   P(0, h)],       closed=False)
        top_bar    = C([P(0, h),   P(w_full, h)],   closed=False)
        mid_bar    = C([P(0, mid_y), P(round(w_full * PHI_INV), mid_y)], closed=False)

        return SkeletonGlyph(
            name="F", unicode_val=0x0046,
            width=round(w_full + self.stem * 2),
            contours=[left_stem, top_bar, mid_bar],
            anchors={"mid": (0, mid_y)}
        )

    def glyph_P(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        h = self.cap
        mid_y, _ = golden_split(h)

        left_stem = C([P(0, 0), P(0, h)], closed=False)

        r_top = (h - mid_y) / 2
        cy_top = mid_y + r_top
        k_top = r_top * 0.5523

        top_bump = C([
            P(0,        cy_top + r_top, True, True, "curve"),
            P(k_top,    cy_top + r_top, False, True, "curve"),
            P(w,        cy_top,         False, True, "curve"),
            P(w,        mid_y,          True,  True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="P", unicode_val=0x0050,
            width=round(w + self.stem * 2),
            contours=[left_stem, top_bump],
            anchors={"mid": (0, mid_y)}
        )

    def glyph_D(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 2.0)
        h = self.cap
        cx, cy = w / 2, self.cap / 2
        rx = w / 2
        ry = self.cap / 2
        kx = rx * 0.5523
        ky = ry * 0.5523

        left_stem = C([P(0, 0), P(0, h)], closed=False)

        right_curve = C([
            P(0,       h,       True, True, "curve"),
            P(w * PHI_INV, h,   False, True, "curve"),
            P(w,       h/2 + h*0.2, False, True, "curve"),
            P(w,       h/2,     True, True, "curve"),
            P(w,       h/2 - h*0.2, False, True, "curve"),
            P(w * PHI_INV, 0,   False, True, "curve"),
            P(0,       0,       True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="D", unicode_val=0x0044,
            width=round(w + self.stem),
            contours=[left_stem, right_curve],
            anchors={"center": (cx, cy)}
        )

    def glyph_U(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        h = self.cap
        rx = w / 2
        kx = rx * 0.5523

        left_stem  = C([P(0, 0), P(0, h)], closed=False)
        right_stem = C([P(w, 0), P(w, h)], closed=False)

        bottom = C([
            P(0,        0, True, True, "curve"),
            P(0,       -OVERSHOOT, False, True, "curve"),
            P(w,       -OVERSHOOT, False, True, "curve"),
            P(w,        0, True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="U", unicode_val=0x0055,
            width=round(w + self.stem),
            contours=[left_stem, right_stem, bottom],
            anchors={}
        )

    def glyph_V(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        h = self.cap
        bottom_x = w / 2

        contour = C([
            P(0,         h),
            P(bottom_x,  0 - OVERSHOOT),
            P(w,         h),
        ])

        return SkeletonGlyph(
            name="V", unicode_val=0x0056,
            width=round(w + self.stem),
            contours=[contour],
            anchors={"bottom": (bottom_x, 0)}
        )

    def glyph_W(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 2.5)
        h = self.cap
        q1 = w / 4
        q3 = 3 * w / 4

        contour = C([
            P(0,   h),
            P(q1,  0 - OVERSHOOT),
            P(w/2, h),
            P(q3,  0 - OVERSHOOT),
            P(w,   h),
        ])

        return SkeletonGlyph(
            name="W", unicode_val=0x0057,
            width=round(w + self.stem),
            contours=[contour],
            anchors={}
        )

    def glyph_X(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        h = self.cap

        diag1 = C([P(0, h), P(w, 0)], closed=False)
        diag2 = C([P(0, 0), P(w, h)], closed=False)

        return SkeletonGlyph(
            name="X", unicode_val=0x0058,
            width=round(w + self.stem),
            contours=[diag1, diag2],
            anchors={}
        )

    def glyph_Y(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        h = self.cap
        mid_x = w / 2
        _, split_y = golden_split(h)

        top_left  = C([P(0, h), P(mid_x, split_y)], closed=False)
        top_right = C([P(w, h), P(mid_x, split_y)], closed=False)
        stem      = C([P(mid_x, split_y), P(mid_x, 0)], closed=False)

        return SkeletonGlyph(
            name="Y", unicode_val=0x0059,
            width=round(w + self.stem),
            contours=[top_left, top_right, stem],
            anchors={"split": (mid_x, split_y)}
        )

    def glyph_Z(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        h = self.cap

        top_bar    = C([P(0, h), P(w, h)], closed=False)
        diagonal   = C([P(w, h), P(0, 0)], closed=False)
        bottom_bar = C([P(0, 0), P(w, 0)], closed=False)

        return SkeletonGlyph(
            name="Z", unicode_val=0x005A,
            width=round(w + self.stem),
            contours=[top_bar, diagonal, bottom_bar],
            anchors={}
        )

    def glyph_C(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 2.0)
        h = self.cap + self.overshoot * 2
        cx, cy = w / 2, self.cap / 2
        rx = w * 0.45
        ry = (self.cap + self.overshoot) / 2
        kx = rx * 0.5523
        ky = ry * 0.5523

        curve = C([
            P(cx + rx,  cy,      True,  True, "curve"),
            P(cx + rx,  cy + ky, False, True, "curve"),
            P(cx + kx,  cy + ry, False, True, "curve"),
            P(cx,       cy + ry, True,  True, "curve"),
            P(cx - kx,  cy + ry, False, True, "curve"),
            P(cx - rx,  cy + ky, False, True, "curve"),
            P(cx - rx,  cy,      True,  True, "curve"),
            P(cx - rx,  cy - ky, False, True, "curve"),
            P(cx - kx,  cy - ry, False, True, "curve"),
            P(cx,       cy - ry, True,  True, "curve"),
            P(cx + kx,  cy - ry, False, True, "curve"),
            P(cx + rx,  cy - ky, False, True, "curve"),
            P(cx + rx,  cy,      True,  True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="C", unicode_val=0x0043,
            width=round(w + self.stem),
            contours=[curve],
            anchors={"center": (cx, cy)}
        )

    def glyph_G(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 2.0)
        h = self.cap + self.overshoot * 2
        cx, cy = w / 2, self.cap / 2
        rx = w * 0.45
        ry = (self.cap + self.overshoot) / 2
        kx = rx * 0.5523
        ky = ry * 0.5523

        curve = C([
            P(cx + rx,  cy,      True,  True, "curve"),
            P(cx + rx,  cy + ky, False, True, "curve"),
            P(cx + kx,  cy + ry, False, True, "curve"),
            P(cx,       cy + ry, True,  True, "curve"),
            P(cx - kx,  cy + ry, False, True, "curve"),
            P(cx - rx,  cy + ky, False, True, "curve"),
            P(cx - rx,  cy,      True,  True, "curve"),
        ], closed=False)

        tab = C([
            P(cx - rx,  cy,   True, True, "curve"),
            P(cx - rx,  cy - ry/2, False, True, "curve"),
            P(cx,       cy - ry/2, True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="G", unicode_val=0x0047,
            width=round(w + self.stem),
            contours=[curve, tab],
            anchors={"center": (cx, cy)}
        )

    def glyph_J(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 0.5)
        h = self.cap
        rx = w / 2
        kx = rx * 0.5523

        stem_line = C([P(w * 0.6, 0), P(w * 0.6, h)], closed=False)

        bottom = C([
            P(0,  0, True, True, "curve"),
            P(0, -OVERSHOOT, False, True, "curve"),
            P(w * 0.6, -OVERSHOOT, False, True, "curve"),
            P(w * 0.6, 0, True, True, "curve"),
        ], closed=False)

        return SkeletonGlyph(
            name="J", unicode_val=0x004A,
            width=round(w + self.stem),
            contours=[stem_line, bottom],
            anchors={}
        )

    def glyph_K(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.5)
        h = self.cap
        mid_y, _ = golden_split(h)

        left_stem   = C([P(0, 0), P(0, h)], closed=False)
        top_diag    = C([P(0, h), P(w, mid_y)], closed=False)
        bot_diag    = C([P(0, mid_y), P(w, 0)], closed=False)

        return SkeletonGlyph(
            name="K", unicode_val=0x004B,
            width=round(w + self.stem),
            contours=[left_stem, top_diag, bot_diag],
            anchors={"knee": (0, mid_y)}
        )

    def glyph_Q(self) -> SkeletonGlyph:
        o = self.glyph_O()
        w = phi_width(self.stem, 2.0)

        tail = C([
            P(w * 0.6,  -self.cap * 0.15),
            P(w,        -self.cap * 0.35),
        ], closed=False)

        o.contours.append(tail)
        o.name = "Q"
        o.unicode_val = 0x0051
        return o

    def glyph_R(self) -> SkeletonGlyph:
        w = phi_width(self.stem, 1.0)
        h = self.cap
        mid_y, _ = golden_split(h)

        left_stem = C([P(0, 0), P(0, h)], closed=False)

        r_top = (h - mid_y) / 2
        cy_top = mid_y + r_top
        k_top = r_top * 0.5523

        top_bump = C([
            P(0,        cy_top + r_top, True, True, "curve"),
            P(k_top,    cy_top + r_top, False, True, "curve"),
            P(w,        cy_top,         False, True, "curve"),
            P(w,        mid_y,          True,  True, "curve"),
        ], closed=False)

        leg = C([
            P(w,  mid_y),
            P(w * 0.8, 0),
        ], closed=False)

        return SkeletonGlyph(
            name="R", unicode_val=0x0052,
            width=round(w + self.stem * 2),
            contours=[left_stem, top_bump, leg],
            anchors={"knee": (w, mid_y)}
        )

    def generate_all(self) -> Dict[str, SkeletonGlyph]:
        return {
            "A": self.glyph_A(), "B": self.glyph_B(), "C": self.glyph_C(),
            "D": self.glyph_D(), "E": self.glyph_E(), "F": self.glyph_F(),
            "G": self.glyph_G(), "H": self.glyph_H(), "I": self.glyph_I(),
            "J": self.glyph_J(), "K": self.glyph_K(), "L": self.glyph_L(),
            "M": self.glyph_M(), "N": self.glyph_N(), "O": self.glyph_O(),
            "P": self.glyph_P(), "Q": self.glyph_Q(), "R": self.glyph_R(),
            "S": self.glyph_S(), "T": self.glyph_T(), "U": self.glyph_U(),
            "V": self.glyph_V(), "W": self.glyph_W(), "X": self.glyph_X(),
            "Y": self.glyph_Y(), "Z": self.glyph_Z(),
        }
