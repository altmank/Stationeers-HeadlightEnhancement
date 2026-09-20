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

Lights are adjusted when a game loads and again whenever the set of worn lights changes, so
changing helmet is handled for you. Press `L` to force a re-apply.

Intensity, range, omnidirectional mode and whether handheld flashlights are included can be
changed in the mod's config, which LaunchPad exposes at startup.

| Setting | Default | What it does |
| --- | --- | --- |
| `Intensity` | `2` | Light intensity. Stock is about 1. |
| `Range` | `30` | Throw in metres. Stock is about 12. |
| `Omnidirectional` | `true` | Converts the spotlight to a point light. |
| `IncludeFlashlights` | `false` | Also boost handheld flashlights. |
| `ReapplyKey` | `L` | Re-apply by hand. |

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
