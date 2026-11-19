# Coding standards

These are the rules the QA review enforces before a branch can merge into `develop`.

## Workflow

- `main` only receives merges from `develop`. `develop` only receives merges from feature branches.
- One feature per branch, named `feature/<short-name>`, branched from `develop`.
- A feature branch merges into `develop` (with `--no-ff`) only after QA approval.
- Commit messages: imperative mood, short subject line (≤ 72 chars), body when the why isn't obvious.

## Architecture

| Project | Purpose | May depend on |
|---|---|---|
| `ControllerPC.Core` | Domain model and all decision-making logic. No Windows APIs, no UI. | nothing |
| `ControllerPC.Windows` | Thin adapters over Win32: XInput, SendInput, rumble. No decisions. | Core |
| `ControllerPC.App` | WinForms tray app and settings window; composition root. | Core, Windows |
| `ControllerPC.Core.Tests` | xUnit tests for Core. | Core |
| `ControllerPC.Windows.Tests` | xUnit tests for the Windows adapters' conversions and native layouts. | Windows (and Core) |
| `ControllerPC.App.Tests` | xUnit tests for the App's UI-independent pieces (e.g. the Desktop Mode host's helpers). | App (and its dependencies) |

- Logic that can be tested without Windows belongs in Core and must have tests.
- Windows adapters stay thin: translate types, call the API, nothing else.
- UI code holds no domain rules: it edits settings and calls Core (e.g. `SettingsValidator`, `TriggerRecorder`).
- Contracts in Core (public types and signatures) are shared across features. Change one only when
  necessary, and call it out in the commit message.

## Clarity

- Use the vocabulary in `CONTEXT.md` (Desktop Mode, Mode Trigger, Step, Chord, Binding, Action, Profile, ...)
  in type names, members, comments and UI text. Don't introduce synonyms.
- One type per file; file name equals type name. Small, focused classes and methods.
- Names say what something is. Avoid abbreviations, `Manager`/`Helper`/`Util` grab-bags, and magic numbers
  (name constants).
- Public types and members get a short XML doc comment when the name alone doesn't say enough.
- Comments explain *why*, not *what*.
- No dead code, no commented-out code, no `TODO` without context.

## Correctness

- Builds with zero warnings (`TreatWarningsAsErrors` is on). Nullable reference types stay enabled.
- `dotnet test` passes on every branch before review.
- Tests describe behavior (`Update_FiresWhenFinalChordHeldForFinalHold`), one behavior per test, no sleeps:
  time is passed in explicitly.
- Native resources and P/Invoke signatures are correct for x64; check return codes.
- Never leave a synthetic key or mouse button pressed: every press has a matching release path.
