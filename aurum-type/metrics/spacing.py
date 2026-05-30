"""
Система межбуквенных интервалов AurumType.
Базовый принцип: визуально равномерная площадь межбуквенного пространства.
"""

import math
from typing import Dict, Tuple, Optional
from math_core.constants import PHI, PHI_INV, STEM_REGULAR, CAP_HEIGHT, X_HEIGHT


class SpacingEngine:
    def __init__(self, stem: int = STEM_REGULAR, cap_height: int = CAP_HEIGHT):
        self.stem = stem
        self.cap_height = cap_height
        self.base_sb = round(stem / 2 * PHI)

    def sidebearings(self, glyph_name: str, uppercase: bool = True) -> Tuple[int, int]:
        h = self.cap_height if uppercase else X_HEIGHT
        base = self.base_sb

        # Модификаторы по форме буквы
        modifiers = {
            "round":  (PHI_INV, PHI_INV),
            "open":   (1.0, PHI_INV ** 2),
            "closed": (PHI_INV ** 2, 1.0),
            "narrow": (PHI_INV, PHI_INV),
            "wide":   (PHI * 0.5, PHI * 0.5),
        }

        # Определяем форму по имени глифа
        if glyph_name in "OCGDQ0":
            form = "round"
        elif glyph_name in "AFLPTVWY":
            form = "open"
        elif glyph_name in "BEKMNRSUXZ23456789":
            form = "closed"
        elif glyph_name in "IJ1":
            form = "narrow"
        elif glyph_name in "MW":
            form = "wide"
        else:
            form = "closed"

        lf, rf = modifiers.get(form, (1.0, 1.0))
        lsb = round(base * lf)
        rsb = round(base * rf)

        return (lsb, rsb)

    def optical_correction(self, glyph_name: str, size_pt: float = 16) -> int:
        r = self.stem * 0.1
        if size_pt < 10:
            r *= 1.3
        elif size_pt > 48:
            r *= 0.7
        return round(r)

    def spacing_for_pair(
        self, left: str, right: str,
        uppercase: bool = True
    ) -> int:
        lsb_l, _ = self.sidebearings(left, uppercase)
        _, rsb_r = self.sidebearings(right, uppercase)
        return lsb_l + rsb_r
