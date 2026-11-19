# ControllerPC

A lightweight Windows tray app that lets an Xbox-style (XInput) controller drive the PC as a mouse and
keyboard. You switch it on and off with button sequences and/or combinations you choose.

- Domain vocabulary: [CONTEXT.md](CONTEXT.md)
- Coding standards and branch workflow: [docs/CODING-STANDARDS.md](docs/CODING-STANDARDS.md)

## Build

Requires the .NET SDK 9.0.200 or later on Windows (needed for the `.slnx` solution; the app targets .NET 8).

```bash
dotnet build
dotnet test
```

## Publish

```bash
dotnet publish src/ControllerPC.App -c Release
```

Produces a single framework-dependent `ControllerPC.exe` (needs the .NET 8 Desktop Runtime).
Settings are stored in `%AppData%\ControllerPC\settings.json`.
