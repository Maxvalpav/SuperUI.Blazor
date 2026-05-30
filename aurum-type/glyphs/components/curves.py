"""
Сплайны на основе логарифмической спирали для построения дуг глифов.
"""

import math
from typing import List, Tuple, Callable
from math_core.constants import PHI, SPIRAL_B
from math_core.spiral import LogarithmicSpiral, GoldenSpiral, BezierCurve


class CurveBuilder:
    @staticmethod
    def golden_arc(
        x0: float, y0: float,
        x1: float, y1: float,
        rotation: float = 0,
        num_curves: int = 1
    ) -> List[BezierCurve]:
        w = x1 - x0
        h = y1 - y0
        cx = (x0 + x1) / 2
        cy = (y0 + y1) / 2
        scale = min(w, h) / PHI

        spiral = GoldenSpiral(scale=scale, center=(cx, cy))
        spiral.rotation = rotation
        t0, t1 = (0, math.pi / 2)
        return spiral.to_cubic_bezier(t0, t1, tolerance=0.25)

    @staticmethod
    def phi_bezier_handle(length: float, angle_deg: float) -> Tuple[float, float]:
        a = math.radians(angle_deg)
        handle = length * 0.5523 * PHI_INV
        return (handle * math.cos(a), handle * math.sin(a))

    @staticmethod
    def spiral_segment(
        start: Tuple[float, float],
        end: Tuple[float, float],
        curvature: float = 1.0
    ) -> List[BezierCurve]:
        dx = end[0] - start[0]
        dy = end[1] - start[1]
        dist = math.sqrt(dx**2 + dy**2)

        cx = (start[0] + end[0]) / 2
        cy = (start[1] + end[1]) / 2

        spiral = LogarithmicSpiral(
            a=dist * 0.5,
            center=(cx, cy),
            rotation=math.atan2(dy, dx)
        )

        return spiral.to_cubic_bezier(0, math.pi * curvature * 0.5, tolerance=0.5)

    @staticmethod
    def phi_oval(
        cx: float, cy: float,
        rx: float, ry: float
    ) -> List[BezierCurve]:
        kx = rx * 0.5523
        ky = ry * 0.5523
        return [
            ((cx, cy + ry), (cx + kx, cy + ry), (cx + rx, cy + ky), (cx + rx, cy)),
            ((cx + rx, cy), (cx + rx, cy - ky), (cx + kx, cy - ry), (cx, cy - ry)),
            ((cx, cy - ry), (cx - kx, cy - ry), (cx - rx, cy - ky), (cx - rx, cy)),
            ((cx - rx, cy), (cx - rx, cy + ky), (cx - kx, cy + ry), (cx, cy + ry)),
        ]
