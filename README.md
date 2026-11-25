# ControllerPC

A lightweight Windows tray app that lets an XInput controller use the PC as a mouse and
keyboard, being able to switch between normal XInput and M&K with customizable commands.

## Using it

ControllerPC runs in the notification area (system tray). Hold the Entry Trigger on any connected controller to
switch **Desktop Mode** on. If you want to switch it off, hold the Exit Trigger. The Default Friggers are:

- **Entry Trigger**: hold **LB + RB + Start** for half a second.
- **Exit Trigger**: hold **Back + Start** for half a second.

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

## Install

Builds the app, installs it to `%LocalAppData%\Programs\ControllerPC` and adds a Start Menu shortcut, so
searching "ControllerPC" in the Start menu finds it. It builds from
source, so it needs the .NET SDK (see Build). Quit the app first when updating.

```bash
powershell -ExecutionPolicy Bypass -File scripts/install.ps1
```

To remove it (add `-RemoveSettings` to also delete your settings):

```bash
powershell -ExecutionPolicy Bypass -File scripts/uninstall.ps1
```

The app icon (`src/ControllerPC.App/Assets/ControllerPC.ico`) is generated from the same drawing code as the tray
icon. After changing that drawing, regenerate it:

```bash
dotnet run --project tools/ControllerPC.IconGenerator -- src/ControllerPC.App/Assets/ControllerPC.ico
```

## Publish

```bash
dotnet publish src/ControllerPC.App -c Release
```

Produces a single framework-dependent `ControllerPC.exe` (needs the .NET 8 Desktop Runtime).
Settings are stored in `%AppData%\ControllerPC\settings.json`.
