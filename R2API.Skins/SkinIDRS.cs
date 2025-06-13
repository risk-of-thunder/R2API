using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace R2API;

/// <summary>
/// Class for adding Skin Specific ItemDisplayRuleSets for skin defs.
/// </summary>
public static class SkinIDRS
{
    private static readonly Dictionary<SkinDef, ItemDisplayRuleSet> skinToIDRS = new();
    private static readonly Dictionary<SkinDef, Dictionary<UnityEngine.Object, DisplayRuleGroup>> skinIDRSOverrides = new();

    private static bool hooksSet = false;
    private static bool initialized = false;

    internal static void SetHooks()
    {
        if (hooksSet)
            return;

        hooksSet = true;

        On.RoR2.ModelSkinController.ApplySkin += SetCustomIDRS;
        RoR2Application.onLoad += SystemInit;
    }

    internal static void UnsetHooks()
    {
        hooksSet = false;
        On.RoR2.ModelSkinController.ApplySkin -= SetCustomIDRS;
        RoR2Application.onLoad -= SystemInit;
    }

    /// <summary>
    /// Adds a pair of SkinDef and ItemDisplayRuleSet
    /// <para>Ingame, once the Skin is applied to the model, the default IDRS will be swapped for the one specified in <paramref name="ruleSet"/></para>
    /// </summary>
    /// <param name="skinDef"></param>
    /// <param name="ruleSet"></param>
    /// <returns>True if added succesfully, false otherwise</returns>
    public static bool AddPair(SkinDef skinDef, ItemDisplayRuleSet ruleSet)
    {
        SetHooks();

        if (initialized)
        {
            SkinsPlugin.Logger.LogInfo($"Cannot add pair {skinDef} && {ruleSet} as the SkinIDRS has already initialized.");
            return false;
        }

        if (skinToIDRS.ContainsKey(skinDef))
        {
            SkinsPlugin.Logger.LogInfo($"Cannot add pair {skinDef} && {ruleSet}, the skin {skinDef} already has an entry associated to it.");
            return false;
        }

        skinToIDRS[skinDef] = ruleSet;
        return true;
    }

    /// <summary>
    /// Adds a displayRuleGroup override for ItemDef/EquipmentDef. If there was no IDRS for the skin, a clone of the IDRS from CharacterModel from a body prefab will be taken as base.
    /// </summary>
    /// <param name="skinDef"></param>
    /// <param name="keyAsset">ItemDef/EquipmentDef</param>
    /// <param name="displayRuleGroup"></param>
    /// <returns></returns>
    public static bool AddGroupOverride(SkinDef skinDef, UnityEngine.Object keyAsset, DisplayRuleGroup displayRuleGroup)
    {
        SetHooks();

        if (initialized)
        {
            SkinsPlugin.Logger.LogInfo($"Cannot add group SkinIDRS has already initialized.");
            return false;
        }

        if (!skinIDRSOverrides.TryGetValue(skinDef, out var overrides))
        {
            skinIDRSOverrides[skinDef] = overrides = new();
        }

        overrides[keyAsset] = displayRuleGroup;

        return true;
    }

    private static void SystemInit()
    {
        initialized = true;

        foreach (var body in BodyCatalog.allBodyPrefabBodyBodyComponents)
        {
            if (!body ||
                !body.TryGetComponent<ModelLocator>(out var modelLocator) ||
                !modelLocator.modelTransform ||
                !modelLocator.modelTransform.TryGetComponent<CharacterModel>(out var characterModel))
            {
                continue;
            }

            var baseIDRS = characterModel.itemDisplayRuleSet;
            foreach (var skin in SkinCatalog.GetBodySkinDefs(body.bodyIndex))
            {
                if (!skinIDRSOverrides.TryGetValue(skin, out var overrides))
                {
                    if (!skinToIDRS.ContainsKey(skin))
                    {
                        skinToIDRS[skin] = baseIDRS;
                    }
                    continue;
                }

                if (!skinToIDRS.TryGetValue(skin, out var idrs))
                {
                    skinToIDRS[skin] = idrs = baseIDRS ? UnityEngine.Object.Instantiate(baseIDRS) : ScriptableObject.CreateInstance<ItemDisplayRuleSet>();
                }

                foreach (var kvp in overrides)
                {
                    idrs.SetDisplayRuleGroup(kvp.Key, kvp.Value);
                }

                var async = idrs.GenerateRuntimeValuesAsync();
                while (async.MoveNext()) ;
            }
        }

        skinIDRSOverrides.Clear();
    }

    private static void SetCustomIDRS(On.RoR2.ModelSkinController.orig_ApplySkin orig, ModelSkinController self, int skinIndex)
    {
        orig(self, skinIndex);

        SkinDef skin = HG.ArrayUtils.GetSafe(self.skins, skinIndex);
        if (!skin)
            return;

        if (!skinToIDRS.TryGetValue(skin, out var idrs))
        {
            return;
        }

        self.characterModel.itemDisplayRuleSet = idrs;
    }
}
