using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Changing namespace to R2API.Director would be breaking
namespace R2API {

    [R2APISubmodule]
    public static partial class DirectorAPI {

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.ClassicStageInfo.Awake += ApplyChangesOnClassicStageInfoAwake;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.ClassicStageInfo.Awake -= ApplyChangesOnClassicStageInfoAwake;
        }

        private static void ApplyChangesOnClassicStageInfoAwake(On.RoR2.ClassicStageInfo.orig_Awake orig, ClassicStageInfo self) {
            self.ApplyChanges();
            orig(self);
        }

        internal static void ApplyChanges(this ClassicStageInfo self) {
            var stageInfo = GetStageInfo(self);
            ApplySettingsChanges(self, stageInfo);
            ApplyMonsterChanges(self, stageInfo);
            ApplyInteractableChanges(self, stageInfo);
            ApplyFamilyChanges(self, stageInfo);
        }

        private static StageInfo GetStageInfo(ClassicStageInfo stage) {
            StageInfo stageInfo = new StageInfo {
                stage = Stage.Custom,
                CustomStageName = "",
            };

            var info = stage.GetComponent<SceneInfo>();
            if (!info) return stageInfo;

            var scene = info.sceneDef;
            if (!scene) return stageInfo;

            switch (scene.baseSceneName) {
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
                    stageInfo.CustomStageName = scene.baseSceneName;
                    break;
            }

            return stageInfo;
        }

        private static void ApplySettingsChanges(ClassicStageInfo self, StageInfo stageInfo) {
            StageSettings settings = GetStageSettings(self);
            StageSettingsActions?.Invoke(settings, stageInfo);
            SetStageSettings(self, settings);
        }

        private static void ApplyMonsterChanges(ClassicStageInfo self, StageInfo stage) {
            var monsterCategoriesSelection = self.monsterCategories;
            var monsterDirectorCardHolders = new List<DirectorCardHolder>();

            foreach (var cardCategorySelection in monsterCategoriesSelection.categories) {
                var monsterCategory = GetMonsterCategory(cardCategorySelection.name);
                var interactableCategory = GetInteractableCategory(cardCategorySelection.name);
                foreach (var card in cardCategorySelection.cards) {
                    monsterDirectorCardHolders.Add(new DirectorCardHolder {
                        InteractableCategory = interactableCategory,
                        MonsterCategory = monsterCategory,
                        Card = card
                    });
                }
            }

            MonsterActions?.Invoke(monsterDirectorCardHolders, stage);

            var basicMonsterCards = new List<DirectorCard>();
            var miniBossCards = new List<DirectorCard>();
            var championMonsterCards = new List<DirectorCard>();

            foreach (var cardHolder in monsterDirectorCardHolders) {
                switch (cardHolder.MonsterCategory) {
                    case MonsterCategory.BasicMonsters:
                        basicMonsterCards.Add(cardHolder.Card);
                        break;

                    case MonsterCategory.Champions:
                        championMonsterCards.Add(cardHolder.Card);
                        break;

                    case MonsterCategory.Minibosses:
                        miniBossCards.Add(cardHolder.Card);
                        break;
                }
            }

            for (int i = 0; i < monsterCategoriesSelection.categories.Length; i++) {
                DirectorCardCategorySelection.Category cat = monsterCategoriesSelection.categories[i];
                switch (cat.name) {
                    case "Champions":
                        cat.cards = championMonsterCards.ToArray();
                        break;

                    case "Minibosses":
                        cat.cards = miniBossCards.ToArray();
                        break;

                    case "Basic Monsters":
                        cat.cards = basicMonsterCards.ToArray();
                        break;
                }

                monsterCategoriesSelection.categories[i] = cat;
            }
        }

        private static void ApplyInteractableChanges(ClassicStageInfo self, StageInfo stage) {
            var interactables = self.interactableCategories;
            var interactableCards = new List<DirectorCardHolder>();

            foreach (var cat in interactables.categories) {
                MonsterCategory monstCat = GetMonsterCategory(cat.name);
                InteractableCategory interCat = GetInteractableCategory(cat.name);
                foreach (var t in cat.cards) {
                    interactableCards.Add(new DirectorCardHolder {
                        InteractableCategory = interCat,
                        MonsterCategory = monstCat,
                        Card = t
                    });
                }
            }

            InteractableActions?.Invoke(interactableCards, stage);

            var interChests = new List<DirectorCard>();
            var interBarrels = new List<DirectorCard>();
            var interShrines = new List<DirectorCard>();
            var interDrones = new List<DirectorCard>();
            var interMisc = new List<DirectorCard>();
            var interRare = new List<DirectorCard>();
            var interDupe = new List<DirectorCard>();

            foreach (var hold in interactableCards) {
                switch (hold.InteractableCategory) {
                    case InteractableCategory.None:
                        R2API.Logger.LogWarning("InteractableCategory from DirectorCardHolder is None !");
                        break;

                    case InteractableCategory.Chests:
                        interChests.Add(hold.Card);
                        break;

                    case InteractableCategory.Barrels:
                        interBarrels.Add(hold.Card);
                        break;

                    case InteractableCategory.Drones:
                        interDrones.Add(hold.Card);
                        break;

                    case InteractableCategory.Duplicator:
                        interDupe.Add(hold.Card);
                        break;

                    case InteractableCategory.Misc:
                        interMisc.Add(hold.Card);
                        break;

                    case InteractableCategory.Rare:
                        interRare.Add(hold.Card);
                        break;

                    case InteractableCategory.Shrines:
                        interShrines.Add(hold.Card);
                        break;
                }
            }

            for (int i = 0; i < interactables.categories.Length; i++) {
                DirectorCardCategorySelection.Category cat = interactables.categories[i];
                switch (cat.name) {
                    case "Chests":
                        cat.cards = interChests.ToArray();
                        break;

                    case "Barrels":
                        cat.cards = interBarrels.ToArray();
                        break;

                    case "Shrines":
                        cat.cards = interShrines.ToArray();
                        break;

                    case "Drones":
                        cat.cards = interDrones.ToArray();
                        break;

                    case "Misc":
                        cat.cards = interMisc.ToArray();
                        break;

                    case "Rare":
                        cat.cards = interRare.ToArray();
                        break;

                    case "Duplicator":
                        cat.cards = interDupe.ToArray();
                        break;
                }

                interactables.categories[i] = cat;
            }
        }

        private static void ApplyFamilyChanges(ClassicStageInfo self, StageInfo stage) {
            var familyHolds = self.possibleMonsterFamilies.Select(GetMonsterFamilyHolder).ToList();

            FamilyActions?.Invoke(familyHolds, stage);

            self.possibleMonsterFamilies = new ClassicStageInfo.MonsterFamily[familyHolds.Count];

            for (int i = 0; i < familyHolds.Count; i++) {
                self.possibleMonsterFamilies[i] = GetMonsterFamily(familyHolds[i]);
            }
        }

        private static StageSettings GetStageSettings(ClassicStageInfo self) {
            var set = new StageSettings {
                SceneDirectorInteractableCredits = self.sceneDirectorInteractibleCredits,
                SceneDirectorMonsterCredits = self.sceneDirectorMonsterCredits,
                BonusCreditObjects = new Dictionary<GameObject, int>()
            };

            foreach (var bonusObj in self.bonusInteractibleCreditObjects) {
                set.BonusCreditObjects[bonusObj.objectThatGrantsPointsIfEnabled] = bonusObj.points;
            }

            set.InteractableCategoryWeights = new Dictionary<InteractableCategory, float>();
            var interactableCategories = self.interactableCategories;

            foreach (var cat in interactableCategories.categories) {
                set.InteractableCategoryWeights[GetInteractableCategory(cat.name)] = cat.selectionWeight;
            }

            set.MonsterCategoryWeights = new Dictionary<MonsterCategory, float>();
            var monsterCategories = self.monsterCategories;

            foreach (var cat in monsterCategories.categories) {
                set.MonsterCategoryWeights[GetMonsterCategory(cat.name)] = cat.selectionWeight;
            }

            return set;
        }

        private static void SetStageSettings(ClassicStageInfo self, StageSettings set) {
            self.sceneDirectorInteractibleCredits = set.SceneDirectorInteractableCredits;
            self.sceneDirectorMonsterCredits = set.SceneDirectorMonsterCredits;

            var keys = set.BonusCreditObjects.Keys.ToArray();
            var bonuses = new ClassicStageInfo.BonusInteractibleCreditObject[keys.Length];

            for (int i = 0; i < keys.Length; i++) {
                bonuses[i] = new ClassicStageInfo.BonusInteractibleCreditObject {
                    objectThatGrantsPointsIfEnabled = keys[i],
                    points = set.BonusCreditObjects[keys[i]]
                };
            }

            self.bonusInteractibleCreditObjects = bonuses;
            var interactableCategories = self.interactableCategories;

            for (int i = 0; i < interactableCategories.categories.Length; i++) {
                var cat = interactableCategories.categories[i];
                InteractableCategory intCat = GetInteractableCategory(cat.name);
                cat.selectionWeight = set.InteractableCategoryWeights[intCat];
                interactableCategories.categories[i] = cat;
            }

            var monsterCategories = self.monsterCategories;

            for (int i = 0; i < monsterCategories.categories.Length; i++) {
                var cat = monsterCategories.categories[i];
                MonsterCategory monCat = GetMonsterCategory(cat.name);
                cat.selectionWeight = set.MonsterCategoryWeights[monCat];
                monsterCategories.categories[i] = cat;
            }
        }

        private static MonsterCategory GetMonsterCategory(string s) {
            return s switch {
                "Champions" => MonsterCategory.Champions,
                "Minibosses" => MonsterCategory.Minibosses,
                "Basic Monsters" => MonsterCategory.BasicMonsters,
                _ => MonsterCategory.None,
            };
        }

        private static InteractableCategory GetInteractableCategory(string s) {
            return s switch {
                "Chests" => InteractableCategory.Chests,
                "Barrels" => InteractableCategory.Barrels,
                "Shrines" => InteractableCategory.Shrines,
                "Drones" => InteractableCategory.Drones,
                "Misc" => InteractableCategory.Misc,
                "Rare" => InteractableCategory.Rare,
                "Duplicator" => InteractableCategory.Duplicator,
                _ => InteractableCategory.None,
            };
        }

        private static MonsterFamilyHolder GetMonsterFamilyHolder(ClassicStageInfo.MonsterFamily family) {
            var hold = new MonsterFamilyHolder {
                MaxStageCompletion = family.maximumStageCompletion,
                MinStageCompletion = family.minimumStageCompletion,
                FamilySelectionWeight = family.selectionWeight,
                SelectionChatString = family.familySelectionChatString
            };

            var cards = family.monsterFamilyCategories.categories;
            foreach (var cat in cards) {
                switch (cat.name) {
                    case "Basic Monsters":
                        hold.FamilyBasicMonsterWeight = cat.selectionWeight;
                        hold.FamilyBasicMonsters = cat.cards.ToList();
                        break;

                    case "Minibosses":
                        hold.FamilyMinibossWeight = cat.selectionWeight;
                        hold.FamilyMinibosses = cat.cards.ToList();
                        break;

                    case "Champions":
                        hold.FamilyChampionWeight = cat.selectionWeight;
                        hold.FamilyChampions = cat.cards.ToList();
                        break;
                }
            }

            return hold;
        }

        private static ClassicStageInfo.MonsterFamily GetMonsterFamily(MonsterFamilyHolder holder) {
            var cardCategorySelection = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();

            cardCategorySelection.categories = new DirectorCardCategorySelection.Category[3];
            cardCategorySelection.categories[0] = new DirectorCardCategorySelection.Category {
                name = "Champions",
                selectionWeight = holder.FamilyChampionWeight,
                cards = (holder.FamilyChampions != null ? holder.FamilyChampions.ToArray() : Array.Empty<DirectorCard>())
            };

            cardCategorySelection.categories[1] = new DirectorCardCategorySelection.Category {
                name = "Minibosses",
                selectionWeight = holder.FamilyMinibossWeight,
                cards = (holder.FamilyMinibosses != null ? holder.FamilyMinibosses.ToArray() : Array.Empty<DirectorCard>())
            };

            cardCategorySelection.categories[2] = new DirectorCardCategorySelection.Category {
                name = "Basic Monsters",
                selectionWeight = holder.FamilyBasicMonsterWeight,
                cards = holder.FamilyBasicMonsters != null ? holder.FamilyBasicMonsters.ToArray() : Array.Empty<DirectorCard>()
            };

            return new ClassicStageInfo.MonsterFamily {
                familySelectionChatString = holder.SelectionChatString,
                maximumStageCompletion = holder.MaxStageCompletion,
                minimumStageCompletion = holder.MinStageCompletion,
                selectionWeight = holder.FamilySelectionWeight,
                monsterFamilyCategories = cardCategorySelection
            };
        }
    }
}
