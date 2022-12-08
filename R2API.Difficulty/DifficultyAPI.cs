using System;
using System.Collections.Concurrent;
using R2API.AutoVersionGen;
using R2API.ScriptableObjects;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace R2API;

/// <summary>
/// API for adding difficulties like Drizzle, Rainstorm, and Monsoon to the game. Does not cover "very easy, easy, ..., HAHAHAHA".
/// </summary>
[AutoVersion]
public partial class DifficultyAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".difficulty";
    public const string PluginName = R2API.PluginName + ".Difficulty";

    private static bool difficultyAlreadyAdded = false;

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
    [Obsolete(R2APISubmoduleDependency.propertyObsolete)]
    public static bool Loaded => true;

    /// <summary>
    /// Fired right before the hooks for the difficultyAPI are set. This is the last chance to add difficulties to the API.
    /// </summary>
    public static event EventHandler? DifficultyCatalogReady;

    private static readonly DifficultyIndex MinimumIndex = DifficultyIndex.Invalid;

    /// <summary>
    /// A dictionairy with ALL difficulty definitions. Post start, this includes both the vanilla ones and the ones added by R2API. Not all indexes are promised to be populated. Iterate over the keyset instead.
    /// </summary>
    public static ConcurrentDictionary<DifficultyIndex, DifficultyDef?>? difficultyDefinitions = new ConcurrentDictionary<DifficultyIndex, DifficultyDef?>();

    /// <summary>
    /// Add a DifficultyDef to the list of available difficulties.
    /// This must be called before the DifficultyCatalog inits, so before plugin.Start()
    /// You'll get your new index returned that you can work with for comparing to Run.Instance.selectedDifficulty.
    /// If this is called after the DifficultyCatalog inits then this will return -1/DifficultyIndex.Invalid and ignore the difficulty
    /// </summary>
    /// <param name="difficulty">The difficulty definition to add.</param>
    /// <returns>DifficultyIndex.Invalid if it fails. Your index otherwise.</returns>
    public static DifficultyIndex AddDifficulty(DifficultyDef? difficulty)
    {
        DifficultyAPI.SetHooks();
        return AddDifficulty(difficulty, false);
    }

    /// <summary>
    /// Add a DifficultyDef to the list of available difficulties.
    /// This must be called before the DifficultyCatalog inits, so before plugin.Start()
    /// You'll get your new index returned that you can work with for comparing to Run.Instance.selectedDifficulty.
    /// If this is called after the DifficultyCatalog inits then this will return -1/DifficultyIndex.Invalid and ignore the difficulty
    /// </summary>
    /// <param name="difficulty">The difficulty definition to add.</param>
    /// <param name="difficultyIcon">Sprite to use as the difficulty's icon.</param>
    /// <returns>DifficultyIndex.Invalid if it fails. Your index otherwise.</returns>
    public static DifficultyIndex AddDifficulty(DifficultyDef difficulty, Sprite difficultyIcon)
    {
        DifficultyAPI.SetHooks();
        difficulty.foundIconSprite = true;
        difficulty.iconSprite = difficultyIcon;
        return AddDifficulty(difficulty, false);
    }

    /// <summary>
    /// Add a DifficultyDef to the list of available difficulties.
    /// This must be called before the DifficultyCatalog inits, so before plugin.Start()
    /// You'll get your new index returned that you can work with for comparing to Run.Instance.selectedDifficulty.
    /// If this is called after the DifficultyCatalog inits then this will return -1/DifficultyIndex.Invalid and ignore the difficulty
    /// </summary>
    /// <param name="difficulty">The difficulty definition to add.</param>
    /// <param name="preferPositive">If you prefer to be appended to the array. In game version 1.0.0.X this means you will get all Eclipse modifiers as well when your difficulty is selected. </param>
    /// <returns>DifficultyIndex.Invalid if it fails. Your index otherwise.</returns>
    public static DifficultyIndex AddDifficulty(DifficultyDef? difficulty, bool preferPositive = false)
    {
        DifficultyAPI.SetHooks();
        if (difficultyAlreadyAdded)
        {
            DifficultyPlugin.Logger.LogError($"Tried to add difficulty: {difficulty.nameToken} after difficulty list was created");
            return DifficultyIndex.Invalid;
        }

        DifficultyIndex pendingIndex;
        if (preferPositive)
        {
            pendingIndex = DifficultyIndex.Count + difficultyDefinitions.Count;
        }
        else
        {
            pendingIndex = MinimumIndex - 1 - difficultyDefinitions.Count;
        }
        difficultyDefinitions[pendingIndex] = difficulty;
        return pendingIndex;
    }

    /// <summary>
    /// Add a DifficultyDef to the list of available difficulties using a SerializableDifficultyDef
    /// This must be called before the DifficultyCatalog inits, so before plugin.Start()
    /// You can get the DifficultyIndex from the SerializableDifficultyDef's DifficultyIndex property
    /// If this is called after the DifficultyCatalog inits, then the DifficultyIndex will return -1/DifficultyIndex.Invalid and ignore the difficulty
    /// </summary>
    /// <param name="serializableDifficultyDef">The SerializableDifficultyDef from which to create the DifficultyDef</param>
    public static void AddDifficulty(SerializableDifficultyDef serializableDifficultyDef)
    {
        DifficultyAPI.SetHooks();
        serializableDifficultyDef.CreateDifficultyDef();
        serializableDifficultyDef.DifficultyIndex = AddDifficulty(serializableDifficultyDef.DifficultyDef, serializableDifficultyDef.preferPositiveIndex);
    }

    private static bool _hooksEnabled = false;

    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        DifficultyCatalogReady?.Invoke(null, null);
        On.RoR2.DifficultyCatalog.GetDifficultyDef += GetExtendedDifficultyDef;
        On.RoR2.RuleDef.FromDifficulty += InitialiseRuleBookAndFinalizeList;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        On.RoR2.DifficultyCatalog.GetDifficultyDef -= GetExtendedDifficultyDef;
        On.RoR2.RuleDef.FromDifficulty -= InitialiseRuleBookAndFinalizeList;

        _hooksEnabled = false;
    }

    private static DifficultyDef GetExtendedDifficultyDef(On.RoR2.DifficultyCatalog.orig_GetDifficultyDef orig, DifficultyIndex difficultyIndex)
    {
        if (difficultyAlreadyAdded)
            return difficultyDefinitions[difficultyIndex];
        return orig(difficultyIndex);
    }

    private static RuleDef InitialiseRuleBookAndFinalizeList(On.RoR2.RuleDef.orig_FromDifficulty orig)
    {
        //Build defaults.
        RuleDef ruleChoices = orig();

        //Populate vanilla fields.
        var vanillaDefs = DifficultyCatalog.difficultyDefs;
        if (difficultyAlreadyAdded == false)
        {//Technically this function we are hooking is only called once, but in the weird case it's called multiple times, we don't want to add the definitions again.
            difficultyAlreadyAdded = true;
            for (int i = 0; i < vanillaDefs.Length; i++)
            {
                difficultyDefinitions[(DifficultyIndex)i] = vanillaDefs[i];
            }
        }

        //This basically replicates what the orig does, but that uses the hardcoded enum.Count to end it's loop, instead of the actual array length.
        foreach (var kv in difficultyDefinitions)
            try
            {
                //Skip vanilla rules.
                if (kv.Key >= DifficultyIndex.Invalid && kv.Key <= DifficultyIndex.Count)
                {
                    continue;
                }

                DifficultyDef difficultyDef = kv.Value;
                RuleChoiceDef choice = ruleChoices.AddChoice(Language.GetString(difficultyDef.nameToken), null, false);

                // If resource sprite has already been loaded, no need to reload it. Allows developers to load their own sprites.
                if (difficultyDef.foundIconSprite)
                {
                    choice.sprite = difficultyDef.iconSprite;
                }
                else
                {
                    choice.spritePath = difficultyDef.iconPath;
                }
                choice.tooltipNameToken = difficultyDef.nameToken;
                choice.tooltipNameColor = difficultyDef.color;
                choice.tooltipBodyToken = difficultyDef.descriptionToken;
                choice.serverTag = difficultyDef.serverTag;
                choice.excludeByDefault = false;
                choice.difficultyIndex = kv.Key;
            }
            catch { }

        ruleChoices.choices.Sort(delegate (RuleChoiceDef x, RuleChoiceDef y)
        {
            var xDiffValue = DifficultyCatalog.GetDifficultyDef(x.difficultyIndex).scalingValue;
            var yDiffValue = DifficultyCatalog.GetDifficultyDef(y.difficultyIndex).scalingValue;
            return xDiffValue.CompareTo(yDiffValue);
        });

        return ruleChoices;
    }
}
