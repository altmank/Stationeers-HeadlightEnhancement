# Headlight Enhancement

A Stationeers mod that makes worn helmet and headlamp lights omnidirectional, with minimal
shadows, further throw and higher intensity than the stock light. If you value being able to
see things clearly over realism, this is the mod for you.

[Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3180617960)

## Installing

Install [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad),
then subscribe on the Workshop. LaunchPad replaces the deprecated StationeersMods loader;
do not install both.

## Using it

Press `K` in game to cycle brightness. Each press steps to the next level and names it in
the console. Not `L`, which the game already uses for light on/off.

| Step | Intensity | Throw |
| --- | --- | --- |
| Stock | whatever the game shipped | unchanged |
| Faint | 0.6 | 28 m |
| Dim | 1.0 | 24 m |
| Low | 1.5 | 20 m |
| Medium | 2 | 30 m |
| High | 3 | 50 m |

Low is the default. Faint and Dim deliberately run a low intensity over a long range: the
built-in point light falls off as roughly `1 / (1 + 25d²/r²)`, so the blown-out look close to
a wall tracks intensity on its own while useful reach tracks `range × √intensity`. Dim keeps
about 98% of Low's reach for two thirds of its close-up glare.

Stock is captured from each light before the mod first touches it, so it restores the real
values rather than a guess. That makes it a genuine off switch without unloading the mod.

The chosen step is saved and survives a restart. Lights are adjusted when a game loads and
again whenever the set of worn lights changes, so changing helmet is handled for you.

Config, which LaunchPad exposes at startup:

| Setting | Default | What it does |
| --- | --- | --- |
| `Level` | `Low` | Step currently in use. The cycle key writes this. |
| `Intensity` | `2` | Intensity of the Medium step. |
| `Range` | `30` | Throw of the Medium step, in metres. |
| `Omnidirectional` | `true` | Converts the spotlight to a point light above Stock. |
| `IncludeFlashlights` | `false` | Also adjust handheld flashlights. |
| `CycleKey` | `K` | Cycle to the next step. L is the game's light on/off. |

## Multiplayer

Only the players who want brighter lights need the mod. The host does not need it, and players
with and without it, or on different versions of it, can play together.

- The mod changes how lights are drawn on your own screen and nothing else. It sends nothing over
  the network, and nothing it changes is part of what the game shares between players.
- On your screen it adjusts every head-worn light in the world, not only yours: other players'
  helmets and headlamps shine at your step too.
- A player without the mod sees every light as the game ships it, yours included. Your step never
  reaches anyone else's screen.
- The step and every other setting belong to each player, in their own config.
- Joining a game works the same as loading a save: the lights are adjusted once the world is
  loaded, and again whenever a light comes or goes, such as a player joining with a helmet on.
- Worked out from the game's code rather than from a multiplayer session.

## Building

Needs the .NET SDK and a Stationeers install with BepInEx and StationeersLaunchPad already
in place, because the build references the game's own assemblies.

```powershell
.\build.ps1            # build and stage .\package\
.\build.ps1 -Deploy    # also copy into the local mods folder
```

Point it at a non-default install with `-GameDir` or the `STATIONEERS_DIR` environment
variable.

## How it works

The game exposes every wearable light through `Thing.AllIWearableLights`, and each entry's
`Thing.Lights` holds the `ThingLight` wrappers around the real Unity `Light`. The mod walks
that list, filters to head-worn types, and writes intensity, range, colour and light type.
It keeps `ThingLight.Range` in step, since that field caches `light.range` at construction.

Versions up to 1.4.0 instead used `GameObject.Find` against a hardcoded scene path and a
fixed list of helmet prefab names. That broke as the character hierarchy changed and never
covered helmets added later.
