using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using R2API.Utils;
using RoR2;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    //[R2APISubmodule]
    public static partial class DirectorAPI {
        /// <summary>
        /// This subclass contains helper methods for use with DirectorAPI.
        /// Note that there is much more flexibility by working with the API directly through its event system.
        /// The primary purpose of these helpers is to serve as example code, and to assist with very simple tasks.
        /// They are NOT intended to be, or ever will be, a comprehensive way to use the DirectorAPI.
        /// </summary>
        public static class Helpers {
            /// <summary>
            /// This class contains static strings for each characterspawncard in the base game.
            /// These can be used for matching names.
            /// </summary>
            public static class MonsterNames {
                public static readonly String StoneTitanDistantRoost = "csctitanblackbeach";
                public static readonly String StoneTitanAbyssalDepths = "csctitandampcaves";
                public static readonly String StoneTitanTitanicPlains = "csctitangolemplains";
                public static readonly String StoneTitanAbandonedAqueduct = "csctitangoolake";
                public static readonly String ArchaicWisp = "cscarchwisp";
                public static readonly String StrikeDrone = "cscbackupdrone";
                public static readonly String Beetle = "cscbeetle";
                public static readonly String BeetleGuard = "cscbeetleguard";
                public static readonly String BeetleGuardFriendly = "cscbeetleguardally";
                public static readonly String BeetleQueen = "cscbeetlequeen";
                public static readonly String BrassContraption = "cscbell";
                public static readonly String BighornBison = "cscbison";
                public static readonly String ClayDunestrider = "cscclayboss";
                public static readonly String ClayTemplar = "cscclaybruiser";
                public static readonly String OverloadingWorm = "cscelectricworm";
                public static readonly String StoneGolem = "cscgolem";
                public static readonly String Grovetender = "cscgravekeeper";
                public static readonly String GreaterWisp = "cscgreaterwisp";
                public static readonly String HermitCrab = "cschermitcrab";
                public static readonly String Imp = "cscimp";
                public static readonly String ImpOverlord = "cscimpboss";
                public static readonly String Jellyfish = "cscjellyfish";
                public static readonly String Lemurian = "csclemurian";
                public static readonly String ElderLemurian = "csclemurianbruiser";
                public static readonly String LesserWisp = "csclesserwisp";
                public static readonly String MagmaWorm = "cscmagmaworm";
                public static readonly String SolusControlUnit = "cscroboballboss";
                public static readonly String SolusProbe = "cscroboballmini";
                public static readonly String AlloyWorshipUnit = "cscsuperroboballboss";
                public static readonly String AlliedWarshipUnit = "cscsuperroboballboss";
                public static readonly String FriendlyBoatUnit = "cscsuperroboballboss";
                public static readonly String Aurelionite = "csctitangold";
                public static readonly String AurelioniteAlly = "csctitangoldally";
                public static readonly String WanderingVagrant = "cscvagrant";
                public static readonly String AlloyVulture = "cscvulture";
            }

            /// <summary>
            /// This class contains static strings for each interactablespawncard in the base game.
            /// These can be used for matching names.
            /// </summary>
            public static class InteractableNames {
                public static readonly String Barrel = "iscbarrel1";
                public static readonly String GunnerDrone = "iscbrokendrone1";
                public static readonly String HealingDrone = "iscbrokendrone2";
                public static readonly String EquipmentDrone = "iscbrokenequipmentdrone";
                public static readonly String IncineratorDrone = "iscbrokenflamedrone";
                public static readonly String TC280 = "iscbrokenmegadrone";
                public static readonly String MissileDrone = "iscbrokenmissiledrone";
                public static readonly String GunnerTurret = "iscbrokenturret1";
                public static readonly String DamageChest = "isccategorychestdamage";
                public static readonly String HealingChest = "isccategorychesthealing";
                public static readonly String UtilityChest = "isccategorychestutility";
                public static readonly String BasicChest = "iscchest1";
                public static readonly String CloakedChest = "iscchest1stealthed";
                public static readonly String LargeChest = "iscchest2";
                public static readonly String PrinterCommon = "iscduplicator";
                public static readonly String PrinterUncommon = "iscduplicatorlarge";
                public static readonly String PrinterLegendary = "iscduplicatormilitary";
                public static readonly String EquipmentBarrel = "iscequipmentbarrel";
                public static readonly String LegendaryChest = "iscgoldchest";
                public static readonly String HalcyonBacon = "iscgoldshoresbracon";
                public static readonly String GoldPortal = "iscgoldshoresportal";
                public static readonly String Lockbox = "isclockbox";
                public static readonly String LunarBud = "isclunarchest";
                public static readonly String CelestialPortal = "iscmsportal";
                public static readonly String RadioScanner = "iscradartower";
                public static readonly String BluePortal = "iscshopportal";
                public static readonly String BloodShrine = "iscshrineblood";
                public static readonly String MountainShrine = "iscshrineboss";
                public static readonly String ChanceShrine = "iscshrinechance";
                public static readonly String CombatShrine = "iscshrinecombat";
                public static readonly String GoldShrine = "iscshrinegoldshoresaccess";
                public static readonly String WoodsShrine = "iscshrinehealing";
                public static readonly String OrderShrine = "iscshrinerestack";
                public static readonly String Teleporter = "iscteleporter";
                public static readonly String MultiShopCommon = "isctripleshop";
                public static readonly String MultiShopUncommon = "isctripleshoplarge";
            }


            /// <summary>
            /// Enables or disables elite spawns for a specific monster.
            /// </summary>
            /// <param name="monsterName">The name of the monster to edit</param>
            /// <param name="elitesAllowed">Should elites be allowed?</param>
            public static void PreventElites( String monsterName, Boolean elitesAllowed ) {
                
                DirectorAPI.monsterActions += ( monsters, currentStage ) => {
                    foreach( DirectorCardHolder holder in monsters ) {
                        if( holder.card.spawnCard.name.ToLower() == monsterName.ToLower() ) {
                            ((CharacterSpawnCard)holder.card.spawnCard).noElites = elitesAllowed;
                        }
                    }
                };
            }

            /// <summary>
            /// Adds a new monster to all stages.
            /// </summary>
            /// <param name="monsterCard">The DirectorCard for the monster</param>
            /// <param name="category">The category to add the monster to</param>
            public static void AddNewMonster( DirectorCard monsterCard, MonsterCategory category ) {
                
                DirectorCardHolder card = new DirectorCardHolder
                {
                    card = monsterCard,
                    interactableCategory = InteractableCategory.None,
                    monsterCategory = category
                };
                DirectorAPI.monsterActions += ( monsters, currentStage ) => {
                    monsters.Add( card );
                };
            }

            /// <summary>
            /// Adds a new monster to a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="monsterCard">The DirectorCard of the monster to add</param>
            /// <param name="category">The category to add the monster to</param>
            /// <param name="stage">The stage to add the monster to</param>
            /// <param name="customStageName">The name of the custom stage</param>
            public static void AddNewMonsterToStage( DirectorCard monsterCard, MonsterCategory category, Stage stage, String customStageName = "" ) {
                
                DirectorCardHolder card = new DirectorCardHolder
                {
                    card = monsterCard,
                    interactableCategory = InteractableCategory.None,
                    monsterCategory = category
                };
                DirectorAPI.monsterActions += ( monsters, currentStage ) => {
                    if( currentStage.stage == stage ) {
                        if( currentStage.CheckStage( stage, customStageName ) ) {
                            monsters.Add( card );
                        }
                    }
                };
            }

            /// <summary>
            /// Adds a new interactable to all stages.
            /// </summary>
            /// <param name="interactableCard">The DirectorCard for the interactable</param>
            /// <param name="category">The category of the interactable</param>
            public static void AddNewInteractable( DirectorCard interactableCard, InteractableCategory category ) {
                
                DirectorCardHolder card = new DirectorCardHolder
                {
                    card = interactableCard,
                    interactableCategory = category,
                    monsterCategory = MonsterCategory.None
                };
                DirectorAPI.interactableActions += ( interactables, currentStage ) => {
                    interactables.Add( card );
                };
            }

            /// <summary>
            /// Adds a new interactable to a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="interactableCard">The DirectorCard of the interactable</param>
            /// <param name="category">The category of the interactable</param>
            /// <param name="stage">The stage to add the interactable to</param>
            /// <param name="customStageName">The name of the custom stage</param>
            public static void AddNewInteractableToStage( DirectorCard interactableCard, InteractableCategory category, Stage stage, String customStageName = "" ) {
                
                DirectorCardHolder card = new DirectorCardHolder
                {
                    card = interactableCard,
                    interactableCategory = category,
                    monsterCategory = MonsterCategory.None
                };
                DirectorAPI.interactableActions += ( interactables, currentStage ) => {
                    if( currentStage.stage == stage ) {
                        if( currentStage.CheckStage( stage, customStageName ) ) {
                            interactables.Add( card );
                        }
                    }
                };
            }

            /// <summary>
            /// Removes a monster from spawns on all stages.
            /// </summary>
            /// <param name="monsterName">The name of the monster card to remove</param>
            public static void RemoveExistingMonster( String monsterName ) {
                
                DirectorAPI.monsterActions += ( monsters, currentStage ) => {
                    monsters.RemoveAll( ( card ) => (card.card.spawnCard.name.ToLower() == monsterName.ToLower()) );
                };
            }

            /// <summary>
            /// Removes a monster from spawns on a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="monsterName">The name of the monster card to remove</param>
            /// <param name="stage">The stage to remove on</param>
            /// <param name="customStageName">The name of the custom stage</param>
            public static void RemoveExistingMonsterFromStage( String monsterName, Stage stage, String customStageName = "" ) {
                
                DirectorAPI.monsterActions += ( monsters, currentStage ) => {
                    if( currentStage.stage == stage ) {
                        if( (stage != Stage.Custom) ^ (currentStage.customStageName == customStageName) ) {
                            monsters.RemoveAll( ( card ) => (card.card.spawnCard.name.ToLower() == monsterName.ToLower()) );
                        }
                    }
                };
            }

            /// <summary>
            /// Remove an interactable from spawns on all stages.
            /// </summary>
            /// <param name="interactableName">Name of the interactable to remove</param>
            public static void RemoveExistingInteractable( String interactableName ) {
                
                DirectorAPI.interactableActions += ( interactables, currentStage ) => {
                    interactables.RemoveAll( ( card ) => (card.card.spawnCard.name.ToLower() == interactableName.ToLower()) );
                };
            }

            /// <summary>
            /// Remove an interactable from spawns on a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="interactableName">The name of the interactable to remove</param>
            /// <param name="stage">The stage to remove on</param>
            /// <param name="customStageName">The name of the custom stage</param>
            public static void RemoveExistingInteractableFromStage( String interactableName, Stage stage, String customStageName = "" ) {
                
                DirectorAPI.interactableActions += ( interactables, currentStage ) => {
                    if( currentStage.stage == stage ) {
                        if( currentStage.CheckStage( stage, customStageName ) ) {
                            interactables.RemoveAll( ( card ) => (card.card.spawnCard.name.ToLower() == interactableName.ToLower()) );
                        }
                    }
                };
            }

            /// <summary>
            /// Adds a flat amount of monster credits to the scene director on a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="increase">The quantity to add</param>
            /// <param name="stage">The stage to add on</param>
            /// <param name="customStageName">The name of the custom stage</param>
            public static void AddSceneMonsterCredits( Int32 increase, Stage stage, String customStageName = "" ) {
                
                DirectorAPI.stageSettingsActions += ( settings, currentStage ) => {
                    if( currentStage.stage == stage ) {
                        if( currentStage.CheckStage( stage, customStageName ) ) {
                            settings.sceneDirectorMonsterCredits += increase;
                        }
                    }
                };
            }

            /// <summary>
            /// Adds a flat amount of interactable credits to the scene director on a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="increase">The quantity to add</param>
            /// <param name="stage">The stage to add on</param>
            /// <param name="customStageName">The name of the custom stage</param>
            public static void AddSceneInteractableCredits( Int32 increase, Stage stage, String customStageName = "" ) {
                
                DirectorAPI.stageSettingsActions += ( settings, currentStage ) => {
                    if( currentStage.stage == stage ) {
                        if( currentStage.CheckStage( stage, customStageName ) ) {
                            settings.sceneDirectorInteractableCredits += increase;
                        }
                    }
                };
            }

            /// <summary>
            /// Multiplies the scene director monster credits on a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="multiplier">The number to multiply by</param>
            /// <param name="stage">The stage to multiply on</param>
            /// <param name="customStageName">The name of the custom stage</param>
            public static void MultiplySceneMonsterCredits( Int32 multiplier, Stage stage, String customStageName = "" ) {
                
                DirectorAPI.stageSettingsActions += ( settings, currentStage ) => {
                    if( currentStage.stage == stage ) {
                        if( currentStage.CheckStage( stage, customStageName ) ) {
                            settings.sceneDirectorMonsterCredits *= multiplier;
                        }
                    }
                };
            }

            /// <summary>
            /// Multiplies the scene director interactable credits on a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="multiplier">The number to multiply by</param>
            /// <param name="stage">The stage to multiply on</param>
            /// <param name="customStageName">The name of the custom stage</param>
            public static void MultiplySceneInteractableCredits( Int32 multiplier, Stage stage, String customStageName = "" ) {
                
                DirectorAPI.stageSettingsActions += ( settings, currentStage ) => {
                    if( currentStage.stage == stage ) {
                        if( currentStage.CheckStage( stage, customStageName ) ) {
                            settings.sceneDirectorInteractableCredits *= multiplier;
                        }
                    }
                };
            }

            /// <summary>
            /// Divides the scene director monster credits on a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="divisor">The number to divide by</param>
            /// <param name="stage">The stage to divide on</param>
            /// <param name="customStageName">The name of the custom stage</param>
            public static void ReduceSceneMonsterCredits( Int32 divisor, Stage stage, String customStageName = "" ) {
                
                DirectorAPI.stageSettingsActions += ( settings, currentStage ) => {
                    if( currentStage.stage == stage ) {
                        if( currentStage.CheckStage( stage, customStageName ) ) {
                            settings.sceneDirectorMonsterCredits /= divisor;
                        }
                    }
                };
            }

            /// <summary>
            /// Divides the scene director interactable credits on a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="divisor">The number to divide by</param>
            /// <param name="stage">The stage to divide on</param>
            /// <param name="customStageName">The name of the custom stage</param>
            public static void ReduceSceneInteractableCredits( Int32 divisor, Stage stage, String customStageName = "" ) {
                
                DirectorAPI.stageSettingsActions += ( settings, currentStage ) => {
                    if( currentStage.stage == stage ) {
                        if( currentStage.CheckStage( stage, customStageName ) ) {
                            settings.sceneDirectorInteractableCredits /= divisor;
                        }
                    }
                };
            }
        }
    }
}
