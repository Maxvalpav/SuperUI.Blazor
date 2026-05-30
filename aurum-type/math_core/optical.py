"""
Оптические коррекции для AurumType.
Компенсация иллюзий восприятия через математические законы.
"""

import math
from typing import Tuple, Optional

from .constants import (
    PHI, PHI_INV, FIB,
    CAP_HEIGHT, X_HEIGHT, OVERSHOOT, STEM_REGULAR,
    UPM, ASCENDER, DESCENDER,
)


class OpticalCorrector:
    def __init__(self, stem: int = STEM_REGULAR, upm: int = UPM):
        self.stem = stem
        self.upm = upm

    def overshoot_y(self, y: float, ref_line: float, is_round: bool = True) -> float:
        if not is_round:
            return y
        direction = 1 if y >= ref_line else -1
        return y + direction * OVERSHOOT

    def correct_diagonal_weight(self, angle_rad: float) -> float:
        cos_a = abs(math.cos(angle_rad))
        if cos_a < 0.01:
            return 1.0
        return min(1.0 / cos_a, PHI)

    def mueller_lyer_correction(self, length: float) -> float:
        return length * (1 + PHI_INV * 0.05)

    @staticmethod
    def optical_size_adjustment(size_pt: float) -> float:
        if size_pt <= 0:
            return 1.0
        if size_pt < 14:
            return 0.95 + 0.05 * (size_pt / 14)
        if size_pt > 48:
            return 1.0 + 0.03 * math.log(size_pt / 48)
        return 1.0

    def correct_stem(self, weight: int, size_pt: float = 14) -> int:
        from .constants import STEMS
        base = STEMS.get(weight, STEM_REGULAR)
        adj = self.optical_size_adjustment(size_pt)
        result = round(base * adj)
        return min(FIB, key=lambda f: abs(f - result))

    @staticmethod
    def white_space_balance(gap: float, height: float) -> float:
        target_ratio = PHI_INV
        actual_ratio = gap / height if height else 0
        if actual_ratio == 0:
            return gap
        return gap * (target_ratio / actual_ratio)
