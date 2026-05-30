"""
Логарифмическая спираль — основа всех скруглений в AurumType.
Любая дуга в шрифте является фрагментом логарифмической спирали с b=SPIRAL_B.
"""

import math
import numpy as np
from typing import List, Tuple

from .constants import PHI, SPIRAL_B

Point = Tuple[float, float]
BezierCurve = Tuple[Point, Point, Point, Point]


class LogarithmicSpiral:
    def __init__(
        self,
        a: float = 1.0,
        b: float = SPIRAL_B,
        center: Point = (0.0, 0.0),
        rotation: float = 0.0
    ):
        self.a = a
        self.b = b
        self.cx, self.cy = center
        self.rotation = rotation

    def radius(self, theta: float) -> float:
        return self.a * math.exp(self.b * theta)

    def point(self, theta: float) -> Point:
        r = self.radius(theta)
        angle = theta + self.rotation
        x = self.cx + r * math.cos(angle)
        y = self.cy + r * math.sin(angle)
        return (x, y)

    def tangent_angle(self, theta: float) -> float:
        psi = math.atan(1.0 / self.b)
        return theta + self.rotation + math.pi / 2 - psi

    def arc_points(self, theta_start: float, theta_end: float, n: int = 64) -> List[Point]:
        thetas = np.linspace(theta_start, theta_end, n)
        return [self.point(float(t)) for t in thetas]

    def fit_to_bbox(
        self,
        x0: float, y0: float,
        x1: float, y1: float,
        theta_start: float,
        theta_end: float
    ) -> "LogarithmicSpiral":
        pts = self.arc_points(theta_start, theta_end, 256)
        xs = [p[0] for p in pts]
        ys = [p[1] for p in pts]
        cur_w = max(xs) - min(xs)
        cur_h = max(ys) - min(ys)
        target_w = x1 - x0
        target_h = y1 - y0

        scale = min(target_w / cur_w if cur_w else 1,
                    target_h / cur_h if cur_h else 1)

        new_a = self.a * scale
        new_cx = x0 + target_w / 2
        new_cy = y0 + target_h / 2

        return LogarithmicSpiral(new_a, self.b, (new_cx, new_cy), self.rotation)

    def to_cubic_bezier(
        self,
        theta_start: float,
        theta_end: float,
        tolerance: float = 0.5
    ) -> List[BezierCurve]:
        curves = []
        self._recursive_bezier(theta_start, theta_end, tolerance, curves, depth=0)
        return curves

    def _recursive_bezier(
        self,
        t0: float, t1: float,
        tolerance: float,
        curves: List[BezierCurve],
        depth: int
    ):
        if depth > 10:
            curves.append(self._segment_to_bezier(t0, t1))
            return

        mid = (t0 + t1) / 2
        bez = self._segment_to_bezier(t0, t1)
        mid_curve = self._bezier_point(bez, 0.5)
        mid_spiral = self.point(mid)
        err = math.dist(mid_curve, mid_spiral)

        if err <= tolerance:
            curves.append(bez)
        else:
            self._recursive_bezier(t0, mid, tolerance, curves, depth + 1)
            self._recursive_bezier(mid, t1, tolerance, curves, depth + 1)

    def _segment_to_bezier(self, t0: float, t1: float) -> BezierCurve:
        p0 = self.point(t0)
        p3 = self.point(t1)

        chord = math.dist(p0, p3)
        alpha = chord * (2 / 3) * math.tan((t1 - t0) / 4)

        a0 = self.tangent_angle(t0)
        a3 = self.tangent_angle(t1)

        p1 = (p0[0] + alpha * math.cos(a0),
              p0[1] + alpha * math.sin(a0))
        p2 = (p3[0] - alpha * math.cos(a3),
              p3[1] - alpha * math.sin(a3))

        return (p0, p1, p2, p3)

    @staticmethod
    def _bezier_point(bez: BezierCurve, t: float) -> Point:
        p0, p1, p2, p3 = bez
        u = 1 - t
        x = u**3*p0[0] + 3*u**2*t*p1[0] + 3*u*t**2*p2[0] + t**3*p3[0]
        y = u**3*p0[1] + 3*u**2*t*p1[1] + 3*u*t**2*p2[1] + t**3*p3[1]
        return (x, y)


class GoldenSpiral(LogarithmicSpiral):
    def __init__(self, scale: float = 1.0, center: Point = (0.0, 0.0)):
        super().__init__(a=scale, b=SPIRAL_B, center=center)

    def quarter_arc(self, quadrant: int) -> Tuple[float, float]:
        t0 = quadrant * math.pi / 2
        t1 = (quadrant + 1) * math.pi / 2
        return (t0, t1)

    def rectangle_for_quadrant(self, quadrant: int) -> Tuple[float, float, float, float]:
        r = self.radius(quadrant * math.pi / 2)
        r_next = self.radius((quadrant + 1) * math.pi / 2)
        return (0, 0, r_next, r)


def corner_curve(
    width: float,
    height: float,
    corner: str = "top-right"
) -> List[BezierCurve]:
    spiral = GoldenSpiral(scale=min(width, height) / PHI)

    rotations = {
        "top-right":    0,
        "top-left":     1,
        "bottom-left":  2,
        "bottom-right": 3,
    }
    rot_quarters = rotations.get(corner, 0)
    rotation_rad = rot_quarters * math.pi / 2

    spiral.rotation = rotation_rad + math.pi
    t0, t1 = spiral.quarter_arc(0)
    return spiral.to_cubic_bezier(t0, t1, tolerance=0.25)
