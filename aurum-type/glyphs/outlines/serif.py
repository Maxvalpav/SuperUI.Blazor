"""
Генерация засечек (serifs) по математическим законам AurumType.

Длина засечки = F₇ = 13
Толщина       = F₆ = 8
Скобка        = φ × 8 ≈ 13
"""

import math
from typing import List, Tuple
from math_core.constants import PHI, SERIF_LENGTH, SERIF_THICKNESS, SERIF_BRACKET


def generate_serif(
    x: float, y: float,
    angle: float,
    length: int = SERIF_LENGTH,
    thickness: int = SERIF_THICKNESS,
    bracket_radius: float = SERIF_BRACKET,
    side: str = "left"
) -> List[Tuple]:
    flip = -1 if side == "right" else 1
    cos_a = math.cos(angle)
    sin_a = math.sin(angle)

    serif_end_x = x + flip * length * cos_a
    serif_end_y = y + flip * length * sin_a

    cp1 = (x + flip * bracket_radius * 0.5523 * cos_a,
           y + flip * bracket_radius * 0.5523 * sin_a)
    cp2 = (serif_end_x + flip * bracket_radius * 0.5523 * cos_a,
           serif_end_y + flip * bracket_radius * 0.5523 * sin_a)

    return [
        ("moveTo", (x, y)),
        ("curveTo", cp1, cp2, (serif_end_x, serif_end_y)),
    ]
