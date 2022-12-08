using System;
using System.Collections.Generic;
using System.Linq;
using R2API.AutoVersionGen;
using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace R2API;

/// <summary>
/// API for adding custom TemporaryVisualEffects to CharacterBody components.
/// </summary>
[AutoVersion]
public static partial class TempVisualEffectAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".tempvisualeffect";
    public const string PluginName = R2API.PluginName + ".TempVisualEffect";

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
    [Obsolete(R2APISubmoduleDependency.propertyObsolete)]
    public static bool Loaded => true;
    /// <summary>
    /// Delegate used for checking if TemporaryVisualEffect should be active (bool active). <see cref="CharacterBody.UpdateSingleTemporaryVisualEffect(ref TemporaryVisualEffect, string, float, bool, string)"/>
    /// </summary>
    /// <param name="body"></param>
    public delegate bool EffectCondition(CharacterBody body);
    /// <summary>
    /// Delegate used for calculating the radius of a TemporaryVisualEffect (float radius). <see cref="CharacterBody.UpdateSingleTemporaryVisualEffect(ref TemporaryVisualEffect, string, float, bool, string)"/>
    /// </summary>
    /// <param name="body"></param>
    public delegate float EffectRadius(CharacterBody body);
    internal struct TemporaryVisualEffectInfo
    {
        public GameObject effectPrefab;
        public EffectRadius radius;
        public bool useBestFitRadius;
        public EffectCondition condition;
        public string childLocatorOverride;
    }
    internal static List<TemporaryVisualEffectInfo> temporaryVisualEffectInfos = new List<TemporaryVisualEffectInfo>();
    internal static Dictionary<CharacterBody, TemporaryVisualEffect[]> bodyToTemporaryVisualEffects = new Dictionary<CharacterBody, TemporaryVisualEffect[]>();
    internal static FixedSizeArrayPool<TemporaryVisualEffect> temporaryVisualEffectArrayPool = new FixedSizeArrayPool<TemporaryVisualEffect>(0);

    /// <summary>
    /// Adds a custom TemporaryVisualEffect to all CharacterBodies.
    /// Will be updated in the CharacterBody just after vanilla TemporaryVisualEffects.
    /// This overload lets you choose between scaling your effect based on <see cref="CharacterBody.radius"/> or <see cref="CharacterBody.bestFitRadius"/>.
    /// Returns true if successful.
    /// </summary>
    /// <param name="effectPrefab">MUST contain a TemporaryVisualEffect component.</param>
    /// <param name="condition"></param>
    /// <param name="useBestFitRadius"></param>
    /// <param name="childLocatorOverride"></param>
    public static bool AddTemporaryVisualEffect(GameObject effectPrefab, EffectCondition condition, bool useBestFitRadius = false, string childLocatorOverride = "")
    {
        if (effectPrefab == null)
        {
            TempVisualEffectPlugin.Logger.LogError($"Failed to add TemporaryVisualEffect: GameObject is null"); throw new ArgumentNullException($"{nameof(effectPrefab)} can't be null");
        }
        if (condition == null)
        {
            TempVisualEffectPlugin.Logger.LogError($"Failed to add TemporaryVisualEffect: {effectPrefab.name} no condition attached"); return false;
        }
        if (!effectPrefab.GetComponent<TemporaryVisualEffect>())
        {
            TempVisualEffectPlugin.Logger.LogError($"Failed to add TemporaryVisualEffect: {effectPrefab.name} GameObject has no TemporaryVisualEffect component"); return false;
        }

        AddTemporaryVisualEffectInternal(effectPrefab, null, condition, useBestFitRadius, childLocatorOverride);

        return true;
    }
    /// <summary>
    /// Adds a custom TemporaryVisualEffect to all CharacterBodies.
    /// Will be updated in the CharacterBody just after vanilla TemporaryVisualEffects.
    /// This overload lets you delegate exactly how to scale your effect.
    /// Returns true if successful.
    /// </summary>
    /// <param name="effectPrefab">MUST contain a TemporaryVisualEffect component.</param>
    /// <param name="radius"></param>
    /// <param name="condition"></param>
    /// <param name="childLocatorOverride"></param>
    public static bool AddTemporaryVisualEffect(GameObject effectPrefab, EffectRadius radius, EffectCondition condition, string childLocatorOverride = "")
    {
        if (effectPrefab == null)
        {
            TempVisualEffectPlugin.Logger.LogError($"Failed to add TemporaryVisualEffect: GameObject is null"); throw new ArgumentNullException($"{nameof(effectPrefab)} can't be null");
        }
        if (radius == null)
        {
            TempVisualEffectPlugin.Logger.LogError($"Failed to add TemporaryVisualEffect: {effectPrefab.name} no radius attached"); return false;
        }
        if (condition == null)
        {
            TempVisualEffectPlugin.Logger.LogError($"Failed to add TemporaryVisualEffect: {effectPrefab.name} no condition attached"); return false;
        }
        if (!effectPrefab.GetComponent<TemporaryVisualEffect>())
        {
            TempVisualEffectPlugin.Logger.LogError($"Failed to add TemporaryVisualEffect: {effectPrefab.name} GameObject has no TemporaryVisualEffect component"); return false;
        }

        AddTemporaryVisualEffectInternal(effectPrefab, radius, condition, false, childLocatorOverride);

        return true;
    }

    internal static void AddTemporaryVisualEffectInternal(GameObject effectPrefab, EffectRadius radius, EffectCondition condition, bool useBestFitRadius, string childLocatorOverride)
    {
        TempVisualEffectAPI.SetHooks();

        var newInfo = new TemporaryVisualEffectInfo
        {
            effectPrefab = effectPrefab,
            radius = radius,
            condition = condition,
            useBestFitRadius = useBestFitRadius,
            childLocatorOverride = childLocatorOverride,
        };
        temporaryVisualEffectInfos.Add(newInfo);
        temporaryVisualEffectArrayPool.lengthOfArrays++;
        TempVisualEffectPlugin.Logger.LogMessage($"Added new TemporaryVisualEffect: {newInfo}");
    }
    private static bool _hooksEnabled = false;

    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        On.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects += UpdateAllHook;
        CharacterBody.onBodyAwakeGlobal += BodyAwake;
        CharacterBody.onBodyDestroyGlobal += BodyDestroy;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        On.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects -= UpdateAllHook;
        CharacterBody.onBodyStartGlobal -= BodyAwake;
        CharacterBody.onBodyDestroyGlobal -= BodyDestroy;

        _hooksEnabled = false;
    }

    private static void BodyAwake(CharacterBody body)
    {
        bodyToTemporaryVisualEffects.Add(body, temporaryVisualEffectArrayPool.Request());
    }
    private static void BodyDestroy(CharacterBody body)
    {
        if (bodyToTemporaryVisualEffects.TryGetValue(body, out TemporaryVisualEffect[] temporaryVisualEffects))
        {
            temporaryVisualEffectArrayPool.Return(temporaryVisualEffects);
        }
        bodyToTemporaryVisualEffects.Remove(body);
    }
    private static void UpdateAllHook(On.RoR2.CharacterBody.orig_UpdateAllTemporaryVisualEffects orig, CharacterBody self)
    {
        orig(self);
        if (bodyToTemporaryVisualEffects.TryGetValue(self, out TemporaryVisualEffect[] temporaryVisualEffects))
        {
            for (int i = 0; i < temporaryVisualEffects.Length; i++)
            {
                TemporaryVisualEffectInfo info = temporaryVisualEffectInfos[i];
                self.UpdateSingleTemporaryVisualEffect(ref temporaryVisualEffects[i],
                    info.effectPrefab,
                    info.radius != null ? info.radius(self) : (info.useBestFitRadius ? self.bestFitRadius : self.radius),
                    info.condition(self),
                    info.childLocatorOverride);
            }
        }       
    }
}
