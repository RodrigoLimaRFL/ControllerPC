# ControllerPC

A lightweight Windows tray app that lets an Xbox-style (XInput) controller drive the PC as a mouse and
keyboard. You switch it on and off with button sequences and/or combinations you choose.

- Domain vocabulary: [CONTEXT.md](CONTEXT.md)
- Coding standards and branch workflow: [docs/CODING-STANDARDS.md](docs/CODING-STANDARDS.md)

## Using it

ControllerPC runs in the notification area (system tray). Hold the Entry Trigger on any connected controller to
switch **Desktop Mode** on; hold the Exit Trigger to switch it off. The controller rumbles once on entering and
twice on leaving.

- **Entry Trigger** (default): hold **LB + RB + Start** for half a second.
- **Exit Trigger** (default): hold **Back + Start** for half a second.

Default Bindings in Desktop Mode:

| Control | Action |
|---|---|
| Left stick | Moves the cursor |
| Right stick | Scrolls |
| LB / RB | Left / right mouse button |
| D-pad | Nudges the cursor one pixel at a time (repeats while held) |
| A | Enter |
| B | Esc |
| Y | On-screen keyboard (Win+Ctrl+O) |

The tray menu can also enter and leave Desktop Mode, pick the Active Profile, open **Settings...** (Mode Triggers,
Profiles, Bindings, movement tuning, per-Profile Trigger Overrides) and quit. Double-click the icon to open Settings.

Things to know:

- Settings are saved in `%AppData%\ControllerPC\settings.json`. If that file cannot be read, it is kept as a `.bad`
  backup next to it and the defaults are loaded.
- Windows does not let a normal program send input to a window running as administrator (UIPI). To control such
  windows, use **Restart as administrator** in the tray menu.
- Games and other programs still see the controller while Desktop Mode is on: ControllerPC only adds mouse and
  keyboard input, it does not hide the controller.

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
