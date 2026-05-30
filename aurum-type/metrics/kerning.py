"""
Система кернинга AurumType.
Расстояние между буквами ∝ 1/φⁿ.
"""

import math
from itertools import product
from typing import Dict, Tuple, List

from math_core.constants import PHI, PHI_INV, STEM_REGULAR, KERN_BASE

KernPair = Tuple[str, str]
KernTable = Dict[KernPair, int]

GLYPH_CLASSES: Dict[str, List[str]] = {
    "open_right": ["A", "F", "P", "T", "V", "W", "Y"],
    "open_left":  ["C", "G", "S", "J"],
    "round":      ["O", "Q", "D", "C", "G"],
    "vertical":   ["H", "I", "J", "L", "M", "N", "U"],
    "diagonal":   ["A", "K", "M", "N", "V", "W", "X", "Y", "Z"],
}


def kern_distance(left_class: str, right_class: str, stem: int = STEM_REGULAR) -> int:
    base = KERN_BASE

    rules = {
        ("open_right", "open_left"): round(-base * PHI_INV),
        ("open_right", "round"):     round(-base * PHI_INV),
        ("open_right", "vertical"):  round(-base * PHI_INV ** 2),
        ("diagonal",   "diagonal"):  round(-base * PHI_INV ** 2),
        ("vertical",   "round"):     round(-base * PHI_INV ** 3),
        ("round",      "round"):     round(-base * PHI_INV ** 3),
        ("open_right", "diagonal"):  round(-base * PHI),
    }

    return rules.get((left_class, right_class), 0)


def build_kern_table(glyphs: List[str], stem: int = STEM_REGULAR) -> KernTable:
    table: KernTable = {}

    def find_classes(glyph: str) -> List[str]:
        return [cls for cls, members in GLYPH_CLASSES.items() if glyph in members]

    for left, right in product(glyphs, repeat=2):
        left_classes  = find_classes(left)
        right_classes = find_classes(right)

        best_kern = 0
        for lc in left_classes:
            for rc in right_classes:
                k = kern_distance(lc, rc, stem)
                if abs(k) > abs(best_kern):
                    best_kern = k

        if best_kern != 0:
            table[(left, right)] = best_kern

    return table


def sidebearing(glyph_class: str, stem: int = STEM_REGULAR) -> Tuple[int, int]:
    base_sb = round(stem / 2 * PHI)

    sidebearings = {
        "open_right": (base_sb, round(base_sb * PHI_INV ** 2)),
        "open_left":  (round(base_sb * PHI_INV ** 2), base_sb),
        "round":      (round(base_sb * PHI_INV), round(base_sb * PHI_INV)),
        "vertical":   (base_sb, base_sb),
        "diagonal":   (round(base_sb * PHI_INV ** 2), round(base_sb * PHI_INV ** 2)),
    }

    return sidebearings.get(glyph_class, (base_sb, base_sb))


class OpticalAligner:
    def __init__(self, target_area: float = None, stem: int = STEM_REGULAR):
        self.target_area = target_area or (stem * PHI) ** 2 / 2
        self.stem = stem

    def estimate_gap_area(self, lsb: int, rsb_prev: int, height: int) -> float:
        gap = lsb + rsb_prev
        return gap * height * PHI_INV

    def adjust_kern(self, current_kern: int, lsb: int, rsb_prev: int, height: int) -> int:
        current_area = self.estimate_gap_area(lsb, rsb_prev, height)
        ratio = self.target_area / current_area if current_area else 1.0
        correction = round(math.log(ratio) * self.stem * PHI_INV)
        return current_kern + correction
