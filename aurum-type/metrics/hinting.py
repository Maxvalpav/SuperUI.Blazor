"""
TrueType hinting для AurumType.
Автоматическая генерация инструкций для чёткого отображения на экране.
"""

from typing import List, Dict, Optional
from math_core.constants import PHI, FIB, UPM, STEM_REGULAR, CAP_HEIGHT, X_HEIGHT


class HintingEngine:
    def __init__(self, stem: int = STEM_REGULAR):
        self.stem = stem
        self.fib_stems = [s for s in FIB if s <= stem * 2]

    def generate_stem_hints(self, glyph_name: str) -> List[Dict]:
        hints = []

        if glyph_name in "HNTU":
            hints.append({
                "type": "vstem",
                "position": 0,
                "width": self.stem,
                "index": 0,
            })
            hints.append({
                "type": "vstem",
                "position": 0,
                "width": self.stem,
                "index": 1,
            })

        if glyph_name in "ABEFHPR":
            hints.append({
                "type": "hstem",
                "position": 0,
                "width": self.stem,
                "index": 0,
            })
            hints.append({
                "type": "hstem",
                "position": CAP_HEIGHT - self.stem,
                "width": self.stem,
                "index": 1,
            })

        if glyph_name in "OQDCG":
            hints.append({
                "type": "vstem",
                "position": 0,
                "width": self.stem,
                "index": 0,
            })

        return hints
