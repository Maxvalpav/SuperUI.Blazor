"""
AurumType CLI — точка входа.

Использование:
  python cli.py generate --weight 400 --serifs
  python cli.py generate-all
  python cli.py variable
  python cli.py proof --weight 400 --text "AURUM"
  python cli.py inspect --math
"""

import click
from pathlib import Path

from math_core.constants import PHI, FIB, STEMS, ENTERPRISE_SCALE


@click.group()
@click.version_option("1.0.0", prog_name="AurumType")
def cli():
    """AurumType — шрифт по законам природы."""
    pass


@cli.command()
@click.option("--weight",  default=400, type=int,  help="Вес (100–900)")
@click.option("--serifs/--no-serifs", default=True, help="Засечки")
@click.option("--output",  default="output",        help="Директория вывода")
def generate(weight: int, serifs: bool, output: str) -> None:
    """Сгенерировать один вес шрифта."""
    from export.ufo_writer import generate_ufo
    from export.ttf_builder import compile_ttf

    out = Path(output)
    out.mkdir(parents=True, exist_ok=True)

    click.echo(f"Генерация AurumType {weight} (serifs={serifs})...")
    ufo_path = generate_ufo(output, weight=weight, has_serifs=serifs)
    ttf_path = compile_ttf(ufo_path, out / "ttf")
    click.echo(f"✓ Готово: {ttf_path}")


@cli.command("generate-all")
@click.option("--output", default="output", help="Директория вывода")
def generate_all(output: str) -> None:
    """Сгенерировать все 9 весов."""
    from export.ufo_writer import generate_ufo
    from export.ttf_builder import compile_ttf

    out = Path(output)
    weights = [100, 200, 300, 400, 500, 600, 700, 800, 900]

    with click.progressbar(weights, label="Генерация весов") as bar:
        for w in bar:
            ufo = generate_ufo(str(out / "sources"), weight=w)
            compile_ttf(ufo, out / "ttf")

    click.echo(f"\n✓ Все {len(weights)} весов сгенерированы в {out}/ttf/")


@cli.command()
@click.option("--output", default="output", help="Директория вывода")
def variable(output: str) -> None:
    """Собрать Variable Font с осями wght, wdth, opsz, CONT."""
    from export.variable import compile_variable_font
    out = Path(output)
    ttf = compile_variable_font(out)
    click.echo(f"✓ Variable font: {ttf}")


@cli.command()
@click.option("--weight",  default=400,       help="Вес")
@click.option("--text",    default="HAMBURGEVONS", help="Тестовый текст")
@click.option("--sizes",   default="8,12,16,24,32,48,64,96", help="Кегли через запятую")
@click.option("--output",  default="output/proof.png", help="Файл вывода")
def proof(weight: int, text: str, sizes: str, output: str) -> None:
    """Рендер proof-sheet (тестового листа)."""
    from tests.render_proof import render_proof_sheet
    size_list = [int(s) for s in sizes.split(",")]
    render_proof_sheet(
        font_weight=weight,
        text=text,
        sizes=size_list,
        output_path=Path(output)
    )
    click.echo(f"✓ Proof: {output}")


@cli.command()
@click.option("--math", "show_math", is_flag=True, help="Показать математические константы")
def inspect(show_math: bool) -> None:
    """Show font parameters."""
    if show_math:
        click.echo("\n=== AurumType Math Constants ===\n")
        click.echo(f"  phi (golden ratio)    = {PHI:.10f}")
        click.echo(f"  1/phi                = {1/PHI:.10f}")
        click.echo(f"  phi^2                = {PHI**2:.10f}")
        click.echo(f"  sqrt(phi)            = {PHI**0.5:.10f}")
        click.echo(f"\n  Fibonacci (F1-F16):")
        click.echo(f"  {FIB[:16]}")
        click.echo(f"\n  Stem widths (UPM={987}):")
        for w, s in STEMS.items():
            click.echo(f"    {w:>3} -> stem={s:>3} UPM units")
        click.echo(f"\n  Enterprise typographic scale (base=16px):")
        for pt in sorted(ENTERPRISE_SCALE):
            click.echo(f"    {pt:>8.2f} pt")


if __name__ == "__main__":
    cli()
