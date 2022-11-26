using System;
using System.Collections.Generic;
using System.Linq;
using HG;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using RoR2.ExpansionManagement;
using UnityEngine;

// Changing namespace to R2API.Director would be breaking
namespace R2API;

public static partial class DirectorAPI
{
    private static DirectorCardCategorySelection _dccsMixEnemyArtifact;

    private static bool _hooksEnabled = false;

    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        On.RoR2.ClassicStageInfo.Start += ApplyChangesOnStart;
        IL.RoR2.ClassicStageInfo.HandleMixEnemyArtifact += SwapVanillaDccsWithOurs;

        On.RoR2.SceneCatalog.Init += InitStageEnumToSceneDefs;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        On.RoR2.ClassicStageInfo.Start -= ApplyChangesOnStart;
        IL.RoR2.ClassicStageInfo.HandleMixEnemyArtifact -= SwapVanillaDccsWithOurs;

        On.RoR2.SceneCatalog.Init -= InitStageEnumToSceneDefs;

        _hooksEnabled = false;
    }

    private static void ApplyChangesOnStart(On.RoR2.ClassicStageInfo.orig_Start orig, ClassicStageInfo classicStageInfo)
    {
        classicStageInfo.PortToNewSystem();
        classicStageInfo.ApplyChanges();
        orig(classicStageInfo);
    }

    private static void SwapVanillaDccsWithOurs(ILContext il)
    {
        var cursor = new ILCursor(il);

        if (cursor.TryGotoNext(
                i => i.MatchCallOrCallvirt<DirectorCardCategorySelection>(nameof(DirectorCardCategorySelection.CopyFrom))
            ))
        {
            cursor.EmitDelegate<Func<DirectorCardCategorySelection, DirectorCardCategorySelection>>(SwapDccs);
        }

        static DirectorCardCategorySelection SwapDccs(DirectorCardCategorySelection vanillaDccs)
        {
            return _dccsMixEnemyArtifact;
        }
    }

    private static void InitStageEnumToSceneDefs(On.RoR2.SceneCatalog.orig_Init orig)
    {
        orig();

        var groups = SceneCatalog.allStageSceneDefs.GroupBy(sceneDef => GetStageEnumFromSceneDef(sceneDef), sceneDef => sceneDef);

        foreach (var group in groups)
        {
            VanillaStageToSceneDefs[group.Key] = group.ToArray();
        }
    }

    // Some stages are still on the old system, those don't have any dlc related content
    private static void PortToNewSystem(this ClassicStageInfo classicStageInfo)
    {
        PortToNewMonsterSystem(classicStageInfo);
        PortToNewInteractableSystem(classicStageInfo);
    }

    internal static void ApplyChanges(this ClassicStageInfo classicStageInfo)
    {
        var stageInfo = GetStageInfo(classicStageInfo);

        BackupOrRestoreClassicStageInfoToOriginalState(classicStageInfo, stageInfo);

        ApplyMonsterChanges(classicStageInfo, stageInfo);
        ApplyInteractableChanges(classicStageInfo, stageInfo);
        ApplySettingsChanges(classicStageInfo, stageInfo);
    }

    private static StageInfo GetStageInfo(ClassicStageInfo classicStageInfo)
    {
        var stageInfo = new StageInfo
        {
            stage = Stage.Custom,
            CustomStageName = "",
        };

        var sceneInfo = classicStageInfo.GetComponent<SceneInfo>();
        if (!sceneInfo) return stageInfo;

        var sceneDef = sceneInfo.sceneDef;
        if (!sceneDef) return stageInfo;
        stageInfo.stage = GetStageEnumFromSceneDef(sceneDef);
        if (stageInfo.stage == Stage.Custom)
        {
            stageInfo.CustomStageName = sceneDef.baseSceneName;
        }

        return stageInfo;
    }

    private static void ApplySettingsChanges(ClassicStageInfo classicStageInfo, StageInfo stageInfo)
    {
        var stageSettings = GetStageSettings(classicStageInfo);
        StageSettingsActions?.Invoke(stageSettings, stageInfo);
        SetStageSettings(classicStageInfo, stageSettings);
    }

    private static void PortToNewMonsterSystem(ClassicStageInfo classicStageInfo)
    {
        var isUsingOldSystem = !classicStageInfo.monsterDccsPool && classicStageInfo.monsterCategories;
        if (isUsingOldSystem)
        {
            R2API.Logger.LogInfo($"Current scene is using old monster dccs system, porting to new one");

            var newDccsPool = ScriptableObject.CreateInstance<DccsPool>();
            newDccsPool.name = "R2API_" + "dp" + classicStageInfo.name + "Monsters";

            var dccsPoolCategories = new List<DccsPool.Category>();

            PortOldStandardMonsterCategoriesToNewDccsPoolSystem(classicStageInfo.monsterCategories, dccsPoolCategories);

            if (classicStageInfo.possibleMonsterFamilies != null)
            {
                PortOldMonsterFamiliesToNewDccsPoolSystem(classicStageInfo.possibleMonsterFamilies, dccsPoolCategories);
            }

            newDccsPool.poolCategories = dccsPoolCategories.ToArray();
            classicStageInfo.monsterDccsPool = newDccsPool;
        }
    }

    private static void PortOldStandardMonsterCategoriesToNewDccsPoolSystem(DirectorCardCategorySelection monsterCategories, List<DccsPool.Category> dccsPoolCategories)
    {
        var standardCategory = new DccsPool.Category();
        standardCategory.name = Helpers.MonsterPoolCategories.Standard;
        standardCategory.categoryWeight = Helpers.MonsterPoolCategories.StandardWeight;

        standardCategory.alwaysIncluded = Array.Empty<DccsPool.PoolEntry>();
        standardCategory.includedIfConditionsMet = Array.Empty<DccsPool.ConditionalPoolEntry>();
        standardCategory.includedIfNoConditionsMet = new DccsPool.PoolEntry[] {
            new DccsPool.PoolEntry() {
                dccs = monsterCategories,
                weight = 1
            }
        };

        dccsPoolCategories.Add(standardCategory);
    }

    private static void PortOldMonsterFamiliesToNewDccsPoolSystem(ClassicStageInfo.MonsterFamily[] possibleMonsterFamilies, List<DccsPool.Category> dccsPoolCategories)
    {
        var familyCategory = new DccsPool.Category();
        familyCategory.name = Helpers.MonsterPoolCategories.Family;
        familyCategory.categoryWeight = ClassicStageInfo.monsterFamilyChance;

        familyCategory.alwaysIncluded = Array.Empty<DccsPool.PoolEntry>();

        var familyPoolEntries = new List<DccsPool.ConditionalPoolEntry>();
        foreach (var monsterFamily in possibleMonsterFamilies)
        {
            if (monsterFamily.monsterFamilyCategories is FamilyDirectorCardCategorySelection familyDccs)
            {
                familyPoolEntries.Add(new DccsPool.ConditionalPoolEntry()
                {
                    dccs = familyDccs,
                    weight = monsterFamily.selectionWeight,
                    requiredExpansions = Array.Empty<ExpansionDef>()
                });
            }
            else
            {
                R2API.Logger.LogError($"classicStageInfo.possibleMonsterFamilies {monsterFamily.monsterFamilyCategories.name} not setup correctly");
            }
        }
        familyCategory.includedIfConditionsMet = familyPoolEntries.ToArray();

        familyCategory.includedIfNoConditionsMet = Array.Empty<DccsPool.PoolEntry>();

        dccsPoolCategories.Add(familyCategory);
    }

    private static void PortToNewInteractableSystem(ClassicStageInfo classicStageInfo)
    {
        var isUsingOldSystem = !classicStageInfo.interactableDccsPool && classicStageInfo.interactableCategories;
        if (isUsingOldSystem)
        {
            R2API.Logger.LogInfo($"Current scene is using old interactable dccs system, porting to new one");

            var newDccsPool = ScriptableObject.CreateInstance<DccsPool>();
            newDccsPool.name = "R2API_" + "dp" + classicStageInfo.name + "Interactables";

            var dccsPoolCategories = new List<DccsPool.Category>();

            var standardCategory = new DccsPool.Category();
            standardCategory.name = Helpers.InteractablePoolCategories.Standard;
            standardCategory.categoryWeight = Helpers.InteractablePoolCategories.StandardWeight;

            standardCategory.alwaysIncluded = Array.Empty<DccsPool.PoolEntry>();
            standardCategory.includedIfConditionsMet = Array.Empty<DccsPool.ConditionalPoolEntry>();
            standardCategory.includedIfNoConditionsMet = new DccsPool.PoolEntry[] {
                new DccsPool.PoolEntry() {
                    dccs = classicStageInfo.interactableCategories,
                    weight = 1
                }
            };
            dccsPoolCategories.Add(standardCategory);

            newDccsPool.poolCategories = dccsPoolCategories.ToArray();
            classicStageInfo.interactableDccsPool = newDccsPool;
        }
    }

    private static void ApplyMonsterChanges(ClassicStageInfo classicStageInfo, StageInfo stageInfo)
    {
        InitCustomMixEnemyArtifactDccs();
        var cardHoldersMixEnemyArtifact = GetDirectorCardHoldersFromDCCS(_dccsMixEnemyArtifact);

        MonsterActions?.Invoke(classicStageInfo.monsterDccsPool, cardHoldersMixEnemyArtifact, stageInfo);

        ApplyNewCardHoldersToDCCS(_dccsMixEnemyArtifact, cardHoldersMixEnemyArtifact);
    }

    // Somehow the changes persist across stages sometimes, so... copy the originals,
    // and restore them each time before invoking the events
    // todo: probably need to backup other data too ?
    public class OriginalClassicStageInfo
    {
        public List<DccsPool.Category> monsterDccsPoolCategories;
        public DirectorCardCategorySelection monsterCategories;
        public List<ClassicStageInfo.MonsterFamily> possibleMonsterFamilies;

        public List<DccsPool.Category> interactableDccsPoolCategories;
        public DirectorCardCategorySelection interactableCategories;
    }

    private static readonly Dictionary<string, OriginalClassicStageInfo> _classicStageInfoNameToOriginalClassicStageInfos = new();

    private static void BackupOrRestoreClassicStageInfoToOriginalState(ClassicStageInfo classicStageInfo, StageInfo stageInfo)
    {
        var key = stageInfo.stage == Stage.Custom ? stageInfo.CustomStageName : stageInfo.stage.ToString();
        if (!_classicStageInfoNameToOriginalClassicStageInfos.TryGetValue(key, out var originalClassicStageInfo))
        {
            originalClassicStageInfo = new();
            if (classicStageInfo.monsterDccsPool)
            {
                originalClassicStageInfo.monsterDccsPoolCategories = CopyDccsPoolCategories(classicStageInfo.monsterDccsPool.poolCategories);
            }
            if (classicStageInfo.monsterCategories)
            {
                originalClassicStageInfo.monsterCategories = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
                originalClassicStageInfo.monsterCategories.CopyFrom(classicStageInfo.monsterCategories);
            }
            if (classicStageInfo.possibleMonsterFamilies != null)
            {
                originalClassicStageInfo.possibleMonsterFamilies = classicStageInfo.possibleMonsterFamilies.ToList();
            }

            if (classicStageInfo.interactableDccsPool)
            {
                originalClassicStageInfo.interactableDccsPoolCategories = CopyDccsPoolCategories(classicStageInfo.interactableDccsPool.poolCategories);
            }
            if (classicStageInfo.interactableCategories)
            {
                originalClassicStageInfo.interactableCategories = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
                originalClassicStageInfo.interactableCategories.CopyFrom(classicStageInfo.interactableCategories);
            }

            _classicStageInfoNameToOriginalClassicStageInfos[key] = originalClassicStageInfo;
        }
        else
        {
            if (originalClassicStageInfo.monsterDccsPoolCategories != null)
            {
                classicStageInfo.monsterDccsPool.poolCategories = CopyDccsPoolCategories(originalClassicStageInfo.monsterDccsPoolCategories).ToArray();
            }
            if (originalClassicStageInfo.monsterCategories)
            {
                classicStageInfo.monsterCategories.CopyFrom(originalClassicStageInfo.monsterCategories);
            }
            if (originalClassicStageInfo.possibleMonsterFamilies != null)
            {
                classicStageInfo.possibleMonsterFamilies = originalClassicStageInfo.possibleMonsterFamilies.ToArray();
            }

            if (originalClassicStageInfo.interactableDccsPoolCategories != null)
            {
                classicStageInfo.interactableDccsPool.poolCategories = CopyDccsPoolCategories(originalClassicStageInfo.interactableDccsPoolCategories).ToArray();
            }
            if (originalClassicStageInfo.interactableCategories)
            {
                classicStageInfo.interactableCategories.CopyFrom(originalClassicStageInfo.interactableCategories);
            }
        }
    }

    private static List<DccsPool.Category> CopyDccsPoolCategories(IEnumerable<DccsPool.Category> dccsPoolCategories)
    {
        var backup = new List<DccsPool.Category>();
        foreach (var poolCategory in dccsPoolCategories)
        {

            var poolCategoryBackup = new DccsPool.Category();

            poolCategoryBackup.name = poolCategory.name;

            poolCategoryBackup.categoryWeight = poolCategory.categoryWeight;

            poolCategoryBackup.alwaysIncluded = CopyPoolEntries(poolCategory.alwaysIncluded).ToArray();
            poolCategoryBackup.includedIfConditionsMet = CopyConditionalPoolEntries(poolCategory.includedIfConditionsMet).ToArray();
            poolCategoryBackup.includedIfNoConditionsMet = CopyPoolEntries(poolCategory.includedIfNoConditionsMet).ToArray();

            backup.Add(poolCategoryBackup);
        }

        return backup;
    }

    private static List<DccsPool.ConditionalPoolEntry> CopyConditionalPoolEntries(IEnumerable<DccsPool.ConditionalPoolEntry> poolEntries)
    {
        List<DccsPool.ConditionalPoolEntry> backup = new();

        foreach (var poolEntry in poolEntries)
        {
            var poolEntryBackup = new DccsPool.ConditionalPoolEntry();

            poolEntryBackup.requiredExpansions = ArrayUtils.Clone<ExpansionDef>(poolEntry.requiredExpansions);

            poolEntryBackup.weight = poolEntry.weight;

            poolEntryBackup.dccs = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
            poolEntryBackup.dccs.CopyFrom(poolEntry.dccs);
            poolEntryBackup.dccs.name = poolEntry.dccs.name;

            backup.Add(poolEntryBackup);
        }

        return backup;
    }

    private static List<DccsPool.PoolEntry> CopyPoolEntries(IEnumerable<DccsPool.PoolEntry> poolEntries)
    {
        List<DccsPool.PoolEntry> backup = new();

        foreach (var poolEntry in poolEntries)
        {
            var poolEntryBackup = new DccsPool.PoolEntry();

            poolEntryBackup.weight = poolEntry.weight;

            poolEntryBackup.dccs = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
            poolEntryBackup.dccs.CopyFrom(poolEntry.dccs);
            poolEntryBackup.dccs.name = poolEntry.dccs.name;

            backup.Add(poolEntryBackup);
        }

        return backup;
    }

    private static List<DirectorCardHolder> GetDirectorCardHoldersFromDCCS(DirectorCardCategorySelection dccs)
    {
        var cardHolders = new List<DirectorCardHolder>();

        if (dccs)
        {
            foreach (var dccsCategory in dccs.categories)
            {
                if (dccsCategory.cards.Length > 0)
                {
                    var isInteractable = false;
                    isInteractable = IsInteractableDccsCategory(dccsCategory);

                    foreach (var card in dccsCategory.cards)
                    {
                        var cardHolder = new DirectorCardHolder();
                        cardHolder.Card = card;

                        if (isInteractable)
                        {
                            var interactableCategory = Helpers.GetInteractableCategory(dccsCategory.name);
                            cardHolder.InteractableCategory = interactableCategory;
                            if (interactableCategory == InteractableCategory.Custom)
                            {
                                cardHolder.CustomInteractableCategory = dccsCategory.name;
                            }
                        }
                        else
                        {
                            var monsterCategory = Helpers.GetMonsterCategory(dccsCategory.name);
                            cardHolder.MonsterCategory = monsterCategory;
                            if (monsterCategory == MonsterCategory.Custom)
                            {
                                cardHolder.CustomMonsterCategory = dccsCategory.name;
                            }
                        }

                        cardHolders.Add(cardHolder);
                    }
                }
            }
        }

        return cardHolders;
    }

    private static bool IsInteractableDccsCategory(DirectorCardCategorySelection.Category dccsCategory)
    {
        bool isInteractable = false;
        foreach (var item in dccsCategory.cards)
        {
            if (item.spawnCard)
            {
                if (item.spawnCard.GetType().IsSameOrSubclassOf<InteractableSpawnCard>())
                {
                    isInteractable = true;
                    break;
                }
            }
        }

        return isInteractable;
    }

    private static void ApplyNewCardHoldersToDCCS(DirectorCardCategorySelection dccs, List<DirectorCardHolder> directorCardHolders)
    {
        dccs.Clear();
        foreach (var dch in directorCardHolders)
        {
            dccs.AddCard(dch);
        }
    }

    private static void ApplyInteractableChanges(ClassicStageInfo classicStageInfo, StageInfo stageInfo)
    {
        InteractableActions?.Invoke(classicStageInfo.interactableDccsPool, stageInfo);
    }

    private static StageSettings GetStageSettings(ClassicStageInfo classicStageInfo)
    {
        var stageSettings = new StageSettings
        {
            SceneDirectorInteractableCredits = classicStageInfo.sceneDirectorInteractibleCredits,
            SceneDirectorMonsterCredits = classicStageInfo.sceneDirectorMonsterCredits,
            BonusCreditObjects = new Dictionary<GameObject, int>()
        };

        foreach (var bonusObj in classicStageInfo.bonusInteractibleCreditObjects)
        {
            if (bonusObj.objectThatGrantsPointsIfEnabled)
            {
                stageSettings.BonusCreditObjects[bonusObj.objectThatGrantsPointsIfEnabled] = bonusObj.points;
            }
        }

        GetMonsterCategoryWeightsPerDccs(classicStageInfo, stageSettings);

        GetInteractableCategoryWeightsPerDccs(classicStageInfo, stageSettings);

        return stageSettings;
    }

    private static void InitCustomMixEnemyArtifactDccs()
    {
        _dccsMixEnemyArtifact = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
        _dccsMixEnemyArtifact.name = "dccsR2APIMixEnemyArtifact";
        _dccsMixEnemyArtifact.CopyFrom(RoR2Content.mixEnemyMonsterCards);
    }

    private static void GetMonsterCategoryWeightsPerDccs(ClassicStageInfo classicStageInfo, StageSettings stageSettings)
    {
        stageSettings.MonsterCategoryWeightsPerDccs = new();

        GetMonsterCategoryWeights(stageSettings, _dccsMixEnemyArtifact);

        if (classicStageInfo.monsterDccsPool)
        {
            foreach (var poolCategory in classicStageInfo.monsterDccsPool.poolCategories)
            {
                GetMonsterCategoryWeights(stageSettings, poolCategory.alwaysIncluded);
                GetMonsterCategoryWeights(stageSettings, poolCategory.includedIfConditionsMet);
                GetMonsterCategoryWeights(stageSettings, poolCategory.includedIfNoConditionsMet);
            }
        }
    }

    private static void GetMonsterCategoryWeights(StageSettings stageSettings, DirectorCardCategorySelection dccs)
    {
        stageSettings.MonsterCategoryWeightsPerDccs[dccs] = new();

        foreach (var category in dccs.categories)
        {
            stageSettings.MonsterCategoryWeightsPerDccs[dccs][category.name] = category.selectionWeight;
        }
    }

    private static void GetMonsterCategoryWeights(StageSettings stageSettings, DccsPool.PoolEntry[] poolCategories)
    {
        foreach (var poolEntry in poolCategories)
        {
            GetMonsterCategoryWeights(stageSettings, poolEntry.dccs);
        }
    }

    private static void GetInteractableCategoryWeightsPerDccs(ClassicStageInfo classicStageInfo, StageSettings stageSettings)
    {
        stageSettings.InteractableCategoryWeightsPerDccs = new();

        if (classicStageInfo.interactableDccsPool)
        {
            foreach (var poolCategory in classicStageInfo.interactableDccsPool.poolCategories)
            {
                GetInteractableCategoryWeights(stageSettings, poolCategory.alwaysIncluded);
                GetInteractableCategoryWeights(stageSettings, poolCategory.includedIfConditionsMet);
                GetInteractableCategoryWeights(stageSettings, poolCategory.includedIfNoConditionsMet);
            }
        }
    }

    private static void GetInteractableCategoryWeights(StageSettings stageSettings, DccsPool.PoolEntry[] poolCategories)
    {
        foreach (var poolEntry in poolCategories)
        {
            GetInteractableCategoryWeights(stageSettings, poolEntry.dccs);
        }
    }

    private static void GetInteractableCategoryWeights(StageSettings stageSettings, DirectorCardCategorySelection dccs)
    {
        stageSettings.InteractableCategoryWeightsPerDccs[dccs] = new();
        foreach (var category in dccs.categories)
        {
            stageSettings.InteractableCategoryWeightsPerDccs[dccs][category.name] = category.selectionWeight;
        }
    }

    private static void SetStageSettings(ClassicStageInfo classicStageInfo, StageSettings stageSettings)
    {
        classicStageInfo.sceneDirectorInteractibleCredits = stageSettings.SceneDirectorInteractableCredits;
        classicStageInfo.sceneDirectorMonsterCredits = stageSettings.SceneDirectorMonsterCredits;

        var keys = stageSettings.BonusCreditObjects.Keys.ToArray();
        var bonuses = new ClassicStageInfo.BonusInteractibleCreditObject[keys.Length];

        for (int i = 0; i < keys.Length; i++)
        {
            bonuses[i] = new ClassicStageInfo.BonusInteractibleCreditObject
            {
                objectThatGrantsPointsIfEnabled = keys[i],
                points = stageSettings.BonusCreditObjects[keys[i]]
            };
        }

        classicStageInfo.bonusInteractibleCreditObjects = bonuses;

        SetMonsterCategoryWeightsPerDccs(classicStageInfo, stageSettings);

        SetInteractableCategoryWeightsPerDccs(classicStageInfo, stageSettings);
    }

    private static void SetMonsterCategoryWeightsPerDccs(ClassicStageInfo classicStageInfo, StageSettings stageSettings)
    {
        SetMonsterCategoryWeights(_dccsMixEnemyArtifact, stageSettings.MonsterCategoryWeightsPerDccs[_dccsMixEnemyArtifact]);

        if (classicStageInfo.monsterDccsPool)
        {
            foreach (var poolCategory in classicStageInfo.monsterDccsPool.poolCategories)
            {
                SetMonsterCategoryWeights(stageSettings, poolCategory.alwaysIncluded);
                SetMonsterCategoryWeights(stageSettings, poolCategory.includedIfConditionsMet);
                SetMonsterCategoryWeights(stageSettings, poolCategory.includedIfNoConditionsMet);
            }
        }
    }

    private static void SetMonsterCategoryWeights(DirectorCardCategorySelection dccs, Dictionary<string, float> newMonsterCategoryWeights)
    {
        for (int i = 0; i < dccs.categories.Length; i++)
        {
            var category = dccs.categories[i];
            var monsterCategory = category.name;
            category.selectionWeight = newMonsterCategoryWeights[monsterCategory];
            dccs.categories[i] = category;
        }
    }

    private static void SetMonsterCategoryWeights(StageSettings stageSettings, DccsPool.PoolEntry[] poolCategories)
    {
        foreach (var poolEntry in poolCategories)
        {
            SetMonsterCategoryWeights(poolEntry.dccs, stageSettings.MonsterCategoryWeightsPerDccs[poolEntry.dccs]);
        }
    }

    private static void SetInteractableCategoryWeightsPerDccs(ClassicStageInfo classicStageInfo, StageSettings stageSettings)
    {
        if (classicStageInfo.interactableDccsPool)
        {
            foreach (var poolCategory in classicStageInfo.interactableDccsPool.poolCategories)
            {
                SetInteractableCategoryWeights(stageSettings, poolCategory.alwaysIncluded);
                SetInteractableCategoryWeights(stageSettings, poolCategory.includedIfConditionsMet);
                SetInteractableCategoryWeights(stageSettings, poolCategory.includedIfNoConditionsMet);
            }
        }
    }

    private static void SetInteractableCategoryWeights(StageSettings stageSettings, DccsPool.PoolEntry[] poolCategories)
    {
        foreach (var poolEntry in poolCategories)
        {
            SetInteractableCategoryWeights(poolEntry.dccs, stageSettings.InteractableCategoryWeightsPerDccs[poolEntry.dccs]);
        }
    }

    private static void SetInteractableCategoryWeights(DirectorCardCategorySelection dccs, Dictionary<string, float> newInteractableCategoryWeights)
    {
        for (int i = 0; i < dccs.categories.Length; i++)
        {
            var category = dccs.categories[i];
            category.selectionWeight = newInteractableCategoryWeights[category.name];
            dccs.categories[i] = category;
        }
    }
}
