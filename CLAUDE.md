# SuperUI

Blazor component library published to NuGet as `SuperUI`. 90+ components covering data grid, canvas grid, forms, overlays, navigation, layout, charts, kanban, gantt, pivot table, org chart, scheduler, diagram editor, and more. Targets **.NET 10**, supports Blazor WASM, Server, and Web App hosting models.

- Repo: https://github.com/Maxvalpav/SuperUI.Blazor
- NuGet: https://www.nuget.org/packages/SuperUI
- Live demo: https://maxvalpav.github.io/SuperUI.Blazor/

## Solution layout

`SuperUI.slnx` contains:

- `SuperUI/SuperUI.csproj` — the library (Microsoft.NET.Sdk.Razor, packed to NuGet)
- `SuperUI.Demo/SuperUI.Demo.csproj` — Blazor WebAssembly demo / manual test harness, deployed to GitHub Pages
- `SuperUI.Tests/SuperUI.Tests.csproj` — xUnit + bUnit tests (not in `.slnx` — add via `dotnet sln` if needed)

## Architecture — flat component library

This is a **UI component library**, not an application. Do not impose VSA, Clean Architecture, or DDD layering. Components are organized by family under `SuperUI/Components/`:

```
SuperUI/Components/
  AI/                     Analytics/             Charts/
  Data/                   Display/               DocumentExtractor/
  Feedback/               Forms/                 HttpApiTester/
  Layout/                 Maps/                  Navigation/
  Network/                Other/                 Overlays/
  SgMachineScheduler/
```

Each component typically lives in its own subfolder with `Sg<Name>.razor` + `Sg<Name>.razor.cs` code-behind. Component-scoped CSS goes into `SuperUI/wwwroot/superui-components.css` (single bundled stylesheet shipped via `_content/SuperUI/`).

### Naming & conventions
- All public components are prefixed `Sg` (e.g. `SgRow`, `SgCol`, `SgSpace`, `SgThemeProvider`, `SgToastHost`).
- Code-behind pattern: partial class in `Sg<Name>.razor.cs` for non-trivial logic; keep `.razor` markup-focused.
- Public APIs ship XML docs — `GenerateDocumentationFile=true` is on and 1591 warnings are enabled. **Every public type, parameter, and event must have XML docs.**
- Browser is the only supported platform (`<SupportedPlatform Include="browser" />`).
- **Complex rendering in code-behind:** When a `.razor` template with loops, conditionals, element ref captures, and event callbacks fails to compile (Razor source generator producing invalid C#), move ALL rendering logic to code-behind using `RenderTreeBuilder`. The `.razor` file acts as a thin shell that renders a `_renderContent` field, which is built in code-behind. See `SgSplitter` for the pattern.

### Theming
- CSS variables drive light/dark mode. Theme styles in `wwwroot/superui-theme.css`, component styles in `wwwroot/superui-components.css`.
- `SgThemeProvider` is the root wrapper consumers place in `MainLayout.razor`.
- Host components required in every app: `SgToastHost`, `SgConfirmHost`, `SgPortalHost`.

### Localization
- en-US and ru-RU built in. Extensibility via `ISuperUILocalizer`.

## Tech stack

- **Framework:** .NET 10, C# `latest`, nullable enabled, implicit usings on
- **Components:** Microsoft.AspNetCore.Components.Web 10.0.0, Microsoft.AspNetCore.Components.CustomElements 10.0.0
- **AI:** Microsoft.Extensions.AI 10.0.0 (used by AI components)
- **Graphics:** SkiaSharp 3.116.1 (+ HarfBuzz, Views.Blazor) — for canvas grid, charts, diagram editor, etc.
- **Misc:** Net.Codecrete.QrCodeGenerator 2.1.0
- **Source link:** Microsoft.SourceLink.GitHub (debugging support in published packages)

## Testing

- **Framework:** xUnit 2.8.1 + bUnit 1.30.3 (Microsoft.NET.Test.Sdk 17.11.1).
- Run from solution root: `dotnet test SuperUI.Tests/SuperUI.Tests.csproj`.
- bUnit renders components in-memory — use it to assert rendered markup, parameter handling, event callbacks, and JS interop substitutes.
- Do not introduce Testcontainers, WebApplicationFactory, or integration-test infrastructure — this is a UI library, not a service.
- Prefer adding a bUnit test next to any non-trivial component change, especially for the data grid, forms, and overlay components.

## NuGet packaging

This project ships as a public NuGet package. Treat the public API as a stable contract.

- **Package ID:** `SuperUI`
- **Version field:** `<Version>` in `SuperUI/SuperUI.csproj` — bump on every release
- **Build defaults:** `GeneratePackageOnBuild=false` (CI packs explicitly), `IncludeSymbols=true`, `SymbolPackageFormat=snupkg`, `EmbedUntrackedSources=true`, `PublishRepositoryUrl=true`
- **Pack:** `dotnet pack SuperUI/SuperUI.csproj -c Release -o ./artifacts`
- **CI:** `.github/workflows/build-and-publish.yml` handles build + publish; demo is deployed to GitHub Pages

### Versioning rules
- **Patch** (`1.0.X`): bug fixes, internal refactors, no public API change
- **Minor** (`1.X.0`): new components, new optional parameters, additive enhancements
- **Major** (`X.0.0`): renaming/removing public components, parameters, events, or required behavior changes — avoid without explicit user direction
- Update `<PackageReleaseNotes>` when bumping version
- README.md is packed into the package (`<None Include="..\README.md" Pack="true" PackagePath="\" />`) — keep it current

### Public API hygiene
- A removed `[Parameter]` is a breaking change. So is renaming a public component class.
- Prefer `[Obsolete]` + a new API over outright removal across a minor version line.
- Default parameter values are part of the contract — changing one is a behavior break.

## Working in this repo

- Use the Roslyn MCP tools (find_symbol, find_references, get_public_api, get_type_hierarchy) before sweeping changes to a component — public surface is referenced from `SuperUI.Demo` and downstream consumers.
- When touching a layout component (`SgRow`, `SgCol`, `SgSpace`, etc.), verify the demo still renders correctly — `SuperUI.Demo` is the canonical manual test harness.
- Keep component styles in `superui-components.css` (preferred) or scoped `.razor.css` files. Both patterns are used in the codebase: `superui-components.css` is the single bundled stylesheet shipped via `_content/SuperUI/`, while `.razor.css` files provide scoped isolation for complex components. Do NOT duplicate styles across both.
- Don't add new dependencies without checking package size impact — consumers ship this library to the browser via WASM.
- Tag any new public component with XML docs **before** committing.

## Demo app

`SuperUI.Demo` is a Blazor WebAssembly app that:
- Doubles as a visual regression / manual test harness
- Is deployed to GitHub Pages on release
- Imports `SuperUI` via `ProjectReference` (so changes in the library are picked up immediately on `dotnet run`)
- Run locally: `dotnet run --project SuperUI.Demo/SuperUI.Demo.csproj`



