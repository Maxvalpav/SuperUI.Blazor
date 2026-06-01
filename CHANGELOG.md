# Changelog

All notable changes to this project are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Versioning is automatic via **MinVer** — derive from git tags matching `v*`. Run `git tag v1.1.0 && git push --tags` to release.

## [2.0.0-alpha.1] - 2026-06-02

### Added

- **JSON-defined themes (43 total).** Every theme is now a JSON file in
  `Themes/json/`, validated against `Themes/schemas/theme.schema.json`,
  embedded as a resource, and loaded at runtime by
  `new ThemeRegistry()`. The 32 hardcoded C# `*Theme.cs` classes are
  gone. New themes: aether, biofilia, calyx, cantus, chrono, circadian,
  clarity, clarity-clinical, element, ergo, fractalis, gordian, inclus,
  lumina, medici, muse, oasis, prism, reader, signature, sylvan, veiled,
  wave, window.
- **New token layers** (D1-D4): motion (`--sg-dur-*`, `--sg-ease-*`),
  state (`--sg-state-*`), elevation (`--sg-elev-0..4` + overlay),
  a11y (`--sg-a11y-focus-ring`, `--sg-a11y-sr-only`).
- **`sg-tokens-compat.css`** — 195 alias tokens (`--sg-color-warn` →
  `--sg-color-warning`, `--sg-color-error` → `--sg-color-danger`,
  `--sg-color-text` → `--sg-fg`, ...) so the 95 silent fallbacks
  and ~200 missing-token references finally resolve.
- **`tools/ThemeCssExporter/`** — small CLI that walks
  `ThemeRegistry.GetAll()` and writes one pre-built `.css` per theme
  to `wwwroot/themes/css/{id}.css` (used by the new link-swap runtime).
- **`SgThemeGenerator.GenerateFullThemeCss(IThemeDefinition)`** —
  the entry point the exporter and any custom build tool can call.
- **`superui-theme.js`** — standalone module with `applyThemeState`,
  `applyThemeLink`, `preloadThemeLink`, `getSavedState`, `initAutoMode`.
  Loaded as `_content/SuperUI/superui-theme.js` via `IJSRuntime.import`.
- **`docs/MIGRATION-2.0.md`** — full 1.x → 2.0 migration guide.

### Changed

- **`ThemeBase` is now `[Obsolete]`** in 2.0. Derive from
  `ThemeBuilder` or implement `IThemeDefinition` directly. Will be
  removed in 3.0.
- **Runtime is now link-swap, not push-CSS.** The C# service no longer
  ships 15-33 KB of CSS strings to JS on every state change. Instead
  it sends a single `themeHref` and the JS module mutates a
  `<link rel="stylesheet">` in place, so the browser cache and gzip
  compression handle repeat swaps. Backed by pre-built
  `_content/SuperUI/themes/css/{id}.css` with `Cache-Control:
  max-age=31536000, immutable` and ETag.
- **`SgThemeService.ApplyThemeAsync`** is debounced 150 ms and now
  ships a single batched DTO (`applyThemeState({...})`) instead of
  5 × `localStorage.setItem` + 6 × `eval` interop hops per state
  change. `InitializeAsync` is idempotent; `DisposeAsync` is race-safe.
- **`prefers-color-scheme` subscription** is wired only while the user
  is in `auto` mode, and is detached the moment they switch to
  `light` / `dark` explicitly.
- **`superui-components.css`** — every `rgba(R, G, B, A)` literal
  (302 calls) is now `color-mix(in srgb, COLOR A*100%, transparent)`.
  The two tokenized forms
  (`rgba(var(--sg-color-primary-rgb), A)` and
  `rgba(var(--sg-bg-rgb), A)`) are remapped to their non-rgb token
  counterparts so the tints are theme-aware. Hardcoded RGB triplets
  for the legacy `0, 111, 238` blue, `244, 63, 94` rose, etc. stay
  as `rgb(R, G, B)` literals for now (a follow-up PR will remap
  them to `--sg-color-primary` / `--sg-color-danger` etc.).

### Removed

- **32 hardcoded `*Theme.cs` classes** (NaturaTheme, SolarisTheme,
  RoyalTheme, ...). Replaced by JSON files.
- **`tools/ThemeConverter/`** — superseded by the JSON-based registry.
- **Legacy `css/superui-tokens.css`** — orphan, conflicting names.

### Fixed

- **3 comma-decimal bugs** in `natura-ui.json` (1,618 → 1.618,
  0,618 → 0.618, 137,507 → 137.507) that broke `phi`, `phi-inv`, and
  `golden-angle` constants at parse time.
- **`SgThemeService.DisposeAsync` race** — captured `ThemeChanged`
  delegate locally before nulling, so subscribers fired during
  teardown no longer see a half-cleared handler list.
- **3 dead CSS branches**: `:root.dark` × 3, `.sui-dark` × 1, and the
  stray `:root` block at `superui-components.css:378`.

### Notes for 1.x users

- Most call sites are source-compatible. The only breaking changes
  are: (a) any code that sub-classed `ThemeBase` will see a build
  warning and should migrate to `ThemeBuilder`; (b) any code that
  referenced the legacy `css/superui-tokens.css` path must drop
  the reference (it is no longer shipped). See
  `docs/MIGRATION-2.0.md` for the full checklist.

## [1.1.0] - 2026-05-28

### Added
- SgButton progress mode: `Progress` (0-100), `ProgressType` (Bar/Ring), `ProgressSpinnerType` (Border/Pulse/Dots/Bars)
- SgButton.razor.cs code-behind with RenderTreeBuilder rendering
- SgButtonProgressType enum (Bar, Ring)
- SgSpinner determinate progress mode with gradient, thickness, easing, completion animation
- SgSpinner types: Typing, Morph (9 total)
- SgSpinner speed fix: non-SVG spinners get inline animation-duration via GetSpeedDuration()
- SgSpinner keyframes fix: CSS `rotate` → `transform: rotate()` for SVG compatibility
- superui-components.css: sgc-btn-progress-fill, sgc-btn-progress-text styles
- SpinnerDemo: interactive constructor (native selects), CSS grid gallery, interactive progress with gradients
- ButtonDemo: progress button demo with type/spinner selectors, live constructor

### Changed
- NuGet version bumped to 1.1.0
- MinVer automatic versioning from git tags
- CS1591 warning suppressed in SuperUI.csproj

### Fixed
- SgSpinner SVG determinate progress: stroke-dashoffset + sgc-spin animation, removed animation:none
- SgSpinner non-SVG speed: SpeedClass on root no longer ignored by children
- SpinnerDemo All Types layout: flex-wrap → CSS grid
- ButtonDemo progress idle state: Progress=-1 prevents "0%" on button text

## [1.0.9] - 2026-04-29

### Added
- Initial public release of SuperUI.
- 25+ Blazor components: data grid, forms, overlays, navigation, layout, charts.
- Light & dark theme via CSS variables.
- Localization: en-US, ru-RU.
- XML documentation for IntelliSense.
- Symbol package (`.snupkg`) with embedded source for debugging.
- Live demo published to GitHub Pages.

[2.0.0-alpha.1]: https://github.com/Maxvalpav/SuperUI.Blazor/compare/v1.1.0...v2.0.0-alpha.1
[1.1.0]: https://github.com/Maxvalpav/SuperUI.Blazor/releases/tag/v1.1.0
[1.0.9]: https://github.com/Maxvalpav/SuperUI.Blazor/releases/tag/v1.0.9
