# AurumType — Корпоративный шрифт на основе законов природы

**Философия:** Каждая буква — не произвольная форма, а математическая необходимость. Золотое сечение, спираль Фибоначчи, логарифмические кривые, гармонические пропорции — всё это зашито в скелет каждого глифа.

## Математическая база

| Константа | Значение | Применение |
|-----------|----------|------------|
| φ | 1.6180339887... | Соотношения высот, ширин, толщин |
| F₁₆ | 987 | UPM (Units Per Em) |
| F₁₅ | 610 | Cap Height |
| F₁₄ | 377 | x-height |
| F₁₃ | −233 | Descender |
| F₉ | 34 | Stem Regular |
| F₇ | 13 | Overshoot, Serif length |

## Быстрый старт

```bash
cd aurum-type

# Установка зависимостей
pip install -r requirements.txt

# Генерация Regular веса
python cli.py generate --weight 400

# Генерация всех весов
python cli.py generate-all

# Генерация Variable Font
python cli.py variable

# Тестовый лист
python cli.py proof --text "AURUMTYPE"

# Инспекция констант
python cli.py inspect --math
```

## Архитектура

```
aurum-type/
  math_core/    # Математические константы, спирали, пропорции
  glyphs/       # Скелеты и контуры глифов (A-Z, a-z, 0-9, пунктуация)
  metrics/      # Кернинг, интервалы, хинтинг
  export/       # UFO, TTF, Variable Font, WOFF2
  tests/        # Юнит-тесты и proof-sheet
  cli.py        # Точка входа CLI
```

## Интеграция с CSS

```css
@import url('aurum-type/output/webfonts/aurumtype.css');

:root {
  --sg-font: 'AurumType', 'Inter', system-ui, sans-serif;
}
```

## Оси Variable Font

- **wght** (100–900): вес по числам Фибоначчи
- **wdth** (75–125): ширина по φ-шкале
- **opsz** (6–144): оптический размер по логарифму
- **CONT** (0–100): контраст по гармоническому ряду
