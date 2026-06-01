# Migrating from SuperUI 1.x to 2.0

2.0 is a major release. Most call sites are source-compatible; the
breaking changes are narrow and well-flagged. This guide walks through
each one, in the order you are most likely to hit them.

---

## TL;DR — what to do

1. Update the package: `dotnet add package SuperUI --version 2.0.0-alpha.1`.
2. Drop the `<link>` to `css/superui-tokens.css` if you have one.
3. Make sure `superui-theme.js` is reachable
   (`_content/SuperUI/superui-theme.js` — it's a static web asset
   from the package; you don't need to add it manually).
4. Replace any sub-class of `ThemeBase` with `ThemeBuilder` or
   implement `IThemeDefinition` directly.
5. Add an `<SgThemeProvider>` if you don't already have one (it was
   optional in 1.x; the new runtime needs it to mount the link
   element and wire the matchMedia listener).
6. Replace any direct call to `CurrentTheme.GenerateCss()` with
   `SgThemeGenerator.GenerateFullThemeCss(theme)` (this is the same
   method the new build-time exporter uses).
7. Build, smoke-test the theme switcher, and ship.

If you only used the public API surface (the 90+ `Sg*` components and
the `SgThemeSwitcher`), step 1 is the only one that matters. The rest
of this guide is for the small set of consumers that touched internals.

---

## Breaking changes

### 1. `ThemeBase` is `[Obsolete]`

`SuperUI.Themes.ThemeBase` was the abstract base class for every
shipped theme. It is now decorated with `[Obsolete]`; the warning
text points at `ThemeBuilder` and `IThemeDefinition`.

**1.x pattern (still compiles with a warning):**

```csharp
public sealed class MyBrandTheme : ThemeBase
{
    public MyBrandTheme() : base("my-brand", "My Brand", "Acme Corp") { }
    // override ColorPrimary, ColorBg, ...
}
```

**2.0 pattern (preferred):**

```csharp
public sealed class MyBrandTheme : ThemeBuilder
{
    public MyBrandTheme()
    {
        Id      = "my-brand";
        Display = "My Brand";
        Author  = "Acme Corp";
        SetPrimary(oklch(0.55, 0.20, 260));
        SetBg(oklch(0.99, 0.01, 260));
        // ... or use the JSON path (see §2)
    }
}
```

If you do not want a code dependency on SuperUI, the JSON path is
cleaner: drop a `my-brand.json` into `Themes/json/` and reference it
via the resource manifest. See [§2](#2-themes-are-now-json).

The 32 `*Theme.cs` classes that were in the `SuperUI.Themes` namespace
in 1.x have been removed (they were not public API; they were an
internal convenience). If you were depending on the type name in a
DI registration or `is`-check, switch to a JSON theme or a
`ThemeBuilder` sub-class.

### 2. Themes are now JSON

`ThemeBase` was backed by 32 hand-written C# classes, one per theme.
In 2.0, every theme is a JSON file in `Themes/json/`, validated
against `Themes/schemas/theme.schema.json`, and embedded as a
resource (`SuperUI.Themes.json.{id}.json`).

`new ThemeRegistry()` is now equivalent to
`new ThemeRegistry().LoadEmbeddedJsonThemes()` — the JSON loader
runs in the constructor.

If you have your own theme files, ship them as
`SuperUI.Themes.json.{your-id}.json` embedded resources in your
own assembly, then call:

```csharp
var reg = new ThemeRegistry();
reg.LoadEmbeddedJsonThemes(typeof(YourAssembly).Assembly);
services.AddSingleton(reg);
```

If you have themes in a non-embedded location:

```csharp
reg.RegisterJsonFromFile("my-theme", "/themes/my-theme.json");
```

### 3. The runtime ships a path, not a CSS string

`SgThemeService` no longer generates a CSS string on the fly and
pumps it into JS. The new flow is:

1. C# emits a path like
   `_content/SuperUI/themes/css/{themeId}.css` in the state DTO.
2. JS swaps the `href` attribute of a single
   `<link id="sg-theme-link" rel="stylesheet">` element in `<head>`.
3. The browser cache serves subsequent swaps (the static web asset
   ships with `Cache-Control: max-age=31536000, immutable` and
   ETag).

The 43 pre-built `.css` files are produced at design time by
`tools/ThemeCssExporter`. Run it whenever you add a new theme:

```bash
dotnet run --project tools/ThemeCssExporter/ThemeCssExporter.csproj \
    -- "SuperUI/wwwroot/themes/css"
```

**If you wrote a custom JS hook that called
`SuperUI.applyThemeCss(cssString)`,** you have two options:

* **Preferred:** switch to `SuperUI.applyThemeLink(href)` and
  pre-build your CSS with `SgThemeGenerator.GenerateFullThemeCss`.
* **Back-compat:** `SuperUI.applyThemeCss` is still exported and
  still works for the duration of the 2.x line. It will be removed
  in 3.0.

### 4. Hardcoded `--sg-color-warn` and friends are now aliases

In 1.x, `--sg-color-warn` (and ~200 other tokens) silently fell back
to `initial` because the variable was never defined. In 2.0, they
are defined in `sg-tokens-compat.css` as aliases:

```css
--sg-color-warn:           var(--sg-color-warning);
--sg-color-warn-subtle:    var(--sg-color-warning-subtle);
--sg-color-error:          var(--sg-color-danger);
--sg-color-text:           var(--sg-fg);
--sg-bg-glass:             var(--sg-color-primary-10);
--sg-easing-out:           var(--sg-ease-out);
...
```

If you were working around the missing tokens by defining your own
copy in your own CSS, you can now remove the override and let the
canonical token flow through. (You can still override — the aliases
are just regular CSS variables — but you no longer have to.)

### 5. `css/superui-tokens.css` is no longer shipped

The 1.x package included a `css/superui-tokens.css` file that was
never imported by anything but was published as a static web asset
anyway. It defined a parallel, conflict-prone token namespace. In
2.0, it is gone. If you were `<link>`-ing it from your `index.html`
or `App.razor`, drop the line — the canonical token set now lives
in `themes/sg-tokens-*.css` and is loaded by the new
`<SgThemeProvider>`.

### 6. `superui-components.css` uses `color-mix()` instead of `rgba()`

`rgba(R, G, B, A)` was replaced everywhere with
`color-mix(in srgb, COLOR A*100%, transparent)`. This is purely a
syntactic migration — the visual output is the same, and the
generated cascade is shorter for the browser to parse. If you
were grepping for `rgba(` in the bundled CSS to do theme audits,
switch to `color-mix(`.

If you need IE 11 / pre-2021 browser support, do not upgrade to 2.0.
`color-mix()` shipped in Chrome 111, Firefox 113, and Safari 16.2.

---

## New things you can use

### New token layers

```css
/* motion */
--sg-dur-fast:    120ms;
--sg-dur-normal:  220ms;
--sg-dur-slow:    380ms;
--sg-ease-out:    cubic-bezier(0.2, 0.7, 0.2, 1);
--sg-ease-in:     cubic-bezier(0.5, 0, 0.75, 0);
--sg-ease-spring: cubic-bezier(0.34, 1.56, 0.64, 1);

/* state */
--sg-state-hover: ...
--sg-state-active: ...
--sg-state-disabled: ...
--sg-state-focus: ...

/* elevation */
--sg-elev-0:  none;
--sg-elev-1:  0 1px 2px ...;
--sg-elev-2:  0 4px 8px ...;
--sg-elev-3:  0 8px 24px ...;
--sg-elev-4:  0 16px 48px ...;
--sg-elev-overlay: 0 24px 64px ...;

/* a11y */
--sg-a11y-focus-ring: 0 0 0 2px var(--sg-color-primary);
--sg-a11y-sr-only: ...   /* screen-reader-only utility */
```

### `SgThemeGenerator.GenerateFullThemeCss`

Exposed publicly for the first time. Returns the full CSS string
for a given `IThemeDefinition` — `:root` (light), `[data-theme="dark"]`
(dark), and the theme-specific `[data-theme-id="..."]` block.

```csharp
string css = SgThemeGenerator.GenerateFullThemeCss(theme);
// serve it, write it to disk, push it through a CDN, whatever.
```

### `SgThemeService` debounce + idempotency

`InitializeAsync` is now safe to call from multiple `OnInitialized`
paths. `DisposeAsync` is race-safe against in-flight `ThemeChanged`
notifications. State-mutating calls coalesce into a single
150 ms-debounced `applyThemeState({...})` DTO.

### `preloadThemeLink(href)`

If you know which theme the user is likely to switch to next (say,
because they hovered a theme card), call:

```js
window.SuperUI.preloadThemeLink(
    '_content/SuperUI/themes/css/' + themeId + '.css'
);
```

The browser will warm its cache so the actual swap is instant.

---

## Deprecations scheduled for 3.0

These still work in 2.0 but will be removed in 3.0. Plan accordingly.

* `SuperUI.Themes.ThemeBase` — use `ThemeBuilder` or `IThemeDefinition`.
* `SuperUI.applyThemeCss(cssString)` (JS) — use `applyThemeLink(href)`.
* The `*-rgb` token naming convention
  (`--sg-color-primary-rgb`, `--sg-bg-rgb`) — these were the
  half-step before `color-mix()`. The 2.0 compat layer still
  defines them so the old `rgba(var(--sg-color-primary-rgb), A)`
  pattern keeps working, but new code should use
  `color-mix(in srgb, var(--sg-color-primary) A*100%, transparent)`.

---

## Getting help

* File an issue at
  [github.com/Maxvalpav/SuperUI.Blazor/issues](https://github.com/Maxvalpav/SuperUI.Blazor/issues).
* The plan that drove this release is in
  [`plans/theme-2.0.md`](../../plans/theme-2.0.md).
* For context on individual PRs, see the commit history — every
  change in 2.0 is split into a standalone PR (#1 through #7) with
  a self-contained commit message.
