using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static partial class DirectorAPI {
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.ClassicStageInfo.Awake += ClassicStageInfo_Awake;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.ClassicStageInfo.Awake -= ClassicStageInfo_Awake;
        }

        private static void ClassicStageInfo_Awake( On.RoR2.ClassicStageInfo.orig_Awake orig, ClassicStageInfo self ) {
            var stageInfo = GetStageInfo( self );
            ApplySettingsChanges( self, stageInfo );
            ApplyMonsterChanges( self, stageInfo );
            ApplyInteractableChanges( self, stageInfo );
            ApplyFamilyChanges( self, stageInfo );
            orig( self );
        }

        private static StageInfo GetStageInfo( ClassicStageInfo stage ) {
            StageInfo stageInfo = new StageInfo
            {
                stage = Stage.Custom,
                customStageName = "",
            };
            SceneInfo info = stage.GetComponent<SceneInfo>();
            if( !info ) return stageInfo;
            SceneDef scene = info.sceneDef;
            if( !scene ) return stageInfo;
            switch( scene.sceneName ) {
                case "golemplains":
                    stageInfo.stage = Stage.TitanicPlains;
                    break;
                case "blackbeach":
                    stageInfo.stage = Stage.DistantRoost;
                    break;
                case "goolake":
                    stageInfo.stage = Stage.AbandonedAqueduct;
                    break;
                case "foggyswamp":
                    stageInfo.stage = Stage.WetlandAspect;
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
                default:
                    stageInfo.stage = Stage.Custom;
                    stageInfo.customStageName = scene.sceneName;
                    break;
            }
            return stageInfo;
        }

        private static void ApplySettingsChanges( ClassicStageInfo self, StageInfo stageInfo ) {
            StageSettings settings = GetStageSettings( self );
            stageSettingsActions?.Invoke( settings, stageInfo );
            SetStageSettings( self, settings );
        }

        private static void ApplyMonsterChanges( ClassicStageInfo self, StageInfo stage ) {
            var monsters = self.GetFieldValue<DirectorCardCategorySelection>("monsterCategories");
            List<DirectorCardHolder> monsterCards = new List<DirectorCardHolder>();
            for( Int32 i = 0; i < monsters.categories.Length; i++ ) {
                DirectorCardCategorySelection.Category cat = monsters.categories[i];
                MonsterCategory monstCat = GetMonsterCategory( cat.name );
                InteractableCategory interCat = GetInteractableCategory( cat.name);
                for( Int32 j = 0; j < cat.cards.Length; j++ ) {
                    monsterCards.Add( new DirectorCardHolder {
                        interactableCategory = interCat,
                        monsterCategory = monstCat,
                        card = cat.cards[j]
                    } );
                }
            }
            monsterActions?.Invoke( monsterCards, stage );
            List<DirectorCard> monsterBasic = new List<DirectorCard>();
            List<DirectorCard> monsterSub = new List<DirectorCard>();
            List<DirectorCard> monsterChamp = new List<DirectorCard>();
            for( Int32 i = 0; i < monsterCards.Count; i++ ) {
                DirectorCardHolder hold = monsterCards[i];
                switch( hold.monsterCategory ) {
                    default:
                        break;
                    case MonsterCategory.BasicMonsters:
                        monsterBasic.Add( hold.card );
                        break;
                    case MonsterCategory.Champions:
                        monsterChamp.Add( hold.card );
                        break;
                    case MonsterCategory.Minibosses:
                        monsterSub.Add( hold.card );
                        break;
                }
            }
            for( Int32 i = 0; i < monsters.categories.Length; i++ ) {
                DirectorCardCategorySelection.Category cat = monsters.categories[i];
                switch( cat.name ) {
                    default:
                        break;
                    case "Champions":
                        cat.cards = monsterChamp.ToArray();
                        break;
                    case "Minibosses":
                        cat.cards = monsterSub.ToArray();
                        break;
                    case "Basic Monsters":
                        cat.cards = monsterBasic.ToArray();
                        break;
                }
                monsters.categories[i] = cat;
            }
        }

        private static void ApplyInteractableChanges( ClassicStageInfo self, StageInfo stage ) {
            var interactables = self.GetFieldValue<DirectorCardCategorySelection>("interactableCategories");
            List<DirectorCardHolder> interactableCards = new List<DirectorCardHolder>();
            for( Int32 i = 0; i < interactables.categories.Length; i++ ) {
                DirectorCardCategorySelection.Category cat = interactables.categories[i];
                MonsterCategory monstCat = GetMonsterCategory( cat.name );
                InteractableCategory interCat = GetInteractableCategory( cat.name );
                for( Int32 j = 0; j < cat.cards.Length; j++ ) {
                    interactableCards.Add( new DirectorCardHolder {
                        interactableCategory = interCat,
                        monsterCategory = monstCat,
                        card = cat.cards[j]
                    } );
                }
            }
            interactableActions?.Invoke( interactableCards, stage );
            List<DirectorCard> interChests = new List<DirectorCard>();
            List<DirectorCard> interBarrels = new List<DirectorCard>();
            List<DirectorCard> interShrines = new List<DirectorCard>();
            List<DirectorCard> interDrones = new List<DirectorCard>();
            List<DirectorCard> interMisc = new List<DirectorCard>();
            List<DirectorCard> interRare = new List<DirectorCard>();
            List<DirectorCard> interDupe = new List<DirectorCard>();
            for( Int32 i = 0; i < interactableCards.Count; i++ ) {
                DirectorCardHolder hold = interactableCards[i];
                switch( hold.interactableCategory ) {
                    default:
                        Debug.Log( "Wtf are you doing..." );
                        break;
                    case InteractableCategory.Chests:
                        interChests.Add( hold.card );
                        break;
                    case InteractableCategory.Barrels:
                        interBarrels.Add( hold.card );
                        break;
                    case InteractableCategory.Drones:
                        interDrones.Add( hold.card );
                        break;
                    case InteractableCategory.Duplicator:
                        interDupe.Add( hold.card );
                        break;
                    case InteractableCategory.Misc:
                        interMisc.Add( hold.card );
                        break;
                    case InteractableCategory.Rare:
                        interRare.Add( hold.card );
                        break;
                    case InteractableCategory.Shrines:
                        interShrines.Add( hold.card );
                        break;
                }
            }
            for( Int32 i = 0; i < interactables.categories.Length; i++ ) {
                DirectorCardCategorySelection.Category cat = interactables.categories[i];
                switch( cat.name ) {
                    default:
                        break;
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

        private static void ApplyFamilyChanges( ClassicStageInfo self, StageInfo stage ) {
            List<MonsterFamilyHolder> familyHolds = new List<MonsterFamilyHolder>();
            for( Int32 i = 0; i < self.possibleMonsterFamilies.Length; i++ ) {
                familyHolds.Add( GetMonsterFamilyHolder( self.possibleMonsterFamilies[i] ) );
            }
            familyActions?.Invoke( familyHolds, stage );
            self.possibleMonsterFamilies = new ClassicStageInfo.MonsterFamily[familyHolds.Count];
            for( Int32 i = 0; i < familyHolds.Count; i++ ) {
                Debug.Log( i );
                self.possibleMonsterFamilies[i] = GetMonsterFamily( familyHolds[i] );
            }
        }

        private static StageSettings GetStageSettings( ClassicStageInfo self ) {
            StageSettings set = new StageSettings
            {
                sceneDirectorInteractableCredits = self.sceneDirectorInteractibleCredits,
                sceneDirectorMonsterCredits = self.sceneDirectorMonsterCredits
            };
            set.bonusCreditObjects = new Dictionary<GameObject, Int32>();
            for( Int32 i = 0; i < self.bonusInteractibleCreditObjects.Length; i++ ) {
                var bonusObj = self.bonusInteractibleCreditObjects[i];
                set.bonusCreditObjects[bonusObj.objectThatGrantsPointsIfEnabled] = bonusObj.points;
            }
            set.interactableCategoryWeights = new Dictionary<InteractableCategory, Single>();
            var interCats = self.GetFieldValue<DirectorCardCategorySelection>("interactableCategories");
            for( Int32 i = 0; i < interCats.categories.Length; i++ ) {
                var cat = interCats.categories[i];
                set.interactableCategoryWeights[GetInteractableCategory( cat.name )] = cat.selectionWeight;
            }
            set.monsterCategoryWeights = new Dictionary<MonsterCategory, Single>();
            var monstCats = self.GetFieldValue<DirectorCardCategorySelection>("monsterCategories");
            for( Int32 i = 0; i < monstCats.categories.Length; i++ ) {
                var cat = monstCats.categories[i];
                set.monsterCategoryWeights[GetMonsterCategory( cat.name )] = cat.selectionWeight;
            }
            return set;
        }

        private static void SetStageSettings( ClassicStageInfo self, StageSettings set ) {
            self.sceneDirectorInteractibleCredits = set.sceneDirectorInteractableCredits;
            self.sceneDirectorMonsterCredits = set.sceneDirectorMonsterCredits;
            var keys = set.bonusCreditObjects.Keys.ToArray();
            var bonuses = new ClassicStageInfo.BonusInteractibleCreditObject[keys.Length];
            for( Int32 i = 0; i < keys.Length; i++ ) {
                bonuses[i] = new ClassicStageInfo.BonusInteractibleCreditObject {
                    objectThatGrantsPointsIfEnabled = keys[i],
                    points = set.bonusCreditObjects[keys[i]]
                };
            }
            self.bonusInteractibleCreditObjects = bonuses;
            var interCats = self.GetFieldValue<DirectorCardCategorySelection>("interactableCategories");
            for( Int32 i = 0; i < interCats.categories.Length; i++ ) {
                var cat = interCats.categories[i];
                InteractableCategory intCat = GetInteractableCategory( cat.name );
                cat.selectionWeight = set.interactableCategoryWeights[intCat];
                interCats.categories[i] = cat;
            }
            var monstCats = self.GetFieldValue<DirectorCardCategorySelection>("monsterCategories");
            for( Int32 i = 0; i < monstCats.categories.Length; i++ ) {
                var cat = monstCats.categories[i];
                MonsterCategory monCat = GetMonsterCategory( cat.name );
                cat.selectionWeight = set.monsterCategoryWeights[monCat];
                monstCats.categories[i] = cat;
            }
        }

        private static MonsterCategory GetMonsterCategory( String s ) {
            switch( s ) {
                default:
                    return MonsterCategory.None;
                case "Champions":
                    return MonsterCategory.Champions;
                case "Minibosses":
                    return MonsterCategory.Minibosses;
                case "Basic Monsters":
                    return MonsterCategory.BasicMonsters;
            }
        }

        private static InteractableCategory GetInteractableCategory( String s ) {
            switch( s ) {
                default:
                    return InteractableCategory.None;
                case "Chests":
                    return InteractableCategory.Chests;
                case "Barrels":
                    return InteractableCategory.Barrels;
                case "Shrines":
                    return InteractableCategory.Shrines;
                case "Drones":
                    return InteractableCategory.Drones;
                case "Misc":
                    return InteractableCategory.Misc;
                case "Rare":
                    return InteractableCategory.Rare;
                case "Duplicator":
                    return InteractableCategory.Duplicator;
            }
        }

        private static MonsterFamilyHolder GetMonsterFamilyHolder( ClassicStageInfo.MonsterFamily family ) {
            MonsterFamilyHolder hold = new MonsterFamilyHolder
            {
                maxStageCompletion = family.maximumStageCompletion,
                minStageCompletion = family.minimumStageCompletion,
                familySelectionWeight = family.selectionWeight,
                selectionChatString = family.familySelectionChatString
            };
            var cards = family.monsterFamilyCategories.categories;
            for( Int32 i = 0; i < cards.Length; i++ ) {
                var cat = cards[i];

                switch( cat.name ) {
                    case "Basic Monsters":
                        hold.familyBasicMonsterWeight = cat.selectionWeight;
                        hold.familyBasicMonsters = cat.cards.ToList();
                        break;
                    case "Minibosses":
                        hold.familyMinibossWeight = cat.selectionWeight;
                        hold.familyMinibosses = cat.cards.ToList();
                        break;
                    case "Champions":
                        hold.familyChampionWeight = cat.selectionWeight;
                        hold.familyChampions = cat.cards.ToList();
                        break;
                }
            }
            return hold;
        }

        private static ClassicStageInfo.MonsterFamily GetMonsterFamily( MonsterFamilyHolder holder ) {
            DirectorCardCategorySelection catSel = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
            catSel.categories = new DirectorCardCategorySelection.Category[3];
            catSel.categories[0] = new DirectorCardCategorySelection.Category {
                name = "Champions",
                selectionWeight = holder.familyChampionWeight,
                cards = (holder.familyChampions != null ? holder.familyChampions.ToArray() : Array.Empty<DirectorCard>())
            };
            catSel.categories[1] = new DirectorCardCategorySelection.Category {
                name = "Minibosses",
                selectionWeight = holder.familyMinibossWeight,
                cards = (holder.familyMinibosses != null ? holder.familyMinibosses.ToArray() : Array.Empty<DirectorCard>())
            };
            catSel.categories[2] = new DirectorCardCategorySelection.Category {
                name = "Basic Monsters",
                selectionWeight = holder.familyBasicMonsterWeight,
                cards = (holder.familyBasicMonsters != null ? holder.familyBasicMonsters.ToArray() : Array.Empty<DirectorCard>())
            };
            return new ClassicStageInfo.MonsterFamily {
                familySelectionChatString = holder.selectionChatString,
                maximumStageCompletion = holder.maxStageCompletion,
                minimumStageCompletion = holder.minStageCompletion,
                selectionWeight = holder.familySelectionWeight,
                monsterFamilyCategories = catSel
            };
        }
    }
}
