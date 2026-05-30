"""
Система пропорций для всех классов глифов AurumType.
Ширина каждой буквы вычисляется через степени φ.
"""

from typing import Dict, Tuple, Optional
from dataclasses import dataclass

from .constants import PHI, PHI_INV, FIB, STEM_REGULAR, SERIF_LENGTH, SERIF_THICKNESS


@dataclass
class GlyphProportions:
    glyph: str
    width_class: str
    width: int
    stem: int
    serif: bool = True
    overshoot: bool = False
    crossbar_y: Optional[int] = None
    lsb: int = 0
    rsb: int = 0


# Ширины букв = round(stem * φ^n) где n от класса
CLASS_POWERS = {
    "wide":   2.5,    # M, W
    "full":   2.0,    # O, G, Q, D
    "normal": 1.5,    # H, N, U, A, V, X, Y
    "medium": 1.0,    # B, E, F, P, R, S, T, 2, 5, 8
    "narrow": 0.5,    # I, J, L
    "thin":   0.0,    # i, l, 1, :
}

CLASS_SIDEBEARINGS = {
    "open_right":  (1.0, PHI_INV ** 2),
    "open_left":   (PHI_INV ** 2, 1.0),
    "round":       (PHI_INV, PHI_INV),
    "vertical":    (1.0, 1.0),
    "diagonal":    (PHI_INV ** 2, PHI_INV ** 2),
}


def width_for_class(glyph_class: str, stem: int = STEM_REGULAR) -> int:
    power = CLASS_POWERS.get(glyph_class, 1.0)
    return round(stem * (PHI ** power))


def sidebearing_for_class(
    glyph_class: str, side: str = "both", stem: int = STEM_REGULAR
) -> Tuple[int, int]:
    base_sb = round(stem / 2 * PHI)
    factors = CLASS_SIDEBEARINGS.get(glyph_class, (1.0, 1.0))
    lsb = round(base_sb * factors[0])
    rsb = round(base_sb * factors[1])
    return (lsb, rsb)


GLYPH_WIDTH_TABLE = {
    "A": ("normal",   "diagonal",   70,  377),
    "B": ("medium",   "vertical",   55,  377),
    "C": ("full",     "round",      89,  None),
    "D": ("full",     "round",      89,  None),
    "E": ("medium",   "vertical",   55,  377),
    "F": ("medium",   "open_right", 55,  377),
    "G": ("full",     "round",      89,  377),
    "H": ("normal",   "vertical",   70,  377),
    "I": ("thin",     "vertical",   34,  None),
    "J": ("narrow",   "open_left",  43,  None),
    "K": ("normal",   "diagonal",   70,  377),
    "L": ("medium",   "vertical",   55,  None),
    "M": ("wide",     "vertical",   110, None),
    "N": ("normal",   "diagonal",   70,  None),
    "O": ("full",     "round",      89,  None),
    "P": ("medium",   "open_right", 55,  377),
    "Q": ("full",     "round",      89,  None),
    "R": ("medium",   "vertical",   63,  377),
    "S": ("medium",   "round",      55,  377),
    "T": ("medium",   "open_right", 55,  None),
    "U": ("normal",   "vertical",   70,  None),
    "V": ("normal",   "diagonal",   70,  None),
    "W": ("wide",     "diagonal",   110, None),
    "X": ("normal",   "diagonal",   70,  377),
    "Y": ("normal",   "diagonal",   70,  377),
    "Z": ("medium",   "diagonal",   63,  None),
    "0": ("full",     "round",      89,  None),
    "1": ("thin",     "vertical",   34,  None),
    "2": ("medium",   "round",      55,  None),
    "3": ("medium",   "round",      55,  377),
    "4": ("medium",   "vertical",   55,  None),
    "5": ("medium",   "round",      55,  None),
    "6": ("full",     "round",      89,  None),
    "7": ("medium",   "diagonal",   55,  None),
    "8": ("full",     "round",      89,  377),
    "9": ("full",     "round",      89,  None),
}
