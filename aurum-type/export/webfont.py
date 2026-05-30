"""
Экспорт в WOFF2 для web-использования.
Конвертация TTF → WOFF2 через fontTools.
"""

from pathlib import Path
from fontTools.ttLib import TTFont


def compile_woff2(ttf_path: Path, output_dir: Path = None) -> Path:
    if output_dir is None:
        output_dir = ttf_path.parent

    output_dir.mkdir(parents=True, exist_ok=True)
    woff2_path = output_dir / (ttf_path.stem + ".woff2")

    font = TTFont(str(ttf_path))
    font.flavor = "woff2"
    font.save(str(woff2_path))

    print(f"[OK] WOFF2: {woff2_path}")
    return woff2_path


def compile_all_web_formats(ttf_path: Path, output_dir: Path = None) -> dict:
    if output_dir is None:
        output_dir = ttf_path.parent

    formats = {}

    woff2 = compile_woff2(ttf_path, output_dir)
    formats["woff2"] = woff2

    return formats


def generate_webfont_css(
    font_family: str = "AurumType",
    weights: list = None,
    base_path: str = "/fonts/"
) -> str:
    if weights is None:
        weights = [300, 400, 600, 700]

    css_lines = []
    for w in weights:
        style = {300: "Light", 400: "Regular", 600: "SemiBold", 700: "Bold"}.get(w, f"W{w}")
        css_lines.append(f"""
@font-face {{
    font-family: '{font_family}';
    src: url('{base_path}AurumType-{style}.woff2') format('woff2'),
         url('{base_path}AurumType-{style}.ttf') format('truetype');
    font-weight: {w};
    font-style: normal;
    font-display: swap;
}}""")

    return "\n".join(css_lines)
