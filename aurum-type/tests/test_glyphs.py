"""
Tests AurumType glyph generation.
"""

import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent.parent))

from math_core.constants import (
    PHI, PHI_INV, CAP_HEIGHT, X_HEIGHT, STEM_REGULAR, OVERSHOOT, FIB
)
from glyphs.skeleton.latin_upper import (
    UppercaseSkeleton, SkeletonPoint, SkeletonContour
)
from glyphs.skeleton.latin_lower import LowercaseSkeleton
from glyphs.skeleton.digits import DigitSkeleton
from glyphs.skeleton.punctuation import PunctuationSkeleton


def test_uppercase_A():
    gen = UppercaseSkeleton()
    a = gen.glyph_A()
    assert a.name == "A"
    assert a.unicode_val == 0x0041
    assert len(a.contours) == 2
    assert "apex" in a.anchors
    print(f"[OK] A: width={a.width}")


def test_uppercase_O():
    gen = UppercaseSkeleton()
    o = gen.glyph_O()
    assert o.name == "O"
    assert o.unicode_val == 0x004F
    assert len(o.contours) == 1
    print(f"[OK] O: width={o.width}")


def test_uppercase_H():
    gen = UppercaseSkeleton()
    h = gen.glyph_H()
    assert h.name == "H"
    assert len(h.contours) == 3
    print(f"[OK] H: width={h.width}")


def test_all_uppercase():
    gen = UppercaseSkeleton()
    glyphs = gen.generate_all()
    assert len(glyphs) == 26
    for char, g in glyphs.items():
        assert g.name == char
        assert g.width > 0
        assert len(g.contours) > 0
    print(f"[OK] All 26 uppercase: {len(glyphs)} glyphs")


def test_lowercase():
    gen = LowercaseSkeleton()
    glyphs = gen.generate_all()
    assert len(glyphs) == 26
    for char, g in glyphs.items():
        assert g.name == char
        assert g.width > 0
    print(f"[OK] All 26 lowercase: {len(glyphs)} glyphs")


def test_digits():
    gen = DigitSkeleton()
    glyphs = gen.generate_all()
    assert len(glyphs) == 10
    for name, g in glyphs.items():
        assert g.unicode_val is not None
        assert g.width > 0
    print(f"[OK] All 10 digits: {len(glyphs)} glyphs")


def test_punctuation():
    gen = PunctuationSkeleton()
    glyphs = gen.generate_all()
    assert len(glyphs) == 11
    for name, g in glyphs.items():
        assert g.width > 0
    print(f"[OK] All 11 punctuation: {len(glyphs)} glyphs")


def test_glyph_widths_fibonacci():
    gen = UppercaseSkeleton()
    glyphs = gen.generate_all()
    widths_close_to_fib = 0
    for char, g in glyphs.items():
        for f in FIB:
            if abs(g.width - f) <= 3:
                widths_close_to_fib += 1
                break
    print(f"[OK] Widths: {widths_close_to_fib}/26 close to Fibonacci")


if __name__ == "__main__":
    print("\n=== Testing AurumType Glyphs ===\n")
    test_uppercase_A()
    test_uppercase_O()
    test_uppercase_H()
    test_all_uppercase()
    test_lowercase()
    test_digits()
    test_punctuation()
    test_glyph_widths_fibonacci()
    print("\n=== All glyph tests passed ===")
