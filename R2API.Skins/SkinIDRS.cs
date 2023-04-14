using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace R2API;

/// <summary>
/// Class for adding Skin Specific ItemDisplayRuleSets for skin defs.
/// </summary>
public static class SkinIDRS
{
    private static List<(SkinDef, ItemDisplayRuleSet)> tuples = new List<(SkinDef, ItemDisplayRuleSet)>();
    private static Dictionary<SkinIndex, ItemDisplayRuleSet> skinIndexToCustomIDRS = new Dictionary<SkinIndex, ItemDisplayRuleSet>();

    private static bool hooksSet = false;
    private static bool catalogInitialized = false;
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

        if (catalogInitialized)
        {
            SkinsPlugin.Logger.LogInfo($"Cannot add pair {skinDef} && {ruleSet} as the SkinCatalog has already initialized.");
            return false;
        }

        if (tuples.Any(t => t.Item1 == skinDef))
        {
            SkinsPlugin.Logger.LogInfo($"Cannot add pair {skinDef} && {ruleSet}, the skin {skinDef} already has an entry associated to it.");
            return false;
        }

        tuples.Add((skinDef, ruleSet));
        return true;
    }

    [SystemInitializer(typeof(SkinCatalog))]
    private static void SystemInit()
    {
        catalogInitialized = true;
        foreach(var (skinDef, idrs) in tuples)
        {
            skinIndexToCustomIDRS.Add(skinDef.skinIndex, idrs);
        }
        tuples.Clear();
    }

    internal static void SetHooks()
    {
        if (hooksSet)
            return;
        hooksSet = true;

        On.RoR2.ModelSkinController.ApplySkin += SetCustomIDRS;
    }

    private static void SetCustomIDRS(On.RoR2.ModelSkinController.orig_ApplySkin orig, ModelSkinController self, int skinIndex)
    {
        orig(self, skinIndex);
        if (!self.characterModel)
            return;

        SkinDef skin = HG.ArrayUtils.GetSafe(self.skins, skinIndex);
        if (!skin)
            return;

        if(skinIndexToCustomIDRS.TryGetValue(skin.skinIndex, out var idrs))
        {
            self.characterModel.itemDisplayRuleSet = idrs;
        }
    }

    internal static void UnsetHooks()
    {
        hooksSet = false;
        On.RoR2.ModelSkinController.ApplySkin -= SetCustomIDRS;
    }
}
