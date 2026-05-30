"""
AurumType — Математические константы.
Все пропорции шрифта выводятся из этих фундаментальных чисел.
"""

import math
from typing import List


# =============================================================================
# ЗОЛОТОЕ СЕЧЕНИЕ
# =============================================================================

PHI: float = (1 + math.sqrt(5)) / 2          # φ ≈ 1.6180339887
PHI_INV: float = 1 / PHI                       # 1/φ = φ - 1 ≈ 0.6180
PHI_SQ: float = PHI ** 2                       # φ² ≈ 2.6180
PHI_SQRT: float = math.sqrt(PHI)               # √φ ≈ 1.2720

# =============================================================================
# ЧИСЛА ФИБОНАЧЧИ
# =============================================================================

def fibonacci_sequence(n: int) -> List[int]:
    fib = [1, 1]
    for _ in range(2, n):
        fib.append(fib[-1] + fib[-2])
    return fib

FIB: List[int] = fibonacci_sequence(25)

def nearest_fibonacci(value: float) -> int:
    return min(FIB, key=lambda f: abs(f - value))

def fibonacci_ratio(a: int, b: int) -> float:
    return FIB[b] / FIB[a]

# =============================================================================
# UPM И БАЗОВЫЕ МЕТРИКИ (ВСЕ — ЧИСЛА ФИБОНАЧЧИ)
# =============================================================================

UPM: int = 987

ASCENDER: int       =  987
CAP_HEIGHT: int     =  610
X_HEIGHT: int       =  377
BASELINE: int       =    0
DESCENDER: int      = -233

OVERSHOOT: int      =   13

STEM_THIN: int      =   13
STEM_EXTRALIGHT: int = 21
STEM_LIGHT: int     =   21
STEM_REGULAR: int   =   34
STEM_MEDIUM: int    =   34
STEM_SEMIBOLD: int  =   55
STEM_BOLD: int      =   55
STEM_EXTRABOLD: int =   89
STEM_BLACK: int     =   89

STEMS = {
    100: STEM_THIN,
    200: STEM_EXTRALIGHT,
    300: STEM_LIGHT,
    400: STEM_REGULAR,
    500: STEM_MEDIUM,
    600: STEM_SEMIBOLD,
    700: STEM_BOLD,
    800: STEM_EXTRABOLD,
    900: STEM_BLACK,
}

SERIF_LENGTH: int    = 13
SERIF_THICKNESS: int =  8
SERIF_BRACKET: float = 5 * PHI

KERN_BASE: int = 21

# =============================================================================
# ЛОГАРИФМИЧЕСКАЯ СПИРАЛЬ
# =============================================================================

SPIRAL_B: float = math.log(PHI) / (math.pi / 2)

# =============================================================================
# УГЛЫ (ПЯТИУГОЛЬНИК, ЗОЛОТОЙ ТРЕУГОЛЬНИК)
# =============================================================================

ANGLE_36: float  = math.radians(36)
ANGLE_72: float  = math.radians(72)
ANGLE_108: float = math.radians(108)

DIAGONAL_ANGLE: float = math.atan(CAP_HEIGHT / (CAP_HEIGHT * PHI_INV))

# =============================================================================
# ОПТИЧЕСКИЕ ПОПРАВКИ
# =============================================================================

def optical_correction(size_pt: float) -> float:
    if size_pt <= 0:
        return 1.0
    return 1.0 + math.log(size_pt / 14.0) * 0.05

def harmonic_weight(n: int, base: float = STEM_REGULAR) -> float:
    return base / n

# =============================================================================
# ПРОГРЕССИЯ ФИ ДЛЯ ТИПОГРАФСКОЙ ШКАЛЫ
# =============================================================================

def phi_scale(base_pt: float, steps: int) -> List[float]:
    scale = []
    for i in range(-steps, steps + 1):
        scale.append(round(base_pt * (PHI ** i), 2))
    return scale

ENTERPRISE_SCALE = phi_scale(16.0, 6)
