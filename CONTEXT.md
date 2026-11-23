# ControllerPC

A lightweight Windows tray app that lets an XInput controller drive the PC as a mouse and keyboard, switched on and off by user-chosen button sequences and/or combinations.

## Language

### Modes

**Desktop Mode**:
The state in which controller input is translated into mouse and keyboard input for the PC.
_Avoid_: mouse mode, active mode, controller mode

### Triggers

**Entry Trigger**:
The Mode Trigger that switches Desktop Mode on.

**Exit Trigger**:
The Mode Trigger that switches Desktop Mode off; configured separately from the Entry Trigger.

**Mode Trigger**:
An ordered list of one or more Steps that, when completed in order, switches Desktop Mode on or off.
_Avoid_: shortcut, hotkey, combo, macro

**Step**:
One element of a Mode Trigger, consisting of exactly one Chord.

**Chord**:
A set of one or more controller buttons held down at the same time.
_Avoid_: combo, combination

**Base Trigger**:
The global Entry or Exit Trigger used by every Profile that does not define its own Trigger Override. The Base Entry Trigger enters Desktop Mode with the Active Profile, and is only watched when the Active Profile has no Entry Trigger Override.

**Trigger Override**:
A Profile's own Entry or Exit Trigger, replacing the corresponding Base Trigger for that Profile. An Entry Trigger Override is watched even when its Profile is not active, and firing it makes that Profile the Active Profile.

**Effective Trigger**:
The Entry or Exit Trigger that actually applies to a Profile: its Trigger Override if defined, otherwise the Base Trigger.

**Step Gap**:
The maximum time allowed between releasing one Step and completing the next before an in-progress Mode Trigger attempt is abandoned.

**Final Hold**:
An optional requirement that the last Chord of a Mode Trigger be held for a minimum duration before the trigger fires.

**Driving Controller**:
The controller that fired the Entry Trigger; while Desktop Mode is on, it is the only controller whose input is translated and the only one that can fire the Exit Trigger.
_Avoid_: active controller, main pad

### Desktop Mode input

**Binding**:
The assignment of an Action to one digital controller button (including LT/RT treated as buttons and each D-pad direction) while in Desktop Mode.
_Avoid_: mapping, keybind

**Chord Binding**:
The assignment of an Action to a Chord of two or more buttons, active while the whole Chord is held in Desktop Mode.
_Avoid_: combo binding, combination

**Chord Wait**:
The short, per-Profile delay before a button that belongs to any Chord Binding performs its own Binding, giving its Chord time to complete.

**Action**:
What a Binding produces: a mouse button, a Key Combination, a Macro, a Nudge in one direction, a Profile switch ("Next Profile" or "Switch to Profile X"), or nothing. Mouse buttons and Key Combinations are held for as long as the button is held.

**Key Combination**:
A set of one or more keyboard keys pressed together as a single Action, e.g. `Ctrl+Alt+Tab` or `Win+D`.
_Avoid_: hotkey, shortcut

**Macro**:
An Action that taps an ordered list of Key Combinations, one after another, each time its button or Chord is pressed.
_Avoid_: script, sequence

**Macro Delay**:
The pause between two consecutive Key Combinations of a Macro.

**Nudge**:
A small, precise cursor movement, by default produced by the D-pad, as opposed to the analog movement of the left stick.

**Profile**:
A named set of Bindings, Chord Bindings, Right Stick Role, movement tuning and optional Trigger Overrides.
_Avoid_: preset, layout, config

**Active Profile**:
The one Profile currently in effect: the Profile Desktop Mode uses, and the one restored at the next launch.

**Right Stick Role**:
The selectable behavior of the right stick in Desktop Mode: scroll, cursor, or none. The left stick always moves the cursor.
