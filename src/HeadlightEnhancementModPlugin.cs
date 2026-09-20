using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace HeadlightEnhancementMod;

/// <summary>Brightness steps the cycle key walks through.</summary>
public enum LightLevel
{
    Stock,
    Low,
    Medium,
    High,
}

[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
public class HeadlightEnhancementModPlugin : BaseUnityPlugin
{
    public const string pluginGuid = "net.xceled.stationeers.headlightenhancementmod";
    public const string pluginName = "HeadlightEnhancementMod";
    public const string pluginVersion = "1.6.0";

    private static readonly LightLevel[] Cycle =
        { LightLevel.Stock, LightLevel.Low, LightLevel.Medium, LightLevel.High };

    /// <summary>What a light looked like before this mod first touched it.</summary>
    private readonly struct Original
    {
        public readonly float Intensity;
        public readonly float Range;
        public readonly LightType Type;
        public readonly float ShadowStrength;

        public Original(Light light)
        {
            Intensity = light.intensity;
            Range = light.range;
            Type = light.type;
            ShadowStrength = light.shadowStrength;
        }
    }

    private readonly Dictionary<Light, Original> _originals = new Dictionary<Light, Original>();

    private ConfigEntry<LightLevel> _level;
    private ConfigEntry<float> _intensity;
    private ConfigEntry<float> _range;
    private ConfigEntry<bool> _omnidirectional;
    private ConfigEntry<bool> _includeFlashlights;
    private ConfigEntry<KeyCode> _cycleKey;

    // AllIWearableLights grows as helmets, masks and lamps Awake, so a single pass on game
    // load misses anything spawned or picked up later. Cheap count watch instead.
    private int _lastLightCount = -1;

    private void Awake()
    {
        _level = Config.Bind("Light", "Level", LightLevel.Medium,
            "Brightness step in use. The cycle key walks Stock, Low, Medium, High and saves the choice here.");
        _intensity = Config.Bind("Light", "Intensity", 2f,
            "Intensity of the Medium step. Stock is whatever the game shipped.");
        _range = Config.Bind("Light", "Range", 30f, "Throw of the Medium step, in metres.");
        _omnidirectional = Config.Bind("Light", "Omnidirectional", true,
            "Convert the spotlight to a point light on every step above Stock.");
        _includeFlashlights = Config.Bind("Light", "IncludeFlashlights", false,
            "Also adjust handheld flashlights, not just head-worn lights.");
        // Not L: the game binds that to ToggleLight, and KeyWrap._ToggleLight was built
        // without SecondaryKeys, so it fires on a bare L regardless of modifiers. Sharing
        // the key would change brightness every time the light is switched on or off.
        _cycleKey = Config.Bind("Input", "CycleKey", KeyCode.K,
            "Cycle to the next brightness step. Avoid L, which the game uses for light on/off.");

        UnityEngine.Object.DontDestroyOnLoad(gameObject);
        GameManager.OnGameStateChange += OnGameStateChange;
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChange -= OnGameStateChange;
    }

    private void OnGameStateChange()
    {
        // Lights from the previous session are gone; their captured originals are dead weight.
        _originals.Clear();
        _lastLightCount = -1;
        Apply();
    }

    private void Update()
    {
        if (!GameManager.IsRunning)
        {
            _lastLightCount = -1;
            return;
        }

        // GetButtonDown already swallows input while the console is open, but not while a
        // labeller or chat field has focus, where the key is a letter being typed.
        if (!InputWindowBase.IsInputWindow && KeyManager.GetButtonDown(_cycleKey.Value))
        {
            CycleLevel();
            return;
        }

        if (Thing.AllIWearableLights.Count != _lastLightCount)
        {
            Apply();
        }
    }

    private void CycleLevel()
    {
        int i = Array.IndexOf(Cycle, _level.Value);
        _level.Value = Cycle[(i + 1) % Cycle.Length];

        int adjusted = Apply();
        string message = _level.Value == LightLevel.Stock
            ? "Headlight: Stock"
            : "Headlight: " + _level.Value + " (" + Describe(_level.Value) + ")";

        Logger.LogInfo(message + ", " + adjusted + " light(s)");
        try
        {
            ConsoleWindow.PrintAction(message);
        }
        catch (Exception e)
        {
            // Never let a UI call take the mod down; the light change is the real feedback.
            Logger.LogWarning("Could not print to the console: " + e.Message);
        }
    }

    private string Describe(LightLevel level)
    {
        var settings = Settings(level);
        return "intensity " + settings.intensity.ToString("0.#") + ", " + settings.range.ToString("0") + " m";
    }

    /// <summary>Intensity and range for a boosted step. Stock is restored from capture instead.</summary>
    private (float intensity, float range) Settings(LightLevel level)
    {
        switch (level)
        {
            case LightLevel.Low: return (1.5f, 20f);
            case LightLevel.Medium: return (_intensity.Value, _range.Value);
            case LightLevel.High: return (3f, 50f);
            default: return (0f, 0f);
        }
    }

    private int Apply()
    {
        if (!GameManager.IsRunning)
        {
            return 0;
        }

        // Copy first: Awake and OnDestroy on any wearable mutate the static list.
        IWearableLight[] wearables;
        try
        {
            wearables = Thing.AllIWearableLights.ToArray();
        }
        catch (Exception e)
        {
            Logger.LogWarning("Could not read the wearable light list: " + e.Message);
            return 0;
        }

        PruneDestroyedLights();

        int adjusted = 0;
        foreach (IWearableLight wearable in wearables)
        {
            if (!ShouldAdjust(wearable))
            {
                continue;
            }

            Thing thing = wearable.GetAsThing;
            List<ThingLight> lights = (thing == null) ? null : thing.Lights;
            if (lights == null)
            {
                continue;
            }

            foreach (ThingLight thingLight in lights)
            {
                if (ApplyTo(thingLight))
                {
                    adjusted++;
                }
            }
        }

        _lastLightCount = Thing.AllIWearableLights.Count;
        return adjusted;
    }

    private bool ShouldAdjust(IWearableLight wearable)
    {
        switch (wearable)
        {
            // Helmet covers the hardsuit family, GasMask the space helmet family, Headlamp
            // the wearable lamp. Resolved by type, never by prefab name.
            case Helmet:
            case GasMask:
            case Headlamp:
                return true;
            case Flashlight:
                return _includeFlashlights.Value;
            default:
                return false;
        }
    }

    private bool ApplyTo(ThingLight thingLight)
    {
        Light light = (thingLight == null) ? null : thingLight.Light;
        if (light == null)
        {
            return false;
        }

        Original original;
        if (!_originals.TryGetValue(light, out original))
        {
            original = new Original(light);
            _originals[light] = original;
        }

        if (_level.Value == LightLevel.Stock)
        {
            light.intensity = original.Intensity;
            light.range = original.Range;
            light.type = original.Type;
            light.shadowStrength = original.ShadowStrength;
            thingLight.Range = original.Range;
            return true;
        }

        var settings = Settings(_level.Value);
        light.intensity = settings.intensity;
        light.range = settings.range;
        light.color = Color.white;
        light.shadowStrength = 0f;
        if (_omnidirectional.Value)
        {
            light.type = LightType.Point;
        }

        // ThingLight caches range at construction; keep the copy in step so the game's own
        // occlusion maths does not fight the new value.
        thingLight.Range = settings.range;
        return true;
    }

    private void PruneDestroyedLights()
    {
        List<Light> dead = null;
        foreach (Light light in _originals.Keys)
        {
            if (light == null)
            {
                if (dead == null)
                {
                    dead = new List<Light>();
                }

                dead.Add(light);
            }
        }

        if (dead == null)
        {
            return;
        }

        foreach (Light light in dead)
        {
            _originals.Remove(light);
        }
    }
}
