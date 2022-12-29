using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using R2API.MiscHelpers;
using RoR2;

namespace R2API;

/// <summary>
/// A class for adding new RuleDefs and RuleCategories to the game's RuleCatalog
/// </summary>
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class RuleCatalogExtras
{
    /// <summary>
    /// The Rules submodule's plugin GUID
    /// </summary>
    public const string PluginGUID = R2API.PluginGUID + ".rules";
    /// <summary>
    /// The Rules submodule's plugin name
    /// </summary>
    public const string PluginName = R2API.PluginName + ".Rules";

    /// <summary>
    /// An index that represents the Difficulty RuleCategoryDef Index
    /// </summary>
    public const int DifficultyCategoryIndex = 0;

    /// <summary>
    /// An index that represents the Expansions RuleCategoryDef Index
    /// </summary>
    public const int ExpansionsCategoryIndex = 1;

    /// <summary>
    /// An index that represents the Artifacts RuleCategoryDef Index
    /// </summary>
    public const int ArtifactsCategoryIndex = 2;

    /// <summary>
    /// An index that represents the Items RuleCategoryDef index, hidden by default
    /// </summary>
    public const int ItemsCategoryIndex = 3;

    /// <summary>
    /// An index that represents the Equipments RuleCategoryDef index, hidden by default
    /// </summary>
    public const int EquipmentsCategoryIndex = 4;

    /// <summary>
    /// An index that represents the Misc RuleCategoryDef index, hidden by default
    /// </summary>
    public const int MiscCategoryIndex = 5;

    /// <summary>
    /// An event which is fired right before the custom categories are added, this is the last chance for adding custom rules.
    /// </summary>
    public static event EventHandler? RuleCatalogReady
    {
        add
        {
            SetHooks();
            _ruleCatalogReady += value;
        }
        remove
        {
            _ruleCatalogReady -= value;
        }
    }
    private static event EventHandler? _ruleCatalogReady;

    private static bool _hooksEnabled = false;
    private static bool _addedCustomEntries = false;
    private static Dictionary<int, List<RuleDef>> categoryToCustomRules = new Dictionary<int, List<RuleDef>>();
    private static List<RuleCategoryDef> ruleCategoryDefs = new List<RuleCategoryDef>();

    /// <summary>
    /// Adds a new category to the RuleCatalog
    /// </summary>
    /// <param name="category">The category to add</param>
    /// <returns>An index that represents this category's index in the catalog, -1 if the custom entries where already added.</returns>
    public static int AddCategory(RuleCategoryDef category)
    {
        SetHooks();

        if (_addedCustomEntries)
            return -1;

        ruleCategoryDefs.Add(category);
        return 5 + ruleCategoryDefs.Count - 1;
    }

    /// <summary>
    /// Adds a new Rule to the RuleCatalog
    /// </summary>
    /// <param name="ruleDef">The RuleDef to add</param>
    /// <param name="ruleCategoryDefIndex">An index that represents which Category to add the <paramref name="ruleDef"/>, this can be either the interger returned by <see cref="AddCategory(RuleCategoryDef)"/>, or one of the constant intergers, such as <see cref="DifficultyCategoryIndex"/></param>
    public static void AddRuleToCatalog(RuleDef ruleDef, int ruleCategoryDefIndex)
    {
        SetHooks();

        if (_addedCustomEntries)
            return;

        if (!categoryToCustomRules.ContainsKey(ruleCategoryDefIndex))
        {
            categoryToCustomRules[ruleCategoryDefIndex] = new List<RuleDef>();
        }
        categoryToCustomRules[ruleCategoryDefIndex].Add(ruleDef);
    }

    /// <summary>
    /// Finds a custom ruleDef by comparing the ruleDef's global name with <paramref name="ruleDefGlobalName"/>
    /// </summary>
    /// <param name="ruleDefGlobalName">The global name of the ruleDef to find</param>
    /// <returns>The rule def, null if it wasnt found</returns>
    public static RuleDef FindCustomRuleDef(string ruleDefGlobalName)
    {
        SetHooks();

        if (_addedCustomEntries)
            return RuleCatalog.FindRuleDef(ruleDefGlobalName);

        foreach (RuleDef ruleDef in categoryToCustomRules.Values.SelectMany(x => x))
        {
            if(string.Equals(ruleDefGlobalName, ruleDef.globalName, StringComparison.OrdinalIgnoreCase))
            {
                return ruleDef;
            }
        }
        return null;
    }

    /// <summary>
    /// Finds a custom RuleChoiceDef by comparing the ruleChoiceDef's global name with <paramref name="ruleChoiceDefGlobalName"/>
    /// </summary>
    /// <param name="ruleChoiceDefGlobalName">The global name of the rule choice def to find</param>
    /// <returns></returns>
    public static RuleChoiceDef FindCustomChoiceDef(string ruleChoiceDefGlobalName)
    {
        SetHooks();

        if (_addedCustomEntries)
            return RuleCatalog.FindChoiceDef(ruleChoiceDefGlobalName);

        foreach (RuleChoiceDef choiceDef in categoryToCustomRules.Values.SelectMany(x => x).SelectMany(x => x.choices))
        {
            if(string.Equals(ruleChoiceDefGlobalName, choiceDef.globalName, StringComparison.OrdinalIgnoreCase))
            {
                return choiceDef;
            }
        }
        return null;
    }
    #region hooks
    internal static void SetHooks()
    {
        if(_hooksEnabled)
        {
            return;
        }

        RuleCatalog.availability.CallWhenAvailable(FinishRulebookSetup);
        IL.RoR2.PreGameController.RecalculateModifierAvailability -= SupportCollectionRequirement;
        IL.RoR2.PreGameController.RecalculateModifierAvailability += SupportCollectionRequirement;
        _hooksEnabled = true;
    }
    internal static void UnsetHooks()
    {
        IL.RoR2.PreGameController.RecalculateModifierAvailability -= SupportCollectionRequirement;
        _hooksEnabled = false;
    }

    private static void SupportCollectionRequirement(MonoMod.Cil.ILContext il)
    {
        ILCursor c = new ILCursor(il);
        ILLabel label = null;
        c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt<RuleBook>(nameof(RuleBook.IsChoiceActive)),
                x => x.MatchBr(out _),
                x => x.MatchLdcI4(1),
                x => x.MatchCallOrCallvirt<SerializableBitArray>("set_Item"));

        label = c.MarkLabel();
        if(label == null)
        {
            R2API.Logger.LogError("ILHook on PreGameController.RecalculateModifierAvilability failed, could not find ILLabel");
            return;
        }

        c.Index = 0;
        bool ILFound = c.TryGotoNext(x => x.MatchRet(),
            x => x.MatchLdcI4(0),
            x => x.MatchStloc(0),
            x => x.MatchBr(out _),
            x => x.MatchLdloc(0),
            x => x.MatchCallOrCallvirt(typeof(RuleCatalog), nameof(RuleCatalog.GetChoiceDef)),
            x => x.MatchStloc(1));

        if(!ILFound)
        {
            R2API.Logger.LogError("ILHook on PreGameController.RecalculateModifierAvilability failed, could not get into position for calling the delegate.");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Stloc_1);
        c.Emit(OpCodes.Stloc_0);
        c.EmitDelegate<Func<PreGameController, RuleChoiceDef, int, bool>>(HandleCollectionRequirement);
        c.Emit(OpCodes.Brtrue, label);

        static bool HandleCollectionRequirement(PreGameController preGameController, RuleChoiceDef currentRule, int choiceIndex)
        {
            if(currentRule.TryCastToExtendedRuleChoiceDef(out ExtendedRuleChoiceDef extendedChoice))
            {
                preGameController.unlockedChoiceMask[choiceIndex] = extendedChoice.requiredUnlockables.Count == 0 || PreGameControllerHelper.AnyUserHasAllUnlockables(extendedChoice.requiredUnlockables);
                preGameController.dependencyChoiceMask[choiceIndex] = extendedChoice.requiredChoiceDefs.Count == 0 || PreGameControllerHelper.AreAllChoicesActive(preGameController, extendedChoice.requiredChoiceDefs);
                preGameController.entitlementChoiceMask[choiceIndex] = extendedChoice.requiredEntitlementDefs.Count == 0 || PreGameControllerHelper.AnyUserHasAllEntitlements(extendedChoice.requiredEntitlementDefs, true) || PreGameControllerHelper.AnyUserHasAllEntitlements(extendedChoice.requiredEntitlementDefs, false);
                preGameController.requiredExpansionEnabledChoiceMask[choiceIndex] = extendedChoice.requiredExpansionDefs.Count == 0 || PreGameControllerHelper.AreAllExpansionsActive(preGameController, extendedChoice.requiredExpansionDefs);
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    private static void FinishRulebookSetup()
    {
        if (_addedCustomEntries)
            return;

        _ruleCatalogReady?.Invoke(null, null);

        RuleCatalog.allCategoryDefs.AddRange(ruleCategoryDefs);

        foreach(var (categoryIndex, ruleDef) in categoryToCustomRules)
        {
            try
            {
                AddRulesToCategory(categoryIndex, ruleDef);
            }
            catch(Exception e)
            {
                R2API.Logger.LogError(e);
            }
        }
    }

    private static void AddRulesToCategory(int categoryIndex, List<RuleDef> ruleDefs)
    {
        RuleCategoryDef category = RuleCatalog.GetCategoryDef(categoryIndex);
        foreach(RuleDef ruleDef in ruleDefs)
        {
            try
            {
                ruleDef.category = category;
                ruleDef.globalIndex = RuleCatalog.allRuleDefs.Count;
                category.children.Add(ruleDef);
                RuleCatalog.allRuleDefs.Add(ruleDef);

                if (RuleCatalog.highestLocalChoiceCount < ruleDef.choices.Count)
                {
                    RuleCatalog.highestLocalChoiceCount = ruleDef.choices.Count;
                }

                RuleCatalog.ruleDefsByGlobalName[ruleDef.globalName] = ruleDef;
                for (int i = 0; i < ruleDef.choices.Count; i++)
                {
                    RuleChoiceDef choiceDef = ruleDef.choices[i];
                    choiceDef.localIndex = i;
                    choiceDef.globalIndex = RuleCatalog.allChoicesDefs.Count;
                    RuleCatalog.allChoicesDefs.Add(choiceDef);

                    RuleCatalog.ruleChoiceDefsByGlobalName[choiceDef.globalName] = choiceDef;

                    if (choiceDef.requiredUnlockable)
                    {
                        HG.ArrayUtils.ArrayAppend(ref RuleCatalog._allChoiceDefsWithUnlocks, choiceDef);
                    }

                    if(choiceDef.TryCastToExtendedRuleChoiceDef(out var extended))
                    {
                        if(extended.requiredUnlockables.Count > 0)
                        {
                            HG.ArrayUtils.ArrayAppend(ref RuleCatalog._allChoiceDefsWithUnlocks, extended);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                R2API.Logger.LogError(e);
            }
        }
    }
    #endregion
}
