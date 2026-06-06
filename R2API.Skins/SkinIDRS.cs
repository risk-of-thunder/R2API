using HG.Coroutines;
using RoR2;
using RoR2.ContentManagement;
using System.Collections;
using System.Collections.Generic;
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

        On.RoR2.ModelSkinController.ApplySkinAsync += SetCustomIDRS;
    }

    internal static void UnsetHooks()
    {
        hooksSet = false;
        On.RoR2.ModelSkinController.ApplySkinAsync -= SetCustomIDRS;
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

    //Some mods add idrs in RoR2Application.onLoad, this is currently the last moment of loading
    [InitDuringStartupPhase(GameInitPhase.PostProgressBar, int.MaxValue - 100)]
    private static void SystemInit()
    {
        initialized = true;

        var coroutine = new ParallelCoroutine();
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

                coroutine.Add(OverrideGroups(idrs, overrides));
            }
        }

        SkinsPlugin.Instance.StartCoroutine(coroutine);
        skinIDRSOverrides.Clear();
    }

    private static IEnumerator OverrideGroups(ItemDisplayRuleSet idrs, Dictionary<UnityEngine.Object, DisplayRuleGroup> overrides)
    {
        for (var i = 0; i < 1000; i++)
        {
            var foundNonInitialized = false;
            foreach (var group in idrs.keyAssetRuleGroups)
            {
                if (!group.keyAsset && (group.keyAssetAddress?.RuntimeKeyIsValid() ?? false))
                {
                    //Some mod just added new idrs and still generating runtime values, delaying overrides
                    foundNonInitialized = true;
                    break;
                }
            }

            if (!foundNonInitialized)
            {
                break;
            }

            yield return new WaitForSecondsRealtime(0.25f);
        }

        foreach (var kvp in overrides)
        {
            idrs.SetDisplayRuleGroup(kvp.Key, kvp.Value);
        }

        var enumerator = idrs.GenerateRuntimeValuesAsync();
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }
    }

    private static IEnumerator SetCustomIDRS(On.RoR2.ModelSkinController.orig_ApplySkinAsync orig, ModelSkinController self, int skinIndex, AsyncReferenceHandleUnloadType unloadType)
    {
        IEnumerator enumerator = orig(self, skinIndex, unloadType);

        SkinDef skin = HG.ArrayUtils.GetSafe(self.skins, skinIndex);
        if (!skin)
            return enumerator;

        var characterModel = self.characterModel;
        if (!skinToIDRS.TryGetValue(skin, out var idrs) || idrs == characterModel.itemDisplayRuleSet)
            return enumerator;

        characterModel.itemDisplayRuleSet = idrs;
        if (characterModel.body && characterModel.body.inventory)
        {
            characterModel.DisableAllItemDisplays();
            characterModel.SetEquipmentDisplay(EquipmentIndex.None);

            characterModel.body.inventory.wasRecentlyCreated = true;
            characterModel.UpdateItemDisplay(characterModel.body.inventory);
            characterModel.SetEquipmentDisplay(characterModel.inventoryEquipmentIndex);
        }

        return enumerator;
    }
}
