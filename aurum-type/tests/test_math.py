"""
Tests AurumType math core.
"""

import math
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent.parent))

from math_core.constants import (
    PHI, PHI_INV, FIB, UPM, CAP_HEIGHT, X_HEIGHT, OVERSHOOT,
    STEM_REGULAR, STEMS, fibonacci_sequence, nearest_fibonacci, phi_scale
)
from math_core.spiral import LogarithmicSpiral, GoldenSpiral


def test_golden_ratio():
    assert abs(PHI - 1.6180339887) < 1e-8
    assert abs(PHI_INV * PHI - 1.0) < 1e-10
    print("[OK] Golden ratio")


def test_fibonacci():
    fib = fibonacci_sequence(16)
    assert fib[0] == 1
    assert fib[1] == 1
    assert fib[6] == 13
    assert fib[15] == 987
    assert UPM == 987
    assert CAP_HEIGHT == 610
    assert X_HEIGHT == 377
    print(f"[OK] Fibonacci F1-F16")


def test_stems():
    assert STEM_REGULAR == 34
    for w in [100, 200, 300, 400, 500, 600, 700, 800, 900]:
        assert STEMS[w] in FIB, f"Stem for {w} not in FIB: {STEMS[w]}"
    print(f"[OK] All stems are Fibonacci")


def test_spiral():
    from math_core.constants import STEMS
    s = LogarithmicSpiral(a=1.0)
    r0 = s.radius(0)
    r90 = s.radius(math.pi / 2)
    ratio = r90 / r0
    assert abs(ratio - PHI) < 1e-4
    print(f"[OK] Logarithmic spiral (r90/r0={ratio:.4f})")


def test_golden_spiral():
    gs = GoldenSpiral(scale=10)
    t0, t1 = gs.quarter_arc(0)
    assert abs(t1 - t0 - math.pi / 2) < 1e-10
    print(f"[OK] Golden spiral quarter arc")


def test_phi_scale():
    scale = phi_scale(16, 4)
    assert len(scale) == 9
    assert abs(scale[4] - 16.0) < 0.01
    assert scale[5] > scale[4]
    assert scale[3] < scale[4]
    print(f"[OK] Phi typographic scale: {len(scale)} steps")


def test_nearest_fib():
    assert nearest_fibonacci(35) == 34
    assert nearest_fibonacci(50) == 55
    assert nearest_fibonacci(0) == 1
    print(f"[OK] nearest_fibonacci")


def test_overshoot():
    assert OVERSHOOT == 13
    assert OVERSHOOT in FIB
    print(f"[OK] Overshoot = F[7] = {OVERSHOOT}")


def test_all_vertical_metrics():
    from math_core.constants import ASCENDER, DESCENDER
    metrics = [UPM, ASCENDER, CAP_HEIGHT, X_HEIGHT, abs(DESCENDER), OVERSHOOT]
    for m in metrics:
        assert m in FIB, f"{m} is not Fibonacci"
    print(f"[OK] All vertical metrics are Fibonacci")


def test_bezier_curve():
    gs = GoldenSpiral(scale=5)
    curves = gs.to_cubic_bezier(0, math.pi, tolerance=0.5)
    assert len(curves) >= 1
    p0, p1, p2, p3 = curves[0]
    assert len(p0) == 2
    print(f"[OK] Bezier approx: {len(curves)} segments")


def test_corner_curve():
    from math_core.spiral import corner_curve
    curves = corner_curve(100, 200, corner="top-right")
    assert len(curves) >= 1
    print(f"[OK] corner_curve: {len(curves)} segments")


if __name__ == "__main__":
    print("\n=== Testing AurumType Math Core ===\n")
    test_golden_ratio()
    test_fibonacci()
    test_stems()
    test_spiral()
    test_golden_spiral()
    test_phi_scale()
    test_nearest_fib()
    test_overshoot()
    test_all_vertical_metrics()
    test_bezier_curve()
    test_corner_curve()
    print("\n=== All math tests passed ===")
