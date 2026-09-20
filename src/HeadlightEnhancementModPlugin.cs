using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace HeadlightEnhancementMod;

[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
public class HeadlightEnhancementModPlugin : BaseUnityPlugin
{
    public const string pluginGuid = "net.xceled.stationeers.headlightenhancementmod";
    public const string pluginName = "HeadlightEnhancementMod";
    public const string pluginVersion = "1.5.0";

    private ConfigEntry<float> _intensity;
    private ConfigEntry<float> _range;
    private ConfigEntry<bool> _omnidirectional;
    private ConfigEntry<bool> _includeFlashlights;
    private ConfigEntry<KeyCode> _reapplyKey;

    // AllIWearableLights grows as helmets, masks and lamps Awake, so a single pass on
    // game load misses anything spawned or picked up later. Cheap count watch instead.
    private int _lastLightCount = -1;

    private void Awake()
    {
        _intensity = Config.Bind("Light", "Intensity", 2f, "Helmet light intensity. Stock is about 1.");
        _range = Config.Bind("Light", "Range", 30f, "Helmet light throw in metres. Stock is about 12.");
        _omnidirectional = Config.Bind("Light", "Omnidirectional", true,
            "Convert the spotlight to a point light so it lights everything around you.");
        _includeFlashlights = Config.Bind("Light", "IncludeFlashlights", false,
            "Also boost handheld flashlights, not just head-worn lights.");
        _reapplyKey = Config.Bind("Input", "ReapplyKey", KeyCode.L, "Re-apply the light changes by hand.");

        UnityEngine.Object.DontDestroyOnLoad(gameObject);
        GameManager.OnGameStateChange += OnGameStateChange;
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChange -= OnGameStateChange;
    }

    private void OnGameStateChange()
    {
        _lastLightCount = -1;
        EnhanceHeadlights();
    }

    private void Update()
    {
        if (!GameManager.IsRunning)
        {
            _lastLightCount = -1;
            return;
        }

        if (KeyManager.GetButtonDown(_reapplyKey.Value))
        {
            EnhanceHeadlights();
            return;
        }

        if (Thing.AllIWearableLights.Count != _lastLightCount)
        {
            EnhanceHeadlights();
        }
    }

    private void EnhanceHeadlights()
    {
        if (!GameManager.IsRunning)
        {
            return;
        }

        // Copy first: Awake/OnDestroy on any wearable mutates the static list.
        IWearableLight[] wearables;
        try
        {
            wearables = Thing.AllIWearableLights.ToArray();
        }
        catch (Exception e)
        {
            Logger.LogWarning($"Could not read the wearable light list: {e.Message}");
            return;
        }

        int adjusted = 0;
        foreach (IWearableLight wearable in wearables)
        {
            if (!ShouldAdjust(wearable))
            {
                continue;
            }

            Thing thing = wearable.GetAsThing;
            if (thing == null)
            {
                continue;
            }

            List<ThingLight> lights = thing.Lights;
            if (lights == null)
            {
                continue;
            }

            foreach (ThingLight thingLight in lights)
            {
                if (AdjustLight(thingLight))
                {
                    adjusted++;
                }
            }
        }

        _lastLightCount = Thing.AllIWearableLights.Count;
        Logger.LogInfo($"Adjusted {adjusted} light(s) across {wearables.Length} wearable light source(s).");
    }

    private bool ShouldAdjust(IWearableLight wearable)
    {
        switch (wearable)
        {
            // Helmet covers the hardsuit family; GasMask covers the space helmet family;
            // Headlamp is the wearable lamp. All resolved by type, never by prefab name.
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

    private bool AdjustLight(ThingLight thingLight)
    {
        Light light = thingLight?.Light;
        if (light == null)
        {
            return false;
        }

        light.intensity = _intensity.Value;
        light.range = _range.Value;
        light.color = Color.white;
        light.shadowStrength = 0f;
        if (_omnidirectional.Value)
        {
            light.type = LightType.Point;
        }

        // ThingLight caches range at construction; keep the copy in step so the game's
        // own occlusion maths does not fight the new value.
        thingLight.Range = _range.Value;
        return true;
    }
}
