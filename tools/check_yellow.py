import json, os

root = r'C:\Users\SuperComp\Documents\Blazor\SuperUI.Blazor\SuperUI\Themes\json'
for fname in ['banana-zest.json', 'golden-hour.json']:
    fp = os.path.join(root, fname)
    with open(fp, 'r', encoding='utf-8') as f:
        d = json.load(f)

    print(f'===== {d["id"]} =====')

    p = d['primitives']
    print('--- primitives ---')
    print(f'  neutral.500: {p["neutral"]["500"]}')
    print(f'  primary.500: {p["primary"]["500"]}')
    print(f'  success.500: {p["success"]["500"]}')
    print(f'  danger.500:  {p["danger"]["500"]}')
    print(f'  warning.500: {p["warning"]["500"]}')

    lt = d['light']
    print('--- light ---')
    print(f'  bg.default:  {lt["bg"]["default"]}')
    print(f'  fg.default:  {lt["fg"]["default"]}')
    print(f'  border.def:  {lt["border"]["default"]}')
    print(f'  divier:      {lt["divider"]}')
    print(f'  colorP.def:  {lt["colorPrimary"]["default"]}')
    print(f'  colorP.fg:   {lt["colorPrimary"]["fg"]}')
    print(f'  colorW.def:  {lt["colorWarning"]["default"]}')
    print(f'  colorW.fg:   {lt["colorWarning"]["fg"]}')
    print(f'  link:        {lt["fg"]["link"]}')
    print(f'  state.sel:   {lt["state"]["surfaceSelected"]}')

    dk = d['dark']
    print('--- dark ---')
    print(f'  bg.default:  {dk["bg"]["default"]}')
    print(f'  fg.default:  {dk["fg"]["default"]}')
    print(f'  colorP.def:  {dk["colorPrimary"]["default"]}')
    print(f'  colorP.fg:   {dk["colorPrimary"]["fg"]}')
    print('--- dark.surface ---')
    if 'surface' in dk:
        for k, v in dk['surface'].items():
            print(f'  {k}: {v}')
    print()
