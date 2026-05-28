# Changelog

All notable changes to this project are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Versioning is automatic via **MinVer** — derive from git tags matching `v*`. Run `git tag v1.1.0 && git push --tags` to release.

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

[1.1.0]: https://github.com/Maxvalpav/SuperUI.Blazor/releases/tag/v1.1.0
[1.0.9]: https://github.com/Maxvalpav/SuperUI.Blazor/releases/tag/v1.0.9
