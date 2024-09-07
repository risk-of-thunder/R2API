using HG.BlendableTypes;
using IL.RoR2.Networking;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace R2API;

/// <summary>
/// Static class utilized for implementing Light Replacements for a Model
/// </summary>
public static class SkinLightReplacement
{
    private static List<(SkinDef, LightReplacement[])> _tuples = new List<(SkinDef, LightReplacement[])>();
    private static Dictionary<SkinIndex, RuntimeLightReplacement[]> _skinIndexToLightReplacements = new Dictionary<SkinIndex, RuntimeLightReplacement[]>();

    private static bool _hooksSet = false;
    private static bool _catalogInitialized = false;

    /// <summary>
    /// Adds new LightReplacements that will be applied to a SkinDef
    /// </summary>
    /// <param name="targetSkinDef">The skin def which will apply the light replacements.</param>
    /// <param name="lightReplacements">The light replacements for the skin.</param>
    /// <returns>true if the light replacement was added succesfully, false otherwise.</returns>
    public static bool AddLightReplacement(SkinDef targetSkinDef, params LightReplacement[] lightReplacements)
    {
        SetHooks();

        if (_catalogInitialized)
        {
            SkinsPlugin.Logger.LogInfo($"Cannot add {lightReplacements.Length} light replacement(s) to the SkinDef {targetSkinDef} because the skin catalog has already initialized.");
            return false;
        }

        if (_tuples.Any(t => t.Item1 == targetSkinDef))
        {
            SkinsPlugin.Logger.LogInfo($"Cannot add {lightReplacements.Length} light replacement(s) to the SkinDef {targetSkinDef} because the skinDef already has light replacements associated to it.");
            return false;
        }
        _tuples.Add((targetSkinDef, lightReplacements));
        return true;
    }

    private static void SystemInit()
    {
        _catalogInitialized = true;

        foreach (var (skin, lightReplacements) in _tuples)
        {
            _skinIndexToLightReplacements.Add(skin.skinIndex, lightReplacements.Select(lr => new RuntimeLightReplacement
            {
                color = lr.color,
                path = Util.BuildPrefabTransformPath(skin.rootObject.transform, lr.light.transform)
            }).ToArray());
        }
    }

    private static void SetHooks()
    {
        if (_hooksSet)
            return;

        _hooksSet = true;

        On.RoR2.ModelSkinController.ApplySkin += SetLightOverrides;
    }

    internal static void UnsetHooks()
    {
        _hooksSet = false;

        On.RoR2.ModelSkinController.ApplySkin -= SetLightOverrides;
    }

    private static void SetLightOverrides(On.RoR2.ModelSkinController.orig_ApplySkin orig, ModelSkinController self, int skinIndex)
    {
        orig(self, skinIndex);

        SkinDef skin = HG.ArrayUtils.GetSafe(self.skins, skinIndex);

        if (!skin)
            return;

        if (_skinIndexToLightReplacements.TryGetValue(skin.skinIndex, out var lightReplacements))
        {
            foreach (RuntimeLightReplacement replacement in lightReplacements)
            {
                Transform transform = self.transform;
                Light light = transform.Find(replacement.path).GetComponent<Light>();
                if (light)
                {
                    light.color = replacement.color;
                }
            }
        }
    }

    private struct RuntimeLightReplacement
    {
        public string path;
        public Color color;
    }
}

/// <summary>
/// Struct that represents a LightReplacement for a Skin
/// </summary>
[Serializable]
public struct LightReplacement
{
    //Tooltips work as documentation as well
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [Tooltip("The light which we will modify it's color")]
    [PrefabReference]
    public Light light;
    [Tooltip($"The new color for the light")]
    public Color color;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
