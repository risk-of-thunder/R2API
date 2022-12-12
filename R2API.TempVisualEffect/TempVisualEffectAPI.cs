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
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;

    private static bool _TVEsAdded;

    /// <summary>
    /// Delegate used for checking if TVE should be active (bool active). <see cref="CharacterBody.UpdateSingleTemporaryVisualEffect(ref TemporaryVisualEffect, string, float, bool, string)"/>
    /// </summary>
    /// <param name="body"></param>
    public delegate bool EffectCondition(CharacterBody body);
    public struct TemporaryVisualEffectStruct
    {
        public TemporaryVisualEffect effect;
        public GameObject effectPrefab;
        public bool useBestFitRadius;
        public EffectCondition condition;
        public string childLocatorOverride;
        public string effectName;
    }

    internal static List<TemporaryVisualEffectStruct> tves = new List<TemporaryVisualEffectStruct>();

    /// <summary>
    /// Adds a custom TemporaryVisualEffect (TVEs) to the static tves List and Dict.
    /// Custom TVEs are used and updated in the CharacterBody just after vanilla TVEs.
    /// Must be called before R2APIContentPackProvider.WhenContentPackReady. Will fail if called after.
    /// Returns true if successful.
    /// </summary>
    /// <param name="effectPrefab">MUST contain a TemporaryVisualEffect component.</param>
    /// <param name="useBestFitRadius"></param>
    /// /// <param name="condition"></param>
    /// <param name="childLocatorOverride"></param>
    public static bool AddTemporaryVisualEffect(GameObject effectPrefab, EffectCondition condition, bool useBestFitRadius = false, string childLocatorOverride = "")
    {
        TempVisualEffectAPI.SetHooks();

        if (effectPrefab == null)
        {
            TempVisualEffectPlugin.Logger.LogError($"Failed to add TVE: GameObject is null"); throw new ArgumentNullException($"{nameof(effectPrefab)} can't be null");
        }

        var prefabName = effectPrefab.name;

        if (_TVEsAdded)
        {
            TempVisualEffectPlugin.Logger.LogError($"Failed to add TVE: {prefabName} after TVE list was created"); return false;
        }
        if (condition == null)
        {
            TempVisualEffectPlugin.Logger.LogError($"Failed to add TVE: {prefabName} no condition attached"); return false;
        }
        if (tves.Any(name => name.effectName == prefabName))
        {
            TempVisualEffectPlugin.Logger.LogError($"Failed to add TVE: {prefabName} name already exists in list"); return false;
        }
        if (!effectPrefab.GetComponent<TemporaryVisualEffect>())
        {
            TempVisualEffectPlugin.Logger.LogError($"Failed to add TVE: {prefabName} GameObject has no TemporaryVisualEffect component"); return false;
        }

        var newTVE = new TemporaryVisualEffectStruct();

        newTVE.effectPrefab = effectPrefab;
        newTVE.useBestFitRadius = useBestFitRadius;
        newTVE.condition = condition;
        newTVE.childLocatorOverride = childLocatorOverride;
        newTVE.effectName = prefabName;

        tves.Add(newTVE);
        TempVisualEffectPlugin.Logger.LogMessage($"Added new TVE: {newTVE}");

        return true;
    }

    private static bool _hooksEnabled = false;

    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        On.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects += UpdateAllHook;
        R2APIContentPackProvider.WhenAddingContentPacks += DontAllowNewEntries;
        CharacterBody.onBodyStartGlobal += BodyStart;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        On.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects -= UpdateAllHook;
        R2APIContentPackProvider.WhenAddingContentPacks -= DontAllowNewEntries;
        CharacterBody.onBodyStartGlobal -= BodyStart;

        _hooksEnabled = false;
    }

    private static void BodyStart(CharacterBody body)
    {
        if (!body.gameObject.GetComponent<R2APITVEController>())
        {
            body.gameObject.AddComponent<R2APITVEController>();
        }
    }

    private static void DontAllowNewEntries()
    {
        _TVEsAdded = true;
    }

    private static void UpdateAllHook(On.RoR2.CharacterBody.orig_UpdateAllTemporaryVisualEffects orig, CharacterBody self)
    {
        orig(self);

        var controller = self.gameObject.GetComponent<R2APITVEController>();
        if (controller)
        {
            for (int i = 0; i < controller.localTVEs.Count; i++)
            {
                var tve = controller.localTVEs[i];
                self.UpdateSingleTemporaryVisualEffect(ref tve.effect, tve.effectPrefab, tve.useBestFitRadius ? self.bestFitRadius : self.radius, tve.condition.Invoke(self), tve.childLocatorOverride);
                controller.localTVEs[i] = tve;
            }
        }
    }
}

/// <summary>
/// Contains a local list of custom TemporaryVisualEffects for each CharacterBody.
/// </summary>
public class R2APITVEController : MonoBehaviour
{
    public List<TempVisualEffectAPI.TemporaryVisualEffectStruct> localTVEs = new(TempVisualEffectAPI.tves);
}
