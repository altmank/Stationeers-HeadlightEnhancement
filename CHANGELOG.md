# Changelog

## 1.6.0

- `L` now cycles brightness instead of only re-applying: Stock, Low, Medium, High, wrapping.
  The step is named in the console on each press and saved between sessions.
- Stock is captured from each light before the mod first touches it, so it restores the real
  shipped values, including light type and shadow strength, and works as an off switch
  without unloading the mod.
- `Intensity` and `Range` now define the Medium step. `ReapplyKey` is renamed `CycleKey`.

## 1.5.0

- Fixed for current builds. The mod located lights by a hardcoded scene path that no
  longer resolves, and its error handler called a `ConsoleWindow.Print` overload whose
  signature changed, so the failure path threw instead of reporting.
- Lights are now found by type (`Helmet`, `GasMask`, `Headlamp`) through
  `Thing.AllIWearableLights` rather than by scene path and prefab name. Every helmet is
  covered, including Emergency, Emergency Space, Icarus, Marine and HARM, which shipped
  after 1.4.0.
- Re-applies automatically when the set of worn lights changes, so swapping helmets no
  longer needs a manual nudge. `L` still forces a re-apply.
- Intensity, range, omnidirectional mode and handheld flashlights are configurable.
- Logs through BepInEx instead of `ConsoleWindow`, so a game-side signature change can no
  longer take the mod down.
- Requires StationeersLaunchPad instead of the deprecated StationeersMods.
- New cover art.

## 1.4.0

Last release against the StationeersMods loader, April 2024.
