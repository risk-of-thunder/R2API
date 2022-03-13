using HG;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Changing namespace to R2API.Director would be breaking
namespace R2API {

    [R2APISubmodule]
    public static partial class DirectorAPI {
        private static DirectorCardCategorySelection _dccsMixEnemyArtifact;

        private static void ThrowIfNotLoaded() {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(DirectorAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(DirectorAPI)})]");
            }
        }

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.ClassicStageInfo.Start += ApplyChangesOnStart;
            IL.RoR2.ClassicStageInfo.HandleMixEnemyArtifact += SwapVanillaDccsWithOurs;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.ClassicStageInfo.Start -= ApplyChangesOnStart;
            IL.RoR2.ClassicStageInfo.HandleMixEnemyArtifact -= SwapVanillaDccsWithOurs;
        }

        private static void ApplyChangesOnStart(On.RoR2.ClassicStageInfo.orig_Start orig, ClassicStageInfo classicStageInfo) {
            classicStageInfo.ApplyChanges();
            orig(classicStageInfo);
        }

        private static void SwapVanillaDccsWithOurs(ILContext il) {
            var cursor = new ILCursor(il);

            if (cursor.TryGotoNext(
                i => i.MatchCallOrCallvirt<DirectorCardCategorySelection>(nameof(DirectorCardCategorySelection.CopyFrom))
                )) {
                cursor.EmitDelegate(SwapDccs);
            }

            static DirectorCardCategorySelection SwapDccs(DirectorCardCategorySelection vanillaDccs) {
                return _dccsMixEnemyArtifact;
            }
        }

        internal static void ApplyChanges(this ClassicStageInfo classicStageInfo) {
            var stageInfo = GetStageInfo(classicStageInfo);

            ApplyMonsterChanges(classicStageInfo, stageInfo);
            ApplyInteractableChanges(classicStageInfo, stageInfo);
            ApplySettingsChanges(classicStageInfo, stageInfo);
        }

        private static StageInfo GetStageInfo(ClassicStageInfo classicStageInfo) {
            var stageInfo = new StageInfo {
                stage = Stage.Custom,
                CustomStageName = "",
            };

            var sceneInfo = classicStageInfo.GetComponent<SceneInfo>();
            if (!sceneInfo) return stageInfo;

            var sceneDef = sceneInfo.sceneDef;
            if (!sceneDef) return stageInfo;
            stageInfo = SetStageEnumFromBaseSceneName(stageInfo, sceneDef);

            return stageInfo;
        }

        private static StageInfo SetStageEnumFromBaseSceneName(StageInfo stageInfo, SceneDef sceneDef) {
            switch (sceneDef.baseSceneName) {
                case "golemplains":
                    stageInfo.stage = Stage.TitanicPlains;
                    break;

                case "blackbeach":
                    stageInfo.stage = Stage.DistantRoost;
                    break;

                case "foggyswamp":
                    stageInfo.stage = Stage.WetlandAspect;
                    break;

                case "goolake":
                    stageInfo.stage = Stage.AbandonedAqueduct;
                    break;

                case "frozenwall":
                    stageInfo.stage = Stage.RallypointDelta;
                    break;

                case "wispgraveyard":
                    stageInfo.stage = Stage.ScorchedAcres;
                    break;

                case "dampcavesimple":
                    stageInfo.stage = Stage.AbyssalDepths;
                    break;

                case "shipgraveyard":
                    stageInfo.stage = Stage.SirensCall;
                    break;

                case "goldshores":
                    stageInfo.stage = Stage.GildedCoast;
                    break;

                case "mysteryspace":
                    stageInfo.stage = Stage.MomentFractured;
                    break;

                case "bazaar":
                    stageInfo.stage = Stage.Bazaar;
                    break;

                case "arena":
                    stageInfo.stage = Stage.VoidCell;
                    break;

                case "limbo":
                    stageInfo.stage = Stage.MomentWhole;
                    break;

                case "skymeadow":
                    stageInfo.stage = Stage.SkyMeadow;
                    break;

                case "artifactworld":
                    stageInfo.stage = Stage.ArtifactReliquary;
                    break;

                case "moon2":
                    stageInfo.stage = Stage.Commencement;
                    break;

                case "rootjungle":
                    stageInfo.stage = Stage.SunderedGrove;
                    break;

                case "ancientloft":
                    stageInfo.stage = Stage.AphelianSanctuary;
                    break;

                case "itancientloft":
                    stageInfo.stage = Stage.AphelianSanctuarySimulacrum;
                    break;

                case "itdampcave":
                    stageInfo.stage = Stage.AbyssalDepthsSimulacrum;
                    break;

                case "itfrozenwall":
                    stageInfo.stage = Stage.RallypointDeltaSimulacrum;
                    break;

                case "itgolemplains":
                    stageInfo.stage = Stage.TitanicPlainsSimulacrum;
                    break;

                case "itgoolake":
                    stageInfo.stage = Stage.AbandonedAqueductSimulacrum;
                    break;

                case "itmoon":
                    stageInfo.stage = Stage.CommencementSimulacrum;
                    break;

                case "itskymeadow":
                    stageInfo.stage = Stage.SkyMeadowSimulacrum;
                    break;

                case "snowyforest":
                    stageInfo.stage = Stage.SiphonedForest;
                    break;

                case "sulfurpools":
                    stageInfo.stage = Stage.SulfurPools;
                    break;

                case "voidraid":
                    stageInfo.stage = Stage.VoidLocus;
                    break;

                case "voidstage":
                    stageInfo.stage = Stage.ThePlanetarium;
                    break;

                default:
                    stageInfo.stage = Stage.Custom;
                    stageInfo.CustomStageName = sceneDef.baseSceneName;
                    break;
            }

            return stageInfo;
        }

        private static void ApplySettingsChanges(ClassicStageInfo classicStageInfo, StageInfo stageInfo) {
            var stageSettings = GetStageSettings(classicStageInfo);
            StageSettingsActions?.Invoke(stageSettings, stageInfo);
            SetStageSettings(classicStageInfo, stageSettings);
        }

        public class OriginalClassicStageInfo {
            public List<DccsPool.Category> monsterDccsPoolCategories;
            public DirectorCardCategorySelection monsterCategories;

            public List<DccsPool.Category> interactableDccsPoolCategories;
            public DirectorCardCategorySelection interactableCategories;
        }

        private static readonly Dictionary<string, OriginalClassicStageInfo> _originalClassicStageInfos = new();
        private static void ApplyMonsterChanges(ClassicStageInfo classicStageInfo, StageInfo stageInfo) {
            RestoreClassicStageInfoToOriginalState(classicStageInfo, stageInfo);

            List<DirectorCardHolder> oldDccs = null;
            if (!classicStageInfo.monsterCategories) {
                oldDccs = GetDirectorCardHoldersFromDCCS(classicStageInfo.monsterCategories);
            }

            InitCustomMixEnemyArtifactDccs();
            var cardHoldersMixEnemyArtifact = GetDirectorCardHoldersFromDCCS(_dccsMixEnemyArtifact);

            MonsterActions?.Invoke(classicStageInfo.monsterDccsPool, oldDccs, cardHoldersMixEnemyArtifact, stageInfo);

            if (oldDccs != null) {
                ApplyNewCardHoldersToDCCS(classicStageInfo.monsterCategories, oldDccs);
            }

            ApplyNewCardHoldersToDCCS(_dccsMixEnemyArtifact, cardHoldersMixEnemyArtifact);
        }

        // Somehow the changes persist across stages sometimes, so... copy the originals,
        // and restore them each time before invoking the events
        // todo: probably need to backup other data too ?
        private static void RestoreClassicStageInfoToOriginalState(ClassicStageInfo classicStageInfo, StageInfo stageInfo) {
            var key = stageInfo.stage == Stage.Custom ? stageInfo.CustomStageName : stageInfo.stage.ToString();
            if (!_originalClassicStageInfos.TryGetValue(key, out var originalClassicStageInfo)) {
                originalClassicStageInfo = new();
                if (classicStageInfo.monsterDccsPool) {
                    originalClassicStageInfo.monsterDccsPoolCategories = CopyDccsPoolCategories(classicStageInfo.monsterDccsPool.poolCategories);
                }
                if (classicStageInfo.monsterCategories) {
                    originalClassicStageInfo.monsterCategories = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
                    originalClassicStageInfo.monsterCategories.CopyFrom(classicStageInfo.monsterCategories);
                }

                if (classicStageInfo.interactableDccsPool) {
                    originalClassicStageInfo.interactableDccsPoolCategories = CopyDccsPoolCategories(classicStageInfo.interactableDccsPool.poolCategories);
                }
                if (classicStageInfo.interactableCategories) {
                    originalClassicStageInfo.interactableCategories = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
                    originalClassicStageInfo.interactableCategories.CopyFrom(classicStageInfo.interactableCategories);
                }

                _originalClassicStageInfos[key] = originalClassicStageInfo;
            }
            else {
                classicStageInfo.monsterDccsPool.poolCategories = CopyDccsPoolCategories(originalClassicStageInfo.monsterDccsPoolCategories).ToArray();
                classicStageInfo.monsterCategories.CopyFrom(originalClassicStageInfo.monsterCategories);

                classicStageInfo.interactableDccsPool.poolCategories = CopyDccsPoolCategories(originalClassicStageInfo.interactableDccsPoolCategories).ToArray();
                classicStageInfo.interactableCategories.CopyFrom(originalClassicStageInfo.interactableCategories);
            }
        }

        private static List<DccsPool.Category> CopyDccsPoolCategories(IEnumerable<DccsPool.Category> dccsPoolCategories) {
            var backup = new List<DccsPool.Category>();
            foreach (var poolCategory in dccsPoolCategories) {

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

        private static List<DccsPool.ConditionalPoolEntry> CopyConditionalPoolEntries(IEnumerable<DccsPool.ConditionalPoolEntry> poolEntries) {
            List<DccsPool.ConditionalPoolEntry> backup = new();

            foreach (var poolEntry in poolEntries) {
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

        private static List<DccsPool.PoolEntry> CopyPoolEntries(IEnumerable<DccsPool.PoolEntry> poolEntries) {
            List<DccsPool.PoolEntry> backup = new();

            foreach (var poolEntry in poolEntries) {
                var poolEntryBackup = new DccsPool.PoolEntry();

                poolEntryBackup.weight = poolEntry.weight;

                poolEntryBackup.dccs = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
                poolEntryBackup.dccs.CopyFrom(poolEntry.dccs);
                poolEntryBackup.dccs.name = poolEntry.dccs.name;

                backup.Add(poolEntryBackup);
            }

            return backup;
        }

        private static List<DirectorCardHolder> GetDirectorCardHoldersFromDCCS(DirectorCardCategorySelection dccs) {
            var cardHolders = new List<DirectorCardHolder>();

            if (dccs) {
                foreach (var dccsCategory in dccs.categories) {
                    if (dccsCategory.cards.Length > 0) {
                        var isInteractable = false;
                        isInteractable = IsInteractableDccsCategory(dccsCategory);

                        foreach (var card in dccsCategory.cards) {
                            var cardHolder = new DirectorCardHolder();
                            cardHolder.Card = card;

                            if (isInteractable) {
                                var interactableCategory = Helpers.GetInteractableCategory(dccsCategory.name);
                                cardHolder.InteractableCategory = interactableCategory;
                                if (interactableCategory == InteractableCategory.Custom) {
                                    cardHolder.CustomInteractableCategory = dccsCategory.name;
                                }
                            }
                            else {
                                var monsterCategory = Helpers.GetMonsterCategory(dccsCategory.name);
                                cardHolder.MonsterCategory = monsterCategory;
                                if (monsterCategory == MonsterCategory.Custom) {
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

        private static bool IsInteractableDccsCategory(DirectorCardCategorySelection.Category dccsCategory) {
            bool isInteractable = false;
            foreach (var item in dccsCategory.cards) {
                if (item.spawnCard) {
                    if (item.spawnCard.GetType().IsSameOrSubclassOf<InteractableSpawnCard>()) {
                        isInteractable = true;
                        break;
                    }
                }
            }

            return isInteractable;
        }

        private static void ApplyNewCardHoldersToDCCS(DirectorCardCategorySelection dccs, List<DirectorCardHolder> directorCardHolders) {
            dccs.Clear();
            foreach (var dch in directorCardHolders) {
                dccs.AddCard(dch);
            }
        }

        private static void ApplyInteractableChanges(ClassicStageInfo classicStageInfo, StageInfo stageInfo) {
            List<DirectorCardHolder> oldDccs = null;
            if (!classicStageInfo.interactableDccsPool) {
                oldDccs = GetDirectorCardHoldersFromDCCS(classicStageInfo.interactableCategories);
            }

            InteractableActions?.Invoke(classicStageInfo.interactableDccsPool, oldDccs, stageInfo);

            if (oldDccs != null) {
                ApplyNewCardHoldersToDCCS(classicStageInfo.interactableCategories, oldDccs);
            }
        }

        private static StageSettings GetStageSettings(ClassicStageInfo classicStageInfo) {
            var stageSettings = new StageSettings {
                SceneDirectorInteractableCredits = classicStageInfo.sceneDirectorInteractibleCredits,
                SceneDirectorMonsterCredits = classicStageInfo.sceneDirectorMonsterCredits,
                BonusCreditObjects = new Dictionary<GameObject, int>()
            };

            foreach (var bonusObj in classicStageInfo.bonusInteractibleCreditObjects) {
                stageSettings.BonusCreditObjects[bonusObj.objectThatGrantsPointsIfEnabled] = bonusObj.points;
            }

            GetMonsterCategoryWeightsPerDccs(classicStageInfo, stageSettings);

            GetInteractableCategoryWeightsPerDccs(classicStageInfo, stageSettings);

            return stageSettings;
        }

        private static void InitCustomMixEnemyArtifactDccs() {
            _dccsMixEnemyArtifact = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
            _dccsMixEnemyArtifact.name = "dccsR2APIMixEnemyArtifact";
            _dccsMixEnemyArtifact.CopyFrom(RoR2Content.mixEnemyMonsterCards);
        }

        private static void GetMonsterCategoryWeightsPerDccs(ClassicStageInfo classicStageInfo, StageSettings stageSettings) {
            stageSettings.MonsterCategoryWeightsPerDccs = new();

            GetMonsterCategoryWeights(stageSettings, _dccsMixEnemyArtifact);

            if (classicStageInfo.monsterDccsPool) {
                foreach (var poolCategory in classicStageInfo.monsterDccsPool.poolCategories) {
                    GetMonsterCategoryWeights(stageSettings, poolCategory.alwaysIncluded);
                    GetMonsterCategoryWeights(stageSettings, poolCategory.includedIfConditionsMet);
                    GetMonsterCategoryWeights(stageSettings, poolCategory.includedIfNoConditionsMet);
                }
            }
            else {
                var oldDccs = classicStageInfo.monsterCategories;
                GetMonsterCategoryWeights(stageSettings, oldDccs);
            }
        }

        private static void GetMonsterCategoryWeights(StageSettings stageSettings, DirectorCardCategorySelection dccs) {
            stageSettings.MonsterCategoryWeightsPerDccs[dccs] = new();

            foreach (var category in dccs.categories) {
                stageSettings.MonsterCategoryWeightsPerDccs[dccs][category.name] = category.selectionWeight;
            }
        }

        private static void GetMonsterCategoryWeights(StageSettings stageSettings, DccsPool.PoolEntry[] poolCategories) {
            foreach (var poolEntry in poolCategories) {
                GetMonsterCategoryWeights(stageSettings, poolEntry.dccs);
            }
        }

        private static void GetInteractableCategoryWeightsPerDccs(ClassicStageInfo classicStageInfo, StageSettings stageSettings) {
            stageSettings.InteractableCategoryWeightsPerDccs = new();

            if (classicStageInfo.interactableDccsPool) {
                foreach (var poolCategory in classicStageInfo.interactableDccsPool.poolCategories) {
                    GetInteractableCategoryWeights(stageSettings, poolCategory.alwaysIncluded);
                    GetInteractableCategoryWeights(stageSettings, poolCategory.includedIfConditionsMet);
                    GetInteractableCategoryWeights(stageSettings, poolCategory.includedIfNoConditionsMet);
                }
            }
            else {
                var oldDccs = classicStageInfo.interactableCategories;
                GetInteractableCategoryWeights(stageSettings, oldDccs);
            }
        }

        private static void GetInteractableCategoryWeights(StageSettings stageSettings, DccsPool.PoolEntry[] poolCategories) {
            foreach (var poolEntry in poolCategories) {
                GetInteractableCategoryWeights(stageSettings, poolEntry.dccs);
            }
        }

        private static void GetInteractableCategoryWeights(StageSettings stageSettings, DirectorCardCategorySelection dccs) {
            stageSettings.InteractableCategoryWeightsPerDccs[dccs] = new();
            foreach (var category in dccs.categories) {
                stageSettings.InteractableCategoryWeightsPerDccs[dccs][category.name] = category.selectionWeight;
            }
        }

        private static void SetStageSettings(ClassicStageInfo classicStageInfo, StageSettings stageSettings) {
            classicStageInfo.sceneDirectorInteractibleCredits = stageSettings.SceneDirectorInteractableCredits;
            classicStageInfo.sceneDirectorMonsterCredits = stageSettings.SceneDirectorMonsterCredits;

            var keys = stageSettings.BonusCreditObjects.Keys.ToArray();
            var bonuses = new ClassicStageInfo.BonusInteractibleCreditObject[keys.Length];

            for (int i = 0; i < keys.Length; i++) {
                bonuses[i] = new ClassicStageInfo.BonusInteractibleCreditObject {
                    objectThatGrantsPointsIfEnabled = keys[i],
                    points = stageSettings.BonusCreditObjects[keys[i]]
                };
            }

            classicStageInfo.bonusInteractibleCreditObjects = bonuses;

            SetMonsterCategoryWeightsPerDccs(classicStageInfo, stageSettings);

            SetInteractableCategoryWeightsPerDccs(classicStageInfo, stageSettings);
        }

        private static void SetMonsterCategoryWeightsPerDccs(ClassicStageInfo classicStageInfo, StageSettings stageSettings) {
            SetMonsterCategoryWeights(_dccsMixEnemyArtifact, stageSettings.MonsterCategoryWeightsPerDccs[_dccsMixEnemyArtifact]);

            if (classicStageInfo.monsterDccsPool) {
                foreach (var poolCategory in classicStageInfo.monsterDccsPool.poolCategories) {
                    SetMonsterCategoryWeights(stageSettings, poolCategory.alwaysIncluded);
                    SetMonsterCategoryWeights(stageSettings, poolCategory.includedIfConditionsMet);
                    SetMonsterCategoryWeights(stageSettings, poolCategory.includedIfNoConditionsMet);
                }
            }
            else {
                var oldDccs = classicStageInfo.monsterCategories;
                SetMonsterCategoryWeights(oldDccs, stageSettings.MonsterCategoryWeightsPerDccs[oldDccs]);
            }
        }

        private static void SetMonsterCategoryWeights(DirectorCardCategorySelection dccs, Dictionary<string, float> newMonsterCategoryWeights) {
            for (int i = 0; i < dccs.categories.Length; i++) {
                var category = dccs.categories[i];
                var monsterCategory = category.name;
                category.selectionWeight = newMonsterCategoryWeights[monsterCategory];
                dccs.categories[i] = category;
            }
        }

        private static void SetMonsterCategoryWeights(StageSettings stageSettings, DccsPool.PoolEntry[] poolCategories) {
            foreach (var poolEntry in poolCategories) {
                SetMonsterCategoryWeights(poolEntry.dccs, stageSettings.MonsterCategoryWeightsPerDccs[poolEntry.dccs]);
            }
        }

        private static void SetInteractableCategoryWeightsPerDccs(ClassicStageInfo classicStageInfo, StageSettings stageSettings) {
            if (classicStageInfo.interactableDccsPool) {
                foreach (var poolCategory in classicStageInfo.interactableDccsPool.poolCategories) {
                    SetInteractableCategoryWeights(stageSettings, poolCategory.alwaysIncluded);
                    SetInteractableCategoryWeights(stageSettings, poolCategory.includedIfConditionsMet);
                    SetInteractableCategoryWeights(stageSettings, poolCategory.includedIfNoConditionsMet);
                }
            }
            else {
                var oldDccs = classicStageInfo.interactableCategories;
                SetInteractableCategoryWeights(oldDccs, stageSettings.InteractableCategoryWeightsPerDccs[oldDccs]);
            }
        }

        private static void SetInteractableCategoryWeights(StageSettings stageSettings, DccsPool.PoolEntry[] poolCategories) {
            foreach (var poolEntry in poolCategories) {
                SetInteractableCategoryWeights(poolEntry.dccs, stageSettings.InteractableCategoryWeightsPerDccs[poolEntry.dccs]);
            }
        }

        private static void SetInteractableCategoryWeights(DirectorCardCategorySelection dccs, Dictionary<string, float> newInteractableCategoryWeights) {
            for (int i = 0; i < dccs.categories.Length; i++) {
                var category = dccs.categories[i];
                category.selectionWeight = newInteractableCategoryWeights[category.name];
                dccs.categories[i] = category;
            }
        }
    }
}
