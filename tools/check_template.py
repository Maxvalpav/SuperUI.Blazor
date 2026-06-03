import json

with open(r'C:\Users\SuperComp\Documents\Blazor\SuperUI.Blazor\SuperUI\Themes\json\natura-ui.json', 'r') as f:
    d = json.load(f)

# Show all keys in light.bg, light.fg, light.colorPrimary, light.surface
print('=== light.bg keys:', list(d['light']['bg'].keys()))
for k, v in d['light']['bg'].items():
    print(f'  {k}: {v}')
print()

print('=== light.fg keys:', list(d['light']['fg'].keys()))
print()

print('=== light.state keys:', list(d['light']['state'].keys()))
print()

print('=== dark.bg keys:', list(d['dark']['bg'].keys()))
print()

print('=== dark.fg keys:', list(d['dark']['fg'].keys()))
print()

print('=== dark.state keys:', list(d['dark']['state'].keys()))
print()

# Check if there's a "surface" key
if 'surface' in d['light']:
    print('=== light.surface:', d['light']['surface'])
if 'surface' in d['dark']:
    print('=== dark.surface:', d['dark']['surface'])
