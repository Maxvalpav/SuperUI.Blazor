# Generate yellow themes with SEPARATE hues for backgrounds (warm) vs primary (yellow)
import json, re, sys, os

ROOT = r'C:\Users\SuperComp\Documents\Blazor\SuperUI.Blazor'
TEMPLATE = os.path.join(ROOT, 'SuperUI/Themes/json/natura-ui.json')
OUT_DIR = os.path.join(ROOT, 'SuperUI/Themes/json')

with open(TEMPLATE, 'r', encoding='utf-8') as f:
    raw = f.read()

themes = [
    {
        'id': 'banana-zest', 'name': 'Banana Zest',
        'wh': 65, 'yh': 95,  # warm hue, yellow hue
        'glass': True,
        'desc': 'Banana Zest: bright yellow with glassmorphism. Bold, playful.',
    },
    {
        'id': 'golden-hour', 'name': 'Golden Hour',
        'wh': 55, 'yh': 85,
        'glass': False,
        'desc': 'Golden Hour: warm honey, refined luxury.',
    },
]

# Lightness boost for primitives.primary (old L C -> new L C)
LIGHTNESS_BOOST = {
    '0.95 0.03':    '0.97 0.03',
    '0.90 0.06':    '0.94 0.06',
    '0.82 0.10':    '0.90 0.10',
    '0.719 0.138':  '0.85 0.14',
    '0.63 0.18':    '0.80 0.18',
    '0.55 0.22':    '0.75 0.20',
    '0.48 0.22':    '0.68 0.20',
    '0.40 0.20':    '0.58 0.18',
    '0.30 0.18':    '0.45 0.16',
    '0.20 0.15':    '0.30 0.14',
}

# theme.{light,dark} path -> template value with {h} placeholder
# Paths with *W* use warm hue; paths with *Y* use yellow hue
TEMPLATES = {
    '*W': {
        'bg.default': 'oklch(0.97 0.008 {h})',
        'bg.subtle': 'oklch(0.95 0.015 {h})',
        'bg.muted': 'oklch(0.92 0.02 {h})',
        'bg.emphasized': 'oklch(0.88 0.025 {h})',
        'bg.overlay': 'oklch(0.15 0.02 {h} / 0.40)',
        'bg.glass': 'oklch(0.97 0.008 {h} / 0.7)',
        'fg.default': 'oklch(0.14 0.015 {h})',
        'fg.subtle': 'oklch(0.36 0.01 {h})',
        'fg.muted': 'oklch(0.52 0.008 {h})',
        'fg.disabled': 'oklch(0.68 0.006 {h})',
        'fg.inverse': 'oklch(0.97 0.008 {h})',
        'fg.link': 'oklch(0.70 0.20 *Y*)',
        'fg.linkHover': 'oklch(0.65 0.20 *Y*)',
        'border.default': 'oklch(0.88 0.01 {h})',
        'border.subtle': 'oklch(0.92 0.006 {h})',
        'border.strong': 'oklch(0.80 0.015 {h})',
        'border.focus': 'oklch(0.70 0.20 *Y*)',
        'divider': 'oklch(0.92 0.015 {h})',
        'colorPrimary.default': 'oklch(0.75 0.20 *Y*)',
        'colorPrimary.subtle': 'oklch(0.95 0.04 *Y*)',
        'colorPrimary.hover': 'oklch(0.68 0.20 *Y*)',
        'colorPrimary.fg': 'oklch(0.14 0.02 *Y*)',
        'colorWarning.default': 'oklch(0.75 0.18 *Y*)',
        'colorWarning.subtle': 'oklch(0.95 0.04 *Y*)',
        'colorWarning.hover': 'oklch(0.68 0.20 *Y*)',
        'colorWarning.fg': 'oklch(0.14 0.02 *Y*)',
        'state.fgPlaceholder': 'oklch(0.52 0.008 {h})',
        'state.surfaceHover': 'oklch(0.95 0.015 {h})',
        'state.surfaceActive': 'oklch(0.92 0.02 {h})',
        'state.surfaceSelected': 'oklch(0.95 0.04 *Y*)',
        'state.borderHover': 'oklch(0.80 0.015 {h})',
        'state.borderFocus': 'oklch(0.70 0.20 *Y*)',
        'state.colorPrimaryDisabled': 'oklch(0.68 0.006 {h})',
        'state.colorPrimaryDisabledBg': 'oklch(0.92 0.02 {h})',
        'state.colorPrimaryActiveBg': 'oklch(0.68 0.20 *Y*)',
        'state.colorWarningActiveBg': 'oklch(0.68 0.20 *Y*)',
        'state.colorWarningDisabled': 'oklch(0.68 0.006 {h})',
    },
    '*D': {
        'bg.default': 'oklch(0.14 0.012 {h})',
        'bg.subtle': 'oklch(0.18 0.014 {h})',
        'bg.muted': 'oklch(0.22 0.016 {h})',
        'bg.emphasized': 'oklch(0.26 0.018 {h})',
        'bg.overlay': 'oklch(0 0 0 / 0.72)',
        'bg.glass': 'oklch(0.14 0.012 {h} / 0.7)',
        'fg.default': 'oklch(0.93 0.008 {h})',
        'fg.subtle': 'oklch(0.80 0.008 {h})',
        'fg.muted': 'oklch(0.63 0.01 {h})',
        'fg.disabled': 'oklch(0.53 0.01 {h})',
        'fg.inverse': 'oklch(0.14 0.012 {h})',
        'fg.link': 'oklch(0.75 0.18 *Y*)',
        'fg.linkHover': 'oklch(0.80 0.16 *Y*)',
        'border.default': 'oklch(0.28 0.016 {h})',
        'border.subtle': 'oklch(0.22 0.014 {h})',
        'border.strong': 'oklch(0.35 0.018 {h})',
        'border.focus': 'oklch(0.75 0.18 *Y*)',
        'divider': 'oklch(0.22 0.014 {h})',
        'colorPrimary.default': 'oklch(0.78 0.18 *Y*)',
        'colorPrimary.subtle': 'oklch(0.30 0.06 *Y*)',
        'colorPrimary.hover': 'oklch(0.83 0.16 *Y*)',
        'colorPrimary.fg': 'oklch(0.12 0.015 *Y*)',
        'colorWarning.default': 'oklch(0.78 0.16 *Y*)',
        'colorWarning.subtle': 'oklch(0.30 0.06 *Y*)',
        'colorWarning.hover': 'oklch(0.83 0.14 *Y*)',
        'state.fgPlaceholder': 'oklch(0.63 0.01 {h})',
        'state.surfaceHover': 'oklch(0.18 0.014 {h})',
        'state.surfaceActive': 'oklch(0.22 0.016 {h})',
        'state.surfaceSelected': 'oklch(0.30 0.06 *Y*)',
        'state.borderHover': 'oklch(0.35 0.018 {h})',
        'state.borderFocus': 'oklch(0.75 0.18 *Y*)',
        'state.colorPrimaryDisabled': 'oklch(0.53 0.01 {h})',
        'state.colorPrimaryDisabledBg': 'oklch(0.22 0.016 {h})',
        'state.colorPrimaryActiveBg': 'oklch(0.83 0.16 *Y*)',
        'state.colorWarningActiveBg': 'oklch(0.80 0.14 *Y*)',
        'state.colorWarningDisabled': 'oklch(0.53 0.01 {h})',
    },
}

def set_nested(d, path, value):
    keys = path.split('.')
    for k in keys[:-1]:
        d = d.setdefault(k, {})
    d[keys[-1]] = value

def fmt(tpl, wh, yh):
    """Replace {h} with warm hue, *Y* with yellow hue"""
    return tpl.replace('{h}', str(wh)).replace('*Y*', str(yh))

def oklch_replace_hue(value, old, new_hue):
    """Replace old hue with new_hue in an oklch string value, being careful about partial matches."""
    # Match patterns like " 262)" or " 262 /" or "/ 262)" etc.
    return re.sub(r'(?<=[\s/])' + str(old) + r'(?=[\s)])', str(new_hue), value)

for t in themes:
    data = json.loads(raw)
    wh = t['wh']
    yh = t['yh']

    # Metadata
    data['id'] = t['id']
    data['name'] = t['name']
    data['category'] = 'Premium'
    data['description'] = t['desc']
    data['author'] = 'SuperUI Premium'
    data.pop('additionalCss', None)

    # Walk ALL oklch values and replace hue 262 based on context
    raw_json = json.dumps(data, indent=2, ensure_ascii=False)

    # First pass: rotate ALL oklch hue 262 to warm hue
    raw_json = re.sub(
        r'(oklch\([^)]*?)\b262\b([^)]*\))',
        lambda m: m.group(1) + str(wh) + m.group(2),
        raw_json
    )

    data = json.loads(raw_json)

    # Fix primitives.primary: change hue from wh to yh AND boost lightness
    for level in ['50','100','200','300','400','500','600','700','800','900']:
        old_val = data['primitives']['primary'][level]
        # Step 1: replace warmth hue with yellow hue
        val = old_val.replace(' ' + str(wh) + ')', ' ' + str(yh) + ')')
        val = val.replace(' ' + str(wh) + ' ', ' ' + str(yh) + ' ')
        # Step 2: extract L C and boost
        m = re.match(r'oklch\(([\d.]+)\s+([\d.]+)\s+' + str(yh) + r'\)', val)
        if m:
            old_lc = m.group(1) + ' ' + m.group(2)
            if old_lc in LIGHTNESS_BOOST:
                new_lc = LIGHTNESS_BOOST[old_lc]
                val = f'oklch({new_lc} {yh})'
        data['primitives']['primary'][level] = val

    # Fix dark.surface.* — change hue from wh to yellow if primary-related, else keep wh
    # For now, let's just apply warm hue (that's better than green)
    if 'surface' in data['dark']:
        for k in data['dark']['surface']:
            v = data['dark']['surface'][k]
            data['dark']['surface'][k] = v.replace(f' {wh}', f' {wh}')  # no change, keep warm

    # Apply section overrides (light + dark)
    for section, section_templates in [('light', TEMPLATES['*W']), ('dark', TEMPLATES['*D'])]:
        for path, tpl in section_templates.items():
            val = fmt(tpl, wh, yh)
            try:
                obj = data[section]
                set_nested(obj, path, val)
            except (KeyError, TypeError) as e:
                print(f"  WARN: Could not set {section}.{path}: {e}", file=sys.stderr)

    # Additional CSS
    H_str = str(yh)
    if t['glass']:
        css = (
            f'/* Banana Zest glassmorphism */\\n'
            f'[data-theme="light"] {{ --sg-bg: oklch(0.97 0.015 {H_str}); --sg-surface: oklch(1 0 0 / 0.6); --sg-bg-glass: oklch(1 0 0 / 0.5); --sg-blur-glass: 20px; }}\\n'
            f'[data-theme="dark"] {{ --sg-bg: oklch(0.14 0.015 {H_str}); --sg-surface: oklch(0.22 0.02 {H_str} / 0.5); --sg-bg-glass: oklch(0.18 0.018 {H_str} / 0.4); --sg-blur-glass: 20px; }}\\n'
            f'.sgc-glass {{ backdrop-filter: blur(20px); -webkit-backdrop-filter: blur(20px); border: 1px solid oklch(1 0 0 / 0.15); }}'
        )
    else:
        css = (
            f'/* Golden Hour premium */\\n'
            f'[data-theme="light"] {{ --sg-bg: oklch(0.97 0.015 {H_str}); }}\\n'
            f'[data-theme="dark"] {{ --sg-bg: oklch(0.14 0.015 {H_str}); }}'
        )
    data['additionalCss'] = css

    raw_out = json.dumps(data, indent=2, ensure_ascii=False)

    out_path = os.path.join(OUT_DIR, f"{t['id']}.json")
    with open(out_path, 'w', encoding='utf-8') as f:
        f.write(raw_out)
    print(f"Created: {t['id']}.json")

print("Done!")
