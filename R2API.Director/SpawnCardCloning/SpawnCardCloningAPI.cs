using BepInEx;
using MonoMod.RuntimeDetour;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace R2API.SpawnCardCloning;
public static partial class SpawnCardCloningAPI
{
    #region Internal
    internal static List<object> allSpawnCardClones = [];
    internal static Dictionary<SpawnCard, List<object>> spawnCardClonesFromOriginalSpawnCard = [];
    internal static Dictionary<GameObject, List<object>> spawnCardClonesFromPrefab = [];
    internal static GameObjectArrayDictionary<List<object>> multiCharacterSpawnCardClonesFromPrefab = new GameObjectArrayDictionary<List<object>>();
    private static HashSet<DccsPool> appliedDccsPools = [];
    private static Hook ClassicStageInfoRebuildCardsHook;
    private static bool handledMixEnemyMonsterCards;
    internal static void SetHooks()
    {
        ClassicStageInfoRebuildCardsHook = new Hook(typeof(ClassicStageInfo).GetMethod(nameof(ClassicStageInfo.RebuildCards), BindingFlags.NonPublic | BindingFlags.Instance), typeof(SpawnCardCloningAPI).GetMethod(nameof(ClassicStageInfo_RebuildCards), BindingFlags.NonPublic | BindingFlags.Static), new HookConfig { Priority = int.MaxValue });
    }
    internal static void UnsetHooks()
    {
        if (ClassicStageInfoRebuildCardsHook != null) ClassicStageInfoRebuildCardsHook.Undo();
    }
    private static void ClassicStageInfo_RebuildCards(On.RoR2.ClassicStageInfo.orig_RebuildCards orig, ClassicStageInfo self, DirectorCardCategorySelection forcedMonsterCategory, DirectorCardCategorySelection forcedInteractableCategory)
    {
        RebuildCardsInfo rebuildCardsInfo = new RebuildCardsInfo
        {
            classicStageInfo = self,
            forcedInteractableCategory = forcedMonsterCategory,
            forcedMonsterCategory = forcedInteractableCategory
        };
        if (!handledMixEnemyMonsterCards)
        {
            HandleDirectorCardCategorySelection(RoR2Content.mixEnemyMonsterCards, rebuildCardsInfo);
            handledMixEnemyMonsterCards = true;
        }
        HandlDccsPool(self.monsterDccsPool, rebuildCardsInfo);
        HandlDccsPool(self.interactableDccsPool, rebuildCardsInfo);
        orig(self, forcedMonsterCategory, forcedInteractableCategory);
    }
    private static void HandlDccsPool(DccsPool dccsPool, RebuildCardsInfo rebuildCardsInfo)
    {
        rebuildCardsInfo.dccsPool = dccsPool;
        if (!dccsPool || appliedDccsPools.Contains(dccsPool)) return;
        try
        {
            HandlePoolCategories(dccsPool.poolCategories, rebuildCardsInfo);
        }
        catch (Exception e)
        {
            DirectorPlugin.Logger.LogError("Failed to setup cloned spawn cards for " + dccsPool.name);
            DirectorPlugin.Logger.LogError(e);
        }
        appliedDccsPools.Add(dccsPool);
    }
    private static void HandlePoolCategories(DccsPool.Category[] categories, RebuildCardsInfo rebuildCardsInfo)
    {
        rebuildCardsInfo.categories = categories;
        if (categories == null) return;
        foreach (DccsPool.Category category in categories)
        {
            rebuildCardsInfo.dccsPoolCategory = category;
            foreach (DccsPool.PoolEntry poolEntry in category.alwaysIncluded) HandlePoolEntry(poolEntry, rebuildCardsInfo);
            foreach (DccsPool.PoolEntry poolEntry in category.includedIfConditionsMet)HandlePoolEntry(poolEntry, rebuildCardsInfo);
            foreach (DccsPool.PoolEntry poolEntry in category.includedIfNoConditionsMet)HandlePoolEntry(poolEntry, rebuildCardsInfo);
        }
    }
    private static void HandlePoolEntry(DccsPool.PoolEntry poolEntry, RebuildCardsInfo rebuildCardsInfo)
    {
        rebuildCardsInfo.poolEntry = poolEntry;
        if (poolEntry == null) return;
        DirectorCardCategorySelection directorCardCategorySelection = poolEntry.dccs;
        if (!directorCardCategorySelection) return;
        HandleDirectorCardCategorySelection(directorCardCategorySelection, rebuildCardsInfo);
    }
    private static void HandleDirectorCardCategorySelection(DirectorCardCategorySelection directorCardCategorySelection, RebuildCardsInfo rebuildCardsInfo)
    {
        rebuildCardsInfo.directorCardCategorySelection = directorCardCategorySelection;
        Dictionary<DirectorCard, string> overrideCategories = [];
        HashSet<string> validCategories = [];
        foreach (DirectorCardCategorySelection.Category category in directorCardCategorySelection.categories)
        {
            if (category.name.IsNullOrWhiteSpace() || validCategories.Contains(category.name)) continue;
            validCategories.Add(category.name);
        }
        for (int i = 0; i < directorCardCategorySelection.categories.Length; i++)
        {
            ref DirectorCardCategorySelection.Category category = ref directorCardCategorySelection.categories[i];
            ref DirectorCard[] directorCards = ref category.cards;
            rebuildCardsInfo.category = category;
            if (directorCards == null || directorCards.Length == 0) continue;
            HashSet<GameObject> appliedClonedSpawnCardPrefabs = [];
            HashSet<GameObject[]> appliedClonedMultiCharacterSpawnCardPrefabs = new HashSet<GameObject[]>(new GameObjectArrayComparer());
            foreach (DirectorCard directorCard in directorCards)
            {
                rebuildCardsInfo.directorCard = directorCard;
                SpawnCard spawnCard = directorCard.spawnCard;
                rebuildCardsInfo.spawnCard = spawnCard;
                if (!spawnCard && directorCard.spawnCardReference != null) spawnCard = directorCard.spawnCardReference.Asset ? directorCard.spawnCardReference.Asset as SpawnCard : directorCard.spawnCardReference.LoadAssetAsync<SpawnCard>().WaitForCompletion();
                if (!spawnCard || !spawnCard.prefab) continue;
                if (spawnCard is CharacterSpawnCard characterSpawnCard) HandleSpawnCard<CharacterSpawnCardClone, CharacterSpawnCard>(ref category, appliedClonedSpawnCardPrefabs, directorCard, characterSpawnCard, overrideCategories, validCategories, rebuildCardsInfo);
                if (spawnCard is InteractableSpawnCard interactableSpawnCard) HandleSpawnCard<InteractableSpawnCardClone, InteractableSpawnCard>(ref category, appliedClonedSpawnCardPrefabs, directorCard, interactableSpawnCard, overrideCategories, validCategories, rebuildCardsInfo);
                if (spawnCard is MultiCharacterSpawnCard multiCharacterSpawnCard && !HandleMultiCharacterSpawnCard(ref category, appliedClonedMultiCharacterSpawnCardPrefabs, directorCard, multiCharacterSpawnCard, overrideCategories, validCategories, rebuildCardsInfo)) HandleSpawnCard<MultiCharacterSpawnCardClone, MultiCharacterSpawnCard>(ref category, appliedClonedSpawnCardPrefabs, directorCard, multiCharacterSpawnCard, overrideCategories, validCategories, rebuildCardsInfo);
            }
        }
        foreach (var pair in overrideCategories)
        {
            DirectorCard directorCard = pair.Key;
            if (directorCard == null) continue;
            for (int i = 0; i < directorCardCategorySelection.categories.Length; i++)
            {
                ref DirectorCardCategorySelection.Category category = ref directorCardCategorySelection.categories[i];
                if (category.name == pair.Value)
                {
                    int length = category.cards.Length;
                    Array.Resize(ref category.cards, length + 1);
                    category.cards[length] = directorCard;
                    break;
                }
            }
        }

    }
    private static void HandleSpawnCard<T1, T2>(ref DirectorCardCategorySelection.Category category, HashSet<GameObject> appliedClonedSpawnCardPrefabs, DirectorCard directorCard, T2 spawnCard, Dictionary<DirectorCard, string> overrideCategories, HashSet<string> validCategories, RebuildCardsInfo rebuildCardsInfo) where T1 : BaseSpawnCardClone<T2> where T2 : SpawnCard
    {
        if (spawnCardClonesFromPrefab.TryGetValue(spawnCard.prefab, out List<object> list) || spawnCardClonesFromOriginalSpawnCard.TryGetValue(spawnCard, out list))
        {
            if (list == null || list.Count == 0) return;
            foreach (var item in list)
            {
                rebuildCardsInfo.spawnCardClone = item;
                if (!(item is T1 spawnCardClone)) continue;
                if (appliedClonedSpawnCardPrefabs.Contains(spawnCardClone.prefab)) continue;
                appliedClonedSpawnCardPrefabs.Add(spawnCardClone.prefab);
                DirectorCard clonedDirectorCard = spawnCardClone.GetDirectorCard(directorCard);
                clonedDirectorCard.spawnCard = spawnCardClone.GetSpawnCard(spawnCard);
                rebuildCardsInfo.clonedDirectorCard = clonedDirectorCard;
                rebuildCardsInfo.clonedSpawnCard = clonedDirectorCard.spawnCard;
                if (spawnCardClone.customCondition != null && !spawnCardClone.customCondition.Invoke(rebuildCardsInfo)) continue;
                spawnCardClone.onSpawnCardCloneAdded?.Invoke(rebuildCardsInfo);
                if (spawnCardClone.overrideCategory != null && spawnCardClone.overrideCategory.Length != 0)
                {
                    foreach (string categoryName in spawnCardClone.overrideCategory)
                    {
                        if (validCategories.Contains(categoryName))
                        {
                            overrideCategories.Add(clonedDirectorCard, categoryName);
                            break;
                        }
                    }
                    continue;
                }
                int length = category.cards.Length;
                Array.Resize(ref category.cards, length + 1);
                category.cards[length] = clonedDirectorCard;
            }
        }
    }
    private static bool HandleMultiCharacterSpawnCard(ref DirectorCardCategorySelection.Category category, HashSet<GameObject[]> appliedClonedSpawnCardPrefabs, DirectorCard directorCard, MultiCharacterSpawnCard spawnCard, Dictionary<DirectorCard, string> overrideCategories, HashSet<string> validCategories, RebuildCardsInfo rebuildCardsInfo)
    {
        if (multiCharacterSpawnCardClonesFromPrefab.TryGetValue(spawnCard.masterPrefabs, false, out List<object> list))
        {
            if (list == null || list.Count == 0) return false;
            foreach (var item in list)
            {
                rebuildCardsInfo.spawnCardClone = item;
                if (!(item is MultiCharacterSpawnCardClone spawnCardClone)) continue;
                GameObject[] gameObjects = spawnCardClone.GetMasterPrefabs();
                if (appliedClonedSpawnCardPrefabs.Contains(gameObjects)) continue;
                appliedClonedSpawnCardPrefabs.Add(gameObjects);
                DirectorCard directorCard1 = spawnCardClone.GetDirectorCard(directorCard);
                directorCard1.spawnCard = spawnCardClone.GetSpawnCard(spawnCard);
                rebuildCardsInfo.clonedDirectorCard = directorCard1;
                rebuildCardsInfo.clonedSpawnCard = directorCard1.spawnCard;
                if (spawnCardClone.customCondition != null && !spawnCardClone.customCondition.Invoke(rebuildCardsInfo)) continue;
                spawnCardClone.onSpawnCardCloneAdded?.Invoke(rebuildCardsInfo);
                if (spawnCardClone.overrideCategory != null && spawnCardClone.overrideCategory.Length != 0)
                {
                    foreach (string categoryName in spawnCardClone.overrideCategory)
                    {
                        if (validCategories.Contains(categoryName))
                        {
                            overrideCategories.Add(directorCard1, categoryName);
                            break;
                        }
                    }
                    continue;
                }
                int length = category.cards.Length;
                Array.Resize(ref category.cards, length + 1);
                category.cards[length] = directorCard1;
            }
            return true;
        }
        else
        {
            return false;
        }
    }
    #endregion Internal
}
