"""
Терминалы — концы штрихов на основе логарифмической спирали.
"""

import math
from typing import List, Tuple
from math_core.constants import PHI, STEM_REGULAR, SERIF_LENGTH
from math_core.spiral import LogarithmicSpiral


class TerminalBuilder:
    def __init__(self, stem: int = STEM_REGULAR):
        self.stem = stem
        self.half = stem / 2

    def ball_terminal(self, x: float, y: float, angle: float, radius: float = None) -> List[Tuple]:
        r = radius or (self.stem * 0.6)
        cx = x + r * math.cos(angle)
        cy = y + r * math.sin(angle)
        k = r * 0.5523

        return [
            ("moveTo", (cx, cy + r)),
            ("curveTo", (cx + k, cy + r), (cx + r, cy + k), (cx + r, cy)),
            ("curveTo", (cx + r, cy - k), (cx + k, cy - r), (cx, cy - r)),
            ("curveTo", (cx - k, cy - r), (cx - r, cy - k), (cx - r, cy)),
            ("curveTo", (cx - r, cy + k), (cx - k, cy + r), (cx, cy + r)),
        ]

    def teardrop_terminal(self, x: float, y: float, angle: float) -> List[Tuple]:
        r = self.stem * 0.5
        cx = x + r * 2 * math.cos(angle)
        cy = y + r * 2 * math.sin(angle)
        k = r * 0.4142

        return [
            ("moveTo", (x, y)),
            ("curveTo", (x + k, y), (cx + r, cy), (cx, cy + r)),
            ("curveTo", (cx - r, cy), (x - k, y), (x, y)),
        ]

    def spiral_terminal(self, x: float, y: float, angle: float, size: float = None) -> List[Tuple]:
        s = size or self.stem
        spiral = LogarithmicSpiral(
            a=s * 0.1,
            center=(x, y),
            rotation=angle
        )
        bezier_curves = spiral.to_cubic_bezier(0, math.pi, tolerance=0.1)
        commands = []
        for i, curve in enumerate(bezier_curves):
            if i == 0:
                commands.append(("moveTo", curve[0]))
            commands.append(("curveTo", curve[1], curve[2], curve[3]))
        return commands
