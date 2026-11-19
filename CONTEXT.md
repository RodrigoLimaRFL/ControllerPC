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
The global Entry or Exit Trigger used by every Profile that does not define its own Trigger Override.

**Trigger Override**:
A Profile's own Entry or Exit Trigger, replacing the corresponding Base Trigger for that Profile.

**Effective Trigger**:
The Entry or Exit Trigger that actually applies to a Profile: its Trigger Override if defined, otherwise the Base Trigger.

**Final Hold**:
An optional requirement that the last Chord of a Mode Trigger be held for a minimum duration before the trigger fires.

**Driving Controller**:
The controller that fired the Entry Trigger; while Desktop Mode is on, it is the only controller whose input is translated and the only one that can fire the Exit Trigger.
_Avoid_: active controller, main pad

### Desktop Mode input

**Binding**:
The assignment of an Action to one digital controller button (including LT/RT treated as buttons and each D-pad direction) while in Desktop Mode.
_Avoid_: mapping, keybind

**Action**:
What a Binding produces: a mouse button, a Key Combination, a Nudge in one direction, a Profile switch ("Next Profile" or "Switch to Profile X"), or nothing. Mouse buttons and Key Combinations are held for as long as the button is held.

**Key Combination**:
A set of one or more keyboard keys pressed together as a single Action, e.g. `Ctrl+Alt+Tab` or `Win+D`.
_Avoid_: hotkey, shortcut, macro

**Nudge**:
A small, precise cursor movement, by default produced by the D-pad, as opposed to the analog movement of the left stick.

**Profile**:
A named set of Bindings, Right Stick Role, movement tuning and optional Trigger Overrides; exactly one Profile is the Active Profile at any time.
_Avoid_: preset, layout, config

**Right Stick Role**:
The selectable behavior of the right stick in Desktop Mode: scroll, cursor, or none. The left stick always moves the cursor.
