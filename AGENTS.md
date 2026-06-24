# AGENTS.md - AI Coding Agent Guide for MudX

This file is the project-specific instruction manual for AI coding agents. Follow it exactly unless the user asks otherwise. If a nested `AGENTS.md` exists in a subdirectory, follow the more specific rules for that area in addition to this file.

## Do / Don't (Read First)

### Do
- Target specific MudX projects only; avoid solution-wide commands unless explicitly requested
- Keep diffs small and focused; avoid repo-wide rewrites unless asked
- Follow `.editorconfig` and add file headers where required
- Add XML `<summary>` docs for all public properties
- Reuse MudBlazor utilities, styles, and classes whenever possible before creating new ones
- Use `CssBuilder` for classes/styles and CSS variables (no hard-coded colors)
- Use `ParameterState<T>` for parameter updates and change handlers
- Keep `src/MudX/Scripts/mudx.js` in sync with related script files
- Any component level JavaScript should be added as a module in the `src/MudX/Scripts/Modules/` folder and imported in the component (See MudXCodeBlock.razor.cs).

### Don't
- Do not add new heavy dependencies or packages without approval
- Do not run solution-wide build/test/format unless explicitly requested
- Do not put logic in parameter getters/setters (parameters must be auto-properties)
- Do not set other component parameters via `@ref` (BL0005)
- Do not hard code colors or bypass design tokens/CSS variables
- Do not ignore analyzer warnings; warnings are errors

## Project Map

```
MudX/
- src/MudX/                    Core library (components, styles, scripts)
- src/MudX.Docs/               Documentation site
- src/MudX.Docs.Generator/     Docs generator
- src/MudX.Docs.Server/        Docs server host
- src/MudX.Docs.Hybrid/        Hybrid docs hosts
- src/MudX.Generator/          Static asset generator
- tests/MudX.UnitTests/        bUnit tests
- tests/MudX.UnitTests.Viewer/ Visual test runner
```

Key config:
- `.editorconfig` (code style, file headers)
- `src/MudX.slnx` (solution)
- `.github/workflows/Build_And_Test.yml` (CI)

## Prerequisites

- .NET 10.0 SDK (10.0.100+) required for tests. Library targets net8.0/net9.0/net10.0.
- Verify with `dotnet --version`.

## Commands (Scoped)

Target specific projects only:
- Components: `src/MudX/MudX.csproj` + `tests/MudX.UnitTests/MudX.UnitTests.csproj`
- Docs: `src/MudX.Docs.Generator/MudX.Docs.Generator.csproj` + `src/MudX.Docs/MudX.Docs.csproj`
- Static assets: `src/MudX.Generator/MudX.Generator.csproj`

Build/Test:
```bash
dotnet clean <project.csproj>
dotnet build <project.csproj> --nologo
dotnet test tests/MudX.UnitTests/MudX.UnitTests.csproj --filter "FullyQualifiedName~MudXChat" --no-build --nologo --blame-hang --blame-hang-timeout 30s
```

C#-only builds (skip Bun for JS/SCSS):
```bash
dotnet build <project.csproj> /p:SkipBunCompile=true --nologo
```
Avoid this for styling/JS changes or full builds.

Formatting (required for changed files):
```bash
dotnet format <project.csproj> --include <path/to/changed/files>
```

Run locally:
```bash
dotnet run --project src/MudX.Docs/MudX.Docs.csproj
dotnet run --project src/MudX.Docs.Server/MudX.Docs.Server.csproj
dotnet run --project tests/MudX.UnitTests.Viewer/MudX.UnitTests.Viewer.csproj
```

## Component Rules

- Parameters are auto-properties only; no logic in getters/setters.
- Do not overwrite component parameters; use the backing `ParameterState<T>` (`.Value`/`SetValueAsync`) for updates.
- Never set other component parameters via `@ref` (BL0005). Use declarative binding.
- RTL support: add `[CascadingParameter] public bool RightToLeft { get; set; }` when layout depends on direction.
- Use `CssBuilder` for classes/styles and CSS variables (no hard-coded colors).
- Add XML `<summary>` for all public properties and use the file header template from `.editorconfig`.
- Components with logic require bUnit tests and a docs page: `src/MudX.Docs/Components/Docs/<ComponentName>/`.
- Follow best ARIA practices.
- Ensure keyboard navigation works for interactive components.
- Provide accessible names for interactive controls (label, `aria-label`, or `aria-labelledby`).

## Docs Rules

- Order examples from simple to complex.
- Collapse examples longer than 15 lines by default.
- Docs examples are exercised by generated tests; keep them rendering without exceptions.
- Keep docs in sync with behavior and parameter changes; update examples and descriptions when APIs change.
- Prefer minimal, focused examples that demonstrate one concept at a time.

## Breaking Changes

- Avoid breaking changes whenever possible; prefer additive APIs, defaults, or obsoleting old behavior.
- If a breaking change is required, call it out explicitly in the PR description and update docs/tests accordingly.
- For parameter renames/removals, consider `[Obsolete]` with a clear message and a migration path.

## Testing Rules

- bUnit: never cache `Find()`/`FindAll()` results; re-query after interactions.
- bUnit: always use `InvokeAsync()` for parameter changes or method calls.
- Prefer async bUnit interactions (`ClickAsync`, `ChangeAsync`, `BlurAsync`, `InputAsync`) over sync methods.
- Keep tests isolated for parallel execution; rework tests to run in parallel instead of using `[NonParallelizable]` when possible.
- Prefer `TimeProvider`/`FakeTimeProvider` over `Task.Delay`.
- Test logic, not full HTML snapshots; use focused assertions.
- Test components live in `tests/MudX.UnitTests.Viewer/TestComponents/<ComponentName>/`.
- Tests live in `tests/MudX.UnitTests/Components/<ComponentName>Tests.cs`.
- Test naming: no `Test`/`Async` suffixes, no `Test_` in the middle, no trailing underscores.

## Code Style and Analyzers

- Follow `.editorconfig` (indentation, using placement, file headers).
- Treat warnings as errors; fix new warnings instead of suppressing them.
- CS4014: no unobserved async discards (`_ = SomeAsync()` is an error).
- BL0007: component parameters should be auto-properties.

## Change Checklist

- Scope is small and focused
- Formatting run for changed files
- Target project builds cleanly (no new warnings)
- Tests updated and run when behavior changes
- Docs updated for component logic changes
- No new dependencies without approval

## When Stuck

- Ask a clarifying question or propose a short plan
- Avoid speculative, large changes without confirmation
