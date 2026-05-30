"""
Генерация Variable Font для AurumType.
Оси: wght, wdth, opsz, CONT.
"""

import math
from pathlib import Path
from typing import List, Dict, Tuple

from fontTools import varLib, designspaceLib
from fontTools.ttLib import TTFont

from math_core.constants import PHI, PHI_INV, STEMS
from export.ufo_writer import generate_ufo

VARIABLE_AXES = [
    {
        "tag":     "wght",
        "name":    "Weight",
        "minimum": 100,
        "default": 400,
        "maximum": 900,
        "masters": [100, 200, 300, 400, 500, 600, 700, 800, 900],
    },
    {
        "tag":     "wdth",
        "name":    "Width",
        "minimum": 75,
        "default": 100,
        "maximum": 125,
        "masters": [75, 100, 125],
    },
    {
        "tag":     "opsz",
        "name":    "Optical Size",
        "minimum": 6,
        "default": 14,
        "maximum": 144,
        "masters": [6, 10, 16, 26, 42, 68, 110],
    },
    {
        "tag":     "CONT",
        "name":    "Contrast",
        "minimum": 0,
        "default": 50,
        "maximum": 100,
        "masters": [0, 50, 100],
    },
]


def phi_interpolate(t: float, v_min: float, v_max: float) -> float:
    t_curved = (math.exp(t * math.log(PHI + 1)) - 1) / PHI
    return v_min + (v_max - v_min) * min(t_curved, 1.0)


def stem_for_weight(weight: float) -> float:
    masters = [
        (100, 13), (200, 21), (300, 21), (400, 34),
        (500, 34), (600, 55), (700, 55), (800, 89), (900, 89),
    ]

    if weight <= masters[0][0]:
        return masters[0][1]
    if weight >= masters[-1][0]:
        return masters[-1][1]

    for i in range(len(masters) - 1):
        w0, s0 = masters[i]
        w1, s1 = masters[i + 1]
        if w0 <= weight <= w1:
            t = (weight - w0) / (w1 - w0)
            return phi_interpolate(t, s0, s1)

    return 34


def generate_designspace(output_dir: Path) -> Path:
    ds = designspaceLib.DesignSpaceDocument()

    for axis_def in VARIABLE_AXES:
        axis = designspaceLib.AxisDescriptor()
        axis.tag     = axis_def["tag"]
        axis.name    = axis_def["name"]
        axis.minimum = axis_def["minimum"]
        axis.default = axis_def["default"]
        axis.maximum = axis_def["maximum"]
        if axis_def["tag"] == "wght":
            axis.map = _build_phi_axis_map(
                axis_def["minimum"], axis_def["maximum"], axis_def["masters"]
            )
        ds.addAxis(axis)

    weight_masters = [100, 400, 700, 900]
    for w in weight_masters:
        ufo_path = generate_ufo(str(output_dir / "sources"), weight=w)

        master = designspaceLib.SourceDescriptor()
        master.path     = str(ufo_path)
        master.familyName = "AurumType"
        master.styleName  = f"W{w}"
        master.location   = {
            "Weight": w, "Width": 100, "Optical Size": 14, "Contrast": 50
        }

        if w == 400:
            master.copyInfo    = True
            master.copyGroups  = True
            master.copyLib     = True
            master.copyFeatures = True

        ds.addSource(master)

    ds_path = output_dir / "AurumType.designspace"
    ds.write(str(ds_path))
    print(f"[OK] Designspace: {ds_path}")
    return ds_path


def _build_phi_axis_map(
    minimum: float, maximum: float, masters: List[float]
) -> List[Tuple[float, float]]:
    mapping = []
    n = len(masters)
    for i, master_val in enumerate(masters):
        t = i / (n - 1)
        internal = minimum + (maximum - minimum) * t
        mapping.append((master_val, internal))
    return mapping


def compile_variable_font(output_dir: Path) -> Path:
    ds_path = generate_designspace(output_dir)

    font, _, _ = varLib.build(str(ds_path))
    out_path = output_dir / "AurumType[wght,wdth,opsz,CONT].ttf"
    font.save(str(out_path))

    print(f"[OK] Variable font: {out_path}")
    return out_path
