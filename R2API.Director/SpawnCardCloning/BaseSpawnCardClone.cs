using RoR2;
using RoR2.Navigation;
using RoR2BepInExPack.GameAssetPaths;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace R2API.SpawnCardCloning;
public abstract class BaseSpawnCardClone<T> : ScriptableObject where T : SpawnCard
{
    [Header("Base For Cloning")]
    [Tooltip("If set to SpawnCard, it will try to match it directly for cloning. If set to Prefab, it will match spawncards with target prefab for cloning. If set to Prefabs, MultiCharacterSpawnCardClone will match spawncards with target prefabs")]
    public CloningType cloningType;
    [Tooltip("SpawnCard to use for matching")]
    public AddressReferencedAssets.AddressReferencedSpawnCard targetSpawnCard;
    [Tooltip("Set Spawn Card to return instead of cloned Spawn Cards")]
    public AddressReferencedAssets.AddressReferencedSpawnCard overrideReturnSpawnCard;
    [Tooltip("Prefab to use for matching the spawn card to clone")]
    public AddressReferencedAssets.AddressReferencedPrefab targetPrefab;
    [Tooltip("Prefab to override the original prefab of the cloned Spawn Card")]
    public AddressReferencedAssets.AddressReferencedPrefab prefab;
    [Tooltip("Override to which category cloned Spawn Card will go. Array incase if expected category name in some stage is slightly different than others. Breaks on first match")]
    public string[] overrideCategory = [];
    [Header("Cloning DirectorCard Settings")]
    public float selectionWeightMultiplier = 1f;
    public bool overrideSelectionWeight;
    public int selectionWeight;
    public bool overrideSpawnDistance;
    public DirectorCore.MonsterSpawnDistance spawnDistance;
    public bool overridePreventOverhead;
    public bool preventOverhead;
    public float minimumStageCompletionsMultiplier = 1f;
    public bool overrideMinimumStageCompletions;
    public int minimumStageCompletions;
    public bool overrideRequiredUnlockableDef;
    public AddressReferencedAssets.AddressReferencedUnlockableDef requiredUnlockableDef;
    public bool overrideForbiddenUnlockableDef;
    public AddressReferencedAssets.AddressReferencedUnlockableDef forbiddenUnlockableDef;
    [Header("Cloning SpawnCard Settings")]
    public float costMultiplier = 1f;
    public bool overrideCost;
    public int cost;
    public bool overrideHullSize;
    public HullClassification hullSize;
    public bool overrideNodeGraphType;
    public MapNodeGroup.GraphType nodeGrapthType;
    public bool overrideRequiredFlags;
    [EnumMask(typeof(NodeFlags))]
    public NodeFlags requiredFlags;
    public bool overrideForbiddenFlags;
    [EnumMask(typeof(NodeFlags))]
    public NodeFlags forbiddenFlags;
    public bool overrideEliteRules;
    [Tooltip("Default = default rules, ArtifactOnly = only elite when forced by the elite-only artifact, Lunar = special lunar elites only (+ regular w/ elite-only artifact)")]
    public SpawnCard.EliteRules eliteRules;
    public delegate bool CustomCondition(RebuildCardsInfo rebuildCardsInfo);
    public CustomCondition customCondition;
    public delegate void OnSpawnCardCloneAdded(RebuildCardsInfo rebuildCardsInfo);
    public OnSpawnCardCloneAdded onSpawnCardCloneAdded;
    private Dictionary<T, T> spawnCardOriginalToCloned = [];
    private Dictionary<DirectorCard, DirectorCard> directorCardOriginalToCloned = [];
    public virtual DirectorCard GetDirectorCard(DirectorCard originalDirectorCard)
    {
        if (!directorCardOriginalToCloned.TryGetValue(originalDirectorCard, out DirectorCard clonedDirectorCard))
        {
            clonedDirectorCard = new DirectorCard
            {
                selectionWeight = overrideSelectionWeight ? selectionWeight : (int)(originalDirectorCard.selectionWeight * selectionWeightMultiplier),
                spawnDistance = overrideSpawnDistance ? spawnDistance : originalDirectorCard.spawnDistance,
                preventOverhead = overridePreventOverhead ? preventOverhead : originalDirectorCard.preventOverhead,
                minimumStageCompletions = overrideMinimumStageCompletions ? minimumStageCompletions : (int)(originalDirectorCard.minimumStageCompletions * minimumStageCompletionsMultiplier),
                requiredUnlockableDef = overrideRequiredUnlockableDef ? requiredUnlockableDef.Asset : originalDirectorCard.requiredUnlockableDef,
                forbiddenUnlockableDef = overrideForbiddenUnlockableDef ? forbiddenUnlockableDef.Asset : originalDirectorCard.forbiddenUnlockableDef
            };
            directorCardOriginalToCloned.Add(originalDirectorCard, clonedDirectorCard);
        }
        return clonedDirectorCard;
    }
    public virtual T GetSpawnCard(T originalSpawnCard)
    {
        if (overrideReturnSpawnCard.Asset && overrideReturnSpawnCard.Asset is T t) return t;
        if (spawnCardOriginalToCloned.TryGetValue(originalSpawnCard, out T clonedSpawnCard)) return clonedSpawnCard;
        clonedSpawnCard = CreateInstance<T>();
        clonedSpawnCard.prefab = prefab.Asset;
        (clonedSpawnCard as ScriptableObject).name = name;
        UpdateValuesForClonedSpawnCard(originalSpawnCard, clonedSpawnCard);
        return clonedSpawnCard;
    }
    public virtual void Register()
    {
        if (!SpawnCardCloningAPI.allSpawnCardClones.Contains(this)) SpawnCardCloningAPI.allSpawnCardClones.Add(this);
        if (cloningType == CloningType.SpawnCard)
        {
            SpawnCard spawnCard = targetSpawnCard.Asset;
            if (spawnCard)
            {
                if (SpawnCardCloningAPI.spawnCardClonesFromOriginalSpawnCard.TryGetValue(spawnCard, out List<object> list) && !list.Contains(this))
                {
                    list.Add(this);
                }
                else
                {
                    SpawnCardCloningAPI.spawnCardClonesFromOriginalSpawnCard.Add(spawnCard, [this]);
                }
            }
        }
        if (cloningType == CloningType.Prefab)
        {
            GameObject gameObject = targetPrefab.Asset;
            if (gameObject)
            {
                if (SpawnCardCloningAPI.spawnCardClonesFromPrefab.TryGetValue(gameObject, out List<object> list) && !list.Contains(this))
                {
                    list.Add(this);
                }
                else
                {
                    SpawnCardCloningAPI.spawnCardClonesFromPrefab.Add(gameObject, [this]);
                }
            }
        }
    }
    public virtual void Unregister()
    {
        if (SpawnCardCloningAPI.allSpawnCardClones.Contains(this)) SpawnCardCloningAPI.allSpawnCardClones.Remove(this);
        SpawnCard spawnCard = targetSpawnCard.Asset;
        if (spawnCard)
        {
            if (SpawnCardCloningAPI.spawnCardClonesFromOriginalSpawnCard.TryGetValue(spawnCard, out List<object> list) && list.Contains(this)) list.Remove(this); 
        }
        GameObject gameObject = targetPrefab.Asset;
        if (gameObject)
        {
            if (SpawnCardCloningAPI.spawnCardClonesFromPrefab.TryGetValue(gameObject, out List<object> list) && list.Contains(this)) list.Remove(this);
        }
    }
    public virtual void UpdateValuesForClonedSpawnCards()
    {
        foreach (var pair in spawnCardOriginalToCloned)
        {
            T originalSpawnCard = pair.Key;
            T clonedSpawnCard = pair.Value;
            if (!originalSpawnCard || !clonedSpawnCard) continue;
            UpdateValuesForClonedSpawnCard(originalSpawnCard, clonedSpawnCard);
        }
    }
    public virtual void UpdateValuesForClonedSpawnCard(T originalSpawnCard, T clonedSpawnCard)
    {
        clonedSpawnCard.directorCreditCost = overrideCost ? cost : (int)(clonedSpawnCard.directorCreditCost * costMultiplier);
        clonedSpawnCard.eliteRules = overrideEliteRules ? eliteRules : clonedSpawnCard.eliteRules;
        clonedSpawnCard.requiredFlags = overrideRequiredFlags ? requiredFlags : clonedSpawnCard.requiredFlags;
        clonedSpawnCard.hullSize = overrideHullSize ? hullSize : clonedSpawnCard.hullSize;
        clonedSpawnCard.nodeGraphType = overrideNodeGraphType ? nodeGrapthType : clonedSpawnCard.nodeGraphType;
        clonedSpawnCard.forbiddenFlags = overrideForbiddenFlags ? forbiddenFlags : clonedSpawnCard.forbiddenFlags;
    }
    public enum CloningType
    {
        SpawnCard,
        Prefab,
        Prefabs
    }
}
