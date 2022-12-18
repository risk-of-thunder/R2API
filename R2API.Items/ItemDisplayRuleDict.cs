using EntityStates;
using R2API.ContentManagement;
using RoR2;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using R2API.MiscHelpers;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API;

public class ItemDisplayRuleDict
{

    /// <summary>
    /// Get the applicable rule for this charactermodel. Returns the default rules if no specific rule is found.
    /// </summary>
    /// <param name="bodyPrefabName">The model to look for. Null and empty strings are also accepted.</param>
    /// <returns>The item display rules for this model, or the default rules if no specifics are found.</returns>
    public ItemDisplayRule[]? this[string? bodyPrefabName]
    {
        get
        {
            if (string.IsNullOrEmpty(bodyPrefabName) || !Dictionary.ContainsKey(bodyPrefabName))
                return DefaultRules;
            else
                return Dictionary[bodyPrefabName];
        }
        set
        {
            if (string.IsNullOrEmpty(bodyPrefabName))
            {
                ItemsPlugin.Logger.LogWarning("DefaultRules overwritten with Indexer! Please set them with the constructor instead!");
                DefaultRules = value;
                return;
            }

            if (Dictionary.ContainsKey(bodyPrefabName))
            {
                Dictionary[bodyPrefabName] = value;
            }
            else
            {
                Dictionary.Add(bodyPrefabName, value);
            }
        }
    }


    /// <summary>
    /// Equivalent to using the set property of the indexer, but added bonus is the ability to ignore the array wrapper normally needed.
    /// </summary>
    /// <param name="bodyPrefabName"></param>
    /// <param name="itemDisplayRules"></param>
    public void Add(string? bodyPrefabName, params ItemDisplayRule[]? itemDisplayRules)
    {
        this[bodyPrefabName] = itemDisplayRules;
    }

    /// <summary>
    /// Safe way of getting a characters rules, with the promise that the out is always filled.
    /// </summary>
    /// <param name="bodyPrefabName"></param>
    /// <param name="itemDisplayRules">The specific rules for this model, or if false is returned, the default rules.</param>
    /// <returns>True if there's a specific rule for this model. False otherwise.</returns>
    public bool TryGetRules(string? bodyPrefabName, out ItemDisplayRule[] itemDisplayRules)
    {
        itemDisplayRules = this[bodyPrefabName];
        return bodyPrefabName != null && Dictionary.ContainsKey(bodyPrefabName);
    }
    /// <summary>
    /// The default rules to apply when no matching model is found.
    /// </summary>
    public ItemDisplayRule[]? DefaultRules { get; private set; }

    internal bool HasInvalidDisplays(out StringBuilder logger)
    {
        bool invalidDisplays = false;
        logger = new StringBuilder();
        foreach (var (bodyName, rules) in Dictionary)
        {
            for (int i = 0; i < rules.Length; i++)
            {
                ItemDisplayRule rule = rules[i];
                if (rule.ruleType != ItemDisplayRuleType.ParentedPrefab)
                    continue;

                if (!rule.followerPrefab)
                {
                    logger.AppendLine($"invalid follower prefab for entry {bodyName}. The follower prefab of entry N°{i} is null. (The ItemDisplayRule.ruleType is ItemDisplayRuleType.ParentedPrefab)");
                    invalidDisplays = true;
                    continue;
                }
                ItemDisplay itemDisplay = rule.followerPrefab.GetComponent<ItemDisplay>();
                if (!itemDisplay)
                {
                    logger.AppendLine($"Invalid follower prefab for entry {bodyName}. The follower prefab ({rule.followerPrefab}) does not have an ItemDisplay component. (The ItemDisplayRule.ruleType is ItemDisplayRuleType.ParentedPrefab) " +
                        $"The ItemDisplay model should have one and have at least a rendererInfo in it for having correct visibility levels.");
                    invalidDisplays = true;
                    continue;
                }

                if (itemDisplay.rendererInfos != null && itemDisplay.rendererInfos.Length != 0)
                {
                    logger.AppendLine($"Invalid follower prefab for entry {bodyName}. The follower prefab ({rule.followerPrefab}) has an ItemDisplay component, but no RendererInfos assigned. (The ItemDisplayRule.ruleType is ItemDisplayRuleType.ParentedPrefab)" +
                        $"The ItemDisplay model should have one and have at least a rendererInfo in it for having correct visibility levels.");
                    invalidDisplays = true;
                    continue;
                }
            }
        }
        return invalidDisplays;
    }

    internal Dictionary<string, ItemDisplayRule[]?> Dictionary { get; private set; }

    public ItemDisplayRuleDict(params ItemDisplayRule[]? defaultRules)
    {
        DefaultRules = defaultRules;
        Dictionary = new Dictionary<string, ItemDisplayRule[]?>();
    }
}
