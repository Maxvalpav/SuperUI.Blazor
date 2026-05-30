"""
OutlineBuilder — превращает скелет (центральную линию) в полноценные контуры,
добавляя толщину штриха, засечки, терминалы и оптические коррекции.
"""

import math
from typing import List, Tuple

from math_core.constants import (
    PHI, PHI_INV, STEM_REGULAR, SERIF_LENGTH, SERIF_THICKNESS, SERIF_BRACKET,
    CAP_HEIGHT, OVERSHOOT,
)
from glyphs.skeleton.latin_upper import (
    SkeletonGlyph, SkeletonPoint, SkeletonContour
)

Point = Tuple[float, float]


def offset_point(pt: Point, angle: float, dist: float) -> Point:
    return (pt[0] + dist * math.cos(angle),
            pt[1] + dist * math.sin(angle))


def perp_angle(p1: Point, p2: Point, side: str = "left") -> float:
    dx, dy = p2[0] - p1[0], p2[1] - p1[1]
    angle = math.atan2(dy, dx)
    return angle + (math.pi / 2 if side == "left" else -math.pi / 2)


def stroke_segment(p1: Point, p2: Point, half_w: float) -> Tuple[Point, Point, Point, Point]:
    langle = perp_angle(p1, p2, "left")
    rangle = perp_angle(p1, p2, "right")
    return (
        offset_point(p1, langle, half_w),
        offset_point(p2, langle, half_w),
        offset_point(p2, rangle, half_w),
        offset_point(p1, rangle, half_w),
    )


class SerifBuilder:
    def __init__(self, stem: int = STEM_REGULAR):
        self.stem = stem
        self.half = stem / 2
        self.serif_len = SERIF_LENGTH
        self.serif_thick = SERIF_THICKNESS
        self.bracket_r = SERIF_BRACKET

    def bottom_left_serif(self, x: float, y: float) -> List:
        r = self.bracket_r
        serif_x0 = x - self.serif_len
        serif_x1 = x + self.serif_thick
        serif_y = y - self.serif_thick

        cp1 = (x, y - r * 0.5523)
        cp2 = (serif_x0 + r * 0.5523, serif_y)

        return [
            ("moveTo", (serif_x0, y)),
            ("curveTo", cp1, cp2, (serif_x0, serif_y)),
            ("lineTo",  (serif_x1, serif_y)),
            ("lineTo",  (serif_x1, y)),
            ("closePath", None),
        ]

    def bottom_right_serif(self, x: float, y: float) -> List:
        r = self.bracket_r
        serif_x0 = x - self.serif_thick
        serif_x1 = x + self.serif_len
        serif_y = y - self.serif_thick

        cp1 = (x, y - r * 0.5523)
        cp2 = (serif_x1 - r * 0.5523, serif_y)

        return [
            ("moveTo", (serif_x1, y)),
            ("curveTo", cp1, cp2, (serif_x1, serif_y)),
            ("lineTo",  (serif_x0, serif_y)),
            ("lineTo",  (serif_x0, y)),
            ("closePath", None),
        ]

    def top_serif(self, x: float, y: float, direction: str = "both") -> List:
        r = self.bracket_r
        half_s = self.stem / 2
        serif_y1 = y + self.serif_thick

        if direction == "both":
            x0 = x - self.serif_len - half_s
            x1 = x + self.serif_len + half_s
        elif direction == "left":
            x0 = x - self.serif_len
            x1 = x + self.serif_thick
        else:
            x0 = x - self.serif_thick
            x1 = x + self.serif_len

        return [
            ("moveTo",   (x0, y)),
            ("curveTo",  (x0, y + r * 0.5523),
                         (x - half_s - r * 0.5523, serif_y1),
                         (x - half_s, serif_y1)),
            ("lineTo",   (x + half_s, serif_y1)),
            ("curveTo",  (x + half_s + r * 0.5523, serif_y1),
                         (x1, y + r * 0.5523),
                         (x1, y)),
            ("closePath", None),
        ]


class OutlineBuilder:
    def __init__(
        self,
        stem: int = STEM_REGULAR,
        has_serifs: bool = True,
        weight: int = 400
    ):
        self.stem = stem
        self.half = stem / 2
        self.has_serifs = has_serifs
        self.weight = weight
        self.serif = SerifBuilder(stem)

    def build(self, skeleton: SkeletonGlyph, pen) -> None:
        for contour in skeleton.contours:
            self._draw_contour(contour, pen)

    def _draw_contour(self, contour: SkeletonContour, pen) -> None:
        pts = contour.points
        if not pts:
            return

        if len(pts) == 2 and not contour.closed:
            p1 = (pts[0].x, pts[0].y)
            p2 = (pts[1].x, pts[1].y)
            self._draw_stroke(p1, p2, pen)
            return

        pen.moveTo((pts[0].x, pts[0].y))
        for pt in pts[1:]:
            if pt.type == "curve":
                pen.curveTo((pt.x - 5, pt.y), (pt.x, pt.y - 5), (pt.x, pt.y))
            else:
                pen.lineTo((pt.x, pt.y))

        if contour.closed:
            pen.closePath()
        else:
            pen.endPath()

    def _draw_stroke(self, p1: Point, p2: Point, pen) -> None:
        tl, tr, br, bl = stroke_segment(p1, p2, self.half)
        pen.moveTo(tl)
        pen.lineTo(tr)
        pen.lineTo(br)
        pen.lineTo(bl)
        pen.closePath()

    def optical_correct_y(self, y: float, ref: float) -> float:
        if abs(y - ref) < OVERSHOOT * 2:
            direction = 1 if y >= ref else -1
            return y + direction * OVERSHOOT
        return y
