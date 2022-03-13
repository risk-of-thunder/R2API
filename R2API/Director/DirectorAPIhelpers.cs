using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

// Changing namespace to R2API.Director would be breaking
namespace R2API {

    //[R2APISubmodule]
    public static partial class DirectorAPI {

        /// <summary>
        /// This subclass contains helper methods for use with DirectorAPI.
        /// Note that there is much more flexibility by working with the API directly through its event system.
        /// The primary purpose of these helpers is to serve as example code, and to assist with very simple tasks.
        /// </summary>
        public static class Helpers {

            /// <summary>
            /// This class contains static strings for each <see cref="CharacterSpawnCard"/> in the base game.
            /// These can be used for matching names.
            /// </summary>
            public static class MonsterNames {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
                public static readonly string StoneTitanDistantRoost = "csctitanblackbeach";
                public static readonly string StoneTitanAbyssalDepths = "csctitandampcaves";
                public static readonly string StoneTitanTitanicPlains = "csctitangolemplains";
                public static readonly string StoneTitanAbandonedAqueduct = "csctitangoolake";
                public static readonly string ArchaicWisp = "cscarchwisp";
                public static readonly string StrikeDrone = "cscbackupdrone";
                public static readonly string Beetle = "cscbeetle";
                public static readonly string BeetleGuard = "cscbeetleguard";
                public static readonly string BeetleGuardFriendly = "cscbeetleguardally";
                public static readonly string BeetleQueen = "cscbeetlequeen";
                public static readonly string BrassContraption = "cscbell";
                public static readonly string BighornBison = "cscbison";
                public static readonly string ClayDunestrider = "cscclayboss";
                public static readonly string ClayTemplar = "cscclaybruiser";
                public static readonly string OverloadingWorm = "cscelectricworm";
                public static readonly string StoneGolem = "cscgolem";
                public static readonly string Grovetender = "cscgravekeeper";
                public static readonly string GreaterWisp = "cscgreaterwisp";
                public static readonly string HermitCrab = "cschermitcrab";
                public static readonly string Imp = "cscimp";
                public static readonly string ImpOverlord = "cscimpboss";
                public static readonly string Jellyfish = "cscjellyfish";
                public static readonly string Lemurian = "csclemurian";
                public static readonly string ElderLemurian = "csclemurianbruiser";
                public static readonly string LesserWisp = "csclesserwisp";
                public static readonly string MagmaWorm = "cscmagmaworm";
                public static readonly string SolusControlUnit = "cscroboballboss";
                public static readonly string SolusProbe = "cscroboballmini";
                public static readonly string AlloyWorshipUnit = "cscsuperroboballboss";
                public static readonly string AlliedWarshipUnit = "cscsuperroboballboss";
                public static readonly string FriendlyBoatUnit = "cscsuperroboballboss";
                public static readonly string Aurelionite = "csctitangold";
                public static readonly string AurelioniteAlly = "csctitangoldally";
                public static readonly string WanderingVagrant = "cscvagrant";
                public static readonly string AlloyVulture = "cscvulture";
                public static readonly string Scavenger = "cscscav";
                public static readonly string LunarScavenger = "cscscavlunar";
                public static readonly string VoidReaver = "cscnullifier";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
            }

            /// <summary>
            /// Most vanilla <see cref="ClassicStageInfo.monsterDccsPool"/> have (right now) 3 <see cref="DccsPool.Category"/> in them,
            /// their name are inside this class.
            /// </summary>
            public static class MonsterPoolCategories {

                /// <summary>
                /// Standard pool category
                /// </summary>
                public static readonly string Standard = "Standard";
                /// <summary>
                /// Weight of the standard pool category, usually 0.98f
                /// </summary>
                public static readonly float StandardWeight = 0.98f;

                /// <summary>
                /// Family pool category
                /// </summary>
                public static readonly string Family = "Family";
                /// <summary>
                /// Weight of the Family pool category, usually 0.02f
                /// </summary>
                public static readonly float FamilyWeight = 0.02f;

                /// <summary>
                /// Void Invasion pool category
                /// </summary>
                public static readonly string VoidInvasion = "VoidInvasion";
                /// <summary>
                /// Weight of the Void Invasion pool category, usually 0.02f
                /// </summary>
                public static readonly float VoidInvasionWeight = 0.02f;
            }

            /// <summary>
            /// Most vanilla <see cref="ClassicStageInfo.interactableDccsPool"/> have (right now) 1 <see cref="DccsPool.Category"/> in them,
            /// their name are inside this class.
            /// </summary>
            public static class InteractablePoolCategories {

                /// <summary>
                /// Standard pool category
                /// </summary>
                public static readonly string Standard = "Standard";
                /// <summary>
                /// Weight of the standard pool category, usually 1f
                /// </summary>
                public static readonly float StandardWeight = 1f;
            }

            /// <summary>
            /// This class contains static strings for each <see cref="InteractableSpawnCard"/> in the base game.
            /// These can be used for matching names.
            /// </summary>
            public static class InteractableNames {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
                public static readonly string Barrel = "iscbarrel1";
                public static readonly string GunnerDrone = "iscbrokendrone1";
                public static readonly string HealingDrone = "iscbrokendrone2";
                public static readonly string EquipmentDrone = "iscbrokenequipmentdrone";
                public static readonly string IncineratorDrone = "iscbrokenflamedrone";
                public static readonly string TC280 = "iscbrokenmegadrone";
                public static readonly string MissileDrone = "iscbrokenmissiledrone";
                public static readonly string GunnerTurret = "iscbrokenturret1";
                public static readonly string DamageChest = "isccategorychestdamage";
                public static readonly string HealingChest = "isccategorychesthealing";
                public static readonly string UtilityChest = "isccategorychestutility";
                public static readonly string BasicChest = "iscchest1";
                public static readonly string CloakedChest = "iscchest1stealthed";
                public static readonly string LargeChest = "iscchest2";
                public static readonly string PrinterCommon = "iscduplicator";
                public static readonly string PrinterUncommon = "iscduplicatorlarge";
                public static readonly string PrinterLegendary = "iscduplicatormilitary";
                public static readonly string EquipmentBarrel = "iscequipmentbarrel";
                public static readonly string LegendaryChest = "iscgoldchest";
                public static readonly string HalcyonBacon = "iscgoldshoresbracon";
                public static readonly string GoldPortal = "iscgoldshoresportal";
                public static readonly string Lockbox = "isclockbox";
                public static readonly string LunarBud = "isclunarchest";
                public static readonly string CelestialPortal = "iscmsportal";
                public static readonly string RadioScanner = "iscradartower";
                public static readonly string BluePortal = "iscshopportal";
                public static readonly string BloodShrine = "iscshrineblood";
                public static readonly string MountainShrine = "iscshrineboss";
                public static readonly string ChanceShrine = "iscshrinechance";
                public static readonly string CombatShrine = "iscshrinecombat";
                public static readonly string GoldShrine = "iscshrinegoldshoresaccess";
                public static readonly string WoodsShrine = "iscshrinehealing";
                public static readonly string OrderShrine = "iscshrinerestack";
                public static readonly string Teleporter = "iscteleporter";
                public static readonly string MultiShopCommon = "isctripleshop";
                public static readonly string MultiShopUncommon = "isctripleshoplarge";
                public static readonly string EmergencyDrone = "iscbrokenemergencydrone";
                public static readonly string AdaptiveChest = "isccasinochest";
                public static readonly string OvergrownPrinter = "iscduplicatorwild";
                public static readonly string ScavengerBackpack = "iscscavbackpack";
                public static readonly string LunarScavengerBackpack = "iscscavlunarbackpack";
                public static readonly string CleansingPool = "iscshrinecleanse";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
            }

            /// <summary>
            /// Try applying changes for the current stage (hot swap) for monster and family changes
            /// </summary>
            public static void TryApplyChangesNow() {
                ThrowIfNotLoaded();

                ClassicStageInfo.instance.ApplyChanges();
                ClassicStageInfo.instance.RebuildCards();
            }

            /// <summary>
            /// Enables or disables elite spawns for a specific monster.
            /// </summary>
            /// <param name="monsterName">The name of the monster to edit</param>
            /// <param name="elitesAllowed">Should elites be allowed?</param>
            public static void PreventElites(string? monsterName, bool elitesAllowed) {
                ThrowIfNotLoaded();

                MonsterActions += (dccsPool, oldDccs, mixEnemyArtifactMonsters, currentStage) => {

                    if (dccsPool) {
                        ForEachPoolEntryInDccsPool(dccsPool, (poolEntry) => {
                            PreventElitesForPoolEntry(monsterName, elitesAllowed, poolEntry);
                        });
                    }
                    else {
                        foreach (var holder in oldDccs) {
                            PreventElite(monsterName, elitesAllowed, holder.Card);
                        }
                    }

                    foreach (var holder in mixEnemyArtifactMonsters) {
                        PreventElite(monsterName, elitesAllowed, holder.Card);
                    }
                };

                static void PreventElitesForPoolEntry(string? monsterName, bool elitesAllowed, DccsPool.PoolEntry poolEntry) {
                    foreach (var category in poolEntry.dccs.categories) {
                        foreach (var card in category.cards) {
                            PreventElite(monsterName, elitesAllowed, card);
                        }
                    }
                }

                static void PreventElite(string? monsterName, bool elitesAllowed, DirectorCard card) {
                    if (string.Equals(card.spawnCard.name, monsterName, StringComparison.InvariantCultureIgnoreCase)) {
                        ((CharacterSpawnCard)card.spawnCard).noElites = elitesAllowed;
                    }
                }
            }

            /// <summary>
            /// Adds a new monster to all stages.
            /// </summary>
            /// <param name="monsterCard">The DirectorCard for the monster</param>
            /// <param name="monsterCategory">The category to add the monster to</param>
            [Obsolete("Use the overload with the DirectorCardHolder instead")]
            public static void AddNewMonster(DirectorCard? monsterCard, MonsterCategory monsterCategory) {
                ThrowIfNotLoaded();

                var directorCardHolder = new DirectorCardHolder {
                    Card = monsterCard,
                    MonsterCategory = monsterCategory
                };

                MonsterActions += (dccsPool, oldDccs, mixEnemyArtifactMonsters, currentStage) => {
                    AddNewMonster(dccsPool, oldDccs, mixEnemyArtifactMonsters, directorCardHolder);
                };
            }

            /// <summary>
            /// Adds a new monster to all stages.
            /// </summary>
            /// <param name="monsterCard"></param>
            public static void AddNewMonster(DirectorCardHolder? monsterCard) {
                ThrowIfNotLoaded();

                MonsterActions += (dccsPool, oldDccs, mixEnemyArtifactMonsters, currentStage) => {
                    AddNewMonster(dccsPool, oldDccs, mixEnemyArtifactMonsters, monsterCard);
                };
            }

            private static void AddNewMonster(
                DccsPool dccsPool, List<DirectorCardHolder> oldDccs, List<DirectorCardHolder> mixEnemyArtifactMonsters,
                DirectorCardHolder directorCardHolder) {
                if (dccsPool) {
                    ForEachPoolEntryInDccsPool(dccsPool, (poolEntry) => {
                        AddMonsterToPoolEntry(directorCardHolder, poolEntry);
                    });
                }
                else {
                    oldDccs.Add(directorCardHolder);
                }

                mixEnemyArtifactMonsters.Add(directorCardHolder);
            }

            private static void AddMonsterToPoolEntry(DirectorCardHolder directorCardHolder, DccsPool.PoolEntry poolEntry) {
                poolEntry.dccs.AddCard(directorCardHolder);
            }

            /// <summary>
            /// Adds a new monster to a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="monsterCard">The DirectorCard of the monster to add</param>
            /// <param name="monsterCategory">The category to add the monster to</param>
            /// <param name="stage">The stage to add the monster to</param>
            /// <param name="customStageName">The name of the custom stage</param>
            [Obsolete("Use the overload with the DirectorCardHolder instead")]
            public static void AddNewMonsterToStage(DirectorCard? monsterCard, MonsterCategory monsterCategory, Stage stage, string? customStageName = "") {
                ThrowIfNotLoaded();

                var directorCardHolder = new DirectorCardHolder {
                    Card = monsterCard,
                    MonsterCategory = monsterCategory
                };

                MonsterActions += (dccsPool, oldDccs, mixEnemyArtifactMonsters, currentStage) => {
                    if (currentStage.stage == stage) {
                        if (currentStage.CheckStage(stage, customStageName)) {
                            AddNewMonster(dccsPool, oldDccs, mixEnemyArtifactMonsters, directorCardHolder);
                        }
                    }
                };
            }

            /// <summary>
            /// Adds a new monster to a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="monsterCard"></param>
            /// <param name="stage"></param>
            /// <param name="customStageName"></param>
            public static void AddNewMonsterToStage(DirectorCardHolder monsterCard, Stage stage, string? customStageName = "") {
                ThrowIfNotLoaded();

                MonsterActions += (dccsPool, oldDccs, mixEnemyArtifactMonsters, currentStage) => {
                    if (currentStage.stage == stage) {
                        if (currentStage.CheckStage(stage, customStageName)) {
                            AddNewMonster(dccsPool, oldDccs, mixEnemyArtifactMonsters, monsterCard);
                        }
                    }
                };
            }

            /// <summary>
            /// Adds a new interactable to all stages.
            /// </summary>
            /// <param name="interactableCard">The DirectorCard for the interactable</param>
            /// <param name="interactableCategory">The category of the interactable</param>
            [Obsolete("Use the overload with the DirectorCardHolder instead")]
            public static void AddNewInteractable(DirectorCard? interactableCard, InteractableCategory interactableCategory) {
                ThrowIfNotLoaded();

                var directorCardHolder = new DirectorCardHolder {
                    Card = interactableCard,
                    InteractableCategory = interactableCategory,
                };

                InteractableActions += (interactablesDccsPool, oldDccs, currentStage) => {
                    AddNewInteractableToStage(interactablesDccsPool, oldDccs, directorCardHolder);
                };
            }

            /// <summary>
            /// Adds a new interactable to all stages.
            /// </summary>
            /// <param name="directorCardHolder"></param>
            public static void AddNewInteractable(DirectorCardHolder? directorCardHolder) {
                ThrowIfNotLoaded();

                InteractableActions += (interactablesDccsPool, oldDccs, currentStage) => {
                    AddNewInteractableToStage(interactablesDccsPool, oldDccs, directorCardHolder);
                };
            }

            private static void AddInteractableToPoolEntry(DirectorCardHolder directorCardHolder, DccsPool.PoolEntry poolEntry) {
                poolEntry.dccs.AddCard(directorCardHolder);
            }

            /// <summary>
            /// Adds a new interactable to a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="interactableCard">The DirectorCard of the interactable</param>
            /// <param name="interactableCategory">The category of the interactable</param>
            /// <param name="stage">The stage to add the interactable to</param>
            /// <param name="customStageName">The name of the custom stage</param>
            [Obsolete("Use the overload with the DirectorCardHolder instead")]
            public static void AddNewInteractableToStage(DirectorCard? interactableCard, InteractableCategory interactableCategory, Stage stage, string? customStageName = "") {
                ThrowIfNotLoaded();

                var directorCardHolder = new DirectorCardHolder {
                    Card = interactableCard,
                    InteractableCategory = interactableCategory
                };

                InteractableActions += (interactablesDccsPool, oldDccs, currentStage) => {
                    if (currentStage.stage == stage) {
                        if (currentStage.CheckStage(stage, customStageName)) {
                            AddNewInteractableToStage(interactablesDccsPool, oldDccs, directorCardHolder);
                        }
                    }
                };
            }

            private static void AddNewInteractableToStage(
                DccsPool interactablesDccsPool, List<DirectorCardHolder> oldDccs,
                DirectorCardHolder directorCardHolder) {
                if (interactablesDccsPool) {
                    ForEachPoolEntryInDccsPool(interactablesDccsPool, (poolEntry) => {
                        AddInteractableToPoolEntry(directorCardHolder, poolEntry);
                    });
                }
                else {
                    oldDccs.Add(directorCardHolder);
                }
            }

            /// <summary>
            /// Removes a monster from spawns on all stages.
            /// </summary>
            /// <param name="monsterName">The name of the monster card to remove</param>
            public static void RemoveExistingMonster(string? monsterName) {
                ThrowIfNotLoaded();
                StringUtils.ThrowIfStringIsNullOrWhiteSpace(monsterName, nameof(monsterName));

                var monsterNameLowered = monsterName.ToLowerInvariant();

                MonsterActions += (dccsPool, oldDccs, mixEnemyArtifactMonsters, currentStage) => {
                    RemoveExistingMonster(dccsPool, oldDccs, mixEnemyArtifactMonsters, monsterNameLowered);
                };
            }

            private static void RemoveExistingMonster(DccsPool dccsPool, List<DirectorCardHolder> oldDccs, List<DirectorCardHolder> mixEnemyArtifactMonsters, string monsterNameLowered) {
                if (dccsPool) {
                    ForEachPoolEntryInDccsPool(dccsPool, (poolEntry) => {
                        RemoveMonsterFromPoolEntry(monsterNameLowered, poolEntry);
                    });
                }
                else {
                    oldDccs.RemoveAll((card) => card.Card.spawnCard.name.ToLowerInvariant() == monsterNameLowered);
                }

                mixEnemyArtifactMonsters.RemoveAll((card) => card.Card.spawnCard.name.ToLowerInvariant() == monsterNameLowered);
            }

            private static void RemoveMonsterFromPoolEntry(string monsterNameLowered, DccsPool.PoolEntry poolEntry) {
                for (int i = 0; i < poolEntry.dccs.categories.Length; i++) {
                    var cards = poolEntry.dccs.categories[i].cards.ToList();
                    cards.RemoveAll((card) => card.spawnCard.name.ToLowerInvariant() == monsterNameLowered);
                    poolEntry.dccs.categories[i].cards = cards.ToArray();
                }
            }

            /// <summary>
            /// Removes a monster from spawns on a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="monsterName">The name of the monster card to remove</param>
            /// <param name="stage">The stage to remove on</param>
            /// <param name="customStageName">The name of the custom stage</param>
            public static void RemoveExistingMonsterFromStage(string? monsterName, Stage stage, string? customStageName = "") {
                ThrowIfNotLoaded();
                StringUtils.ThrowIfStringIsNullOrWhiteSpace(monsterName, nameof(monsterName));

                var monsterNameLowered = monsterName.ToLowerInvariant();

                MonsterActions += (dccsPool, oldDccs, mixEnemyArtifactMonsters, currentStage) => {
                    if (currentStage.stage == stage) {
                        if (currentStage.CheckStage(stage, customStageName)) {
                            RemoveExistingMonster(dccsPool, oldDccs, mixEnemyArtifactMonsters, monsterNameLowered);
                        }
                    }
                };
            }

            /// <summary>
            /// Remove an interactable from spawns on all stages.
            /// </summary>
            /// <param name="interactableName">Name of the interactable to remove</param>
            public static void RemoveExistingInteractable(string? interactableName) {
                ThrowIfNotLoaded();
                StringUtils.ThrowIfStringIsNullOrWhiteSpace(interactableName, nameof(interactableName));

                var interactableNameLowered = interactableName.ToLowerInvariant();

                InteractableActions += (interactablesDccsPool, oldDccs, currentStage) => {
                    RemoveExistingInteractable(interactablesDccsPool, oldDccs, interactableNameLowered);
                };
            }

            private static void RemoveExistingInteractable(DccsPool interactablesDccsPool, List<DirectorCardHolder> oldDccs, string interactableNameLowered) {
                if (interactablesDccsPool) {
                    ForEachPoolEntryInDccsPool(interactablesDccsPool, (poolEntry) => {
                        RemoveInteractableFromPoolEntry(interactableNameLowered, poolEntry);
                    });
                }
                else {
                    oldDccs.RemoveAll((card) => card.Card.spawnCard.name.ToLowerInvariant() == interactableNameLowered);
                }
            }

            /// <summary>
            /// Remove an interactable from spawns on a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="interactableName">The name of the interactable to remove</param>
            /// <param name="stage">The stage to remove on</param>
            /// <param name="customStageName">The name of the custom stage</param>
            public static void RemoveExistingInteractableFromStage(string? interactableName, Stage stage, string? customStageName = "") {
                ThrowIfNotLoaded();
                StringUtils.ThrowIfStringIsNullOrWhiteSpace(interactableName, nameof(interactableName));

                var interactableNameLowered = interactableName.ToLowerInvariant();

                InteractableActions += (interactablesDccsPool, oldDccs, currentStage) => {
                    if (currentStage.stage == stage) {
                        if (currentStage.CheckStage(stage, customStageName)) {
                            RemoveExistingInteractable(interactablesDccsPool, oldDccs, interactableNameLowered);
                        }
                    }
                };
            }

            private static void RemoveInteractableFromPoolEntry(string interactableNameLowered, DccsPool.PoolEntry poolEntry) {
                for (int i = 0; i < poolEntry.dccs.categories.Length; i++) {
                    var cards = poolEntry.dccs.categories[i].cards.ToList();
                    cards.RemoveAll((card) => card.spawnCard.name.ToLowerInvariant() == interactableNameLowered);
                    poolEntry.dccs.categories[i].cards = cards.ToArray();
                }
            }

            /// <summary>
            /// Adds a flat amount of monster credits to the scene director on a specific stage.
            /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
            /// </summary>
            /// <param name="increase">The quantity to add</param>
            /// <param name="stage">The stage to add on</param>
            /// <param name="customStageName">The name of the custom stage</param>
            public static void AddSceneMonsterCredits(int increase, Stage stage, string? customStageName = "") {
                ThrowIfNotLoaded();

                StageSettingsActions += (settings, currentStage) => {
                    if (currentStage.stage == stage) {
                        if (currentStage.CheckStage(stage, customStageName)) {
                            settings.SceneDirectorMonsterCredits += increase;
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
            public static void AddSceneInteractableCredits(int increase, Stage stage, string? customStageName = "") {
                ThrowIfNotLoaded();

                StageSettingsActions += (settings, currentStage) => {
                    if (currentStage.stage == stage) {
                        if (currentStage.CheckStage(stage, customStageName)) {
                            settings.SceneDirectorInteractableCredits += increase;
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
            public static void MultiplySceneMonsterCredits(int multiplier, Stage stage, string? customStageName = "") {
                ThrowIfNotLoaded();

                StageSettingsActions += (settings, currentStage) => {
                    if (currentStage.stage == stage) {
                        if (currentStage.CheckStage(stage, customStageName)) {
                            settings.SceneDirectorMonsterCredits *= multiplier;
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
            public static void MultiplySceneInteractableCredits(int multiplier, Stage stage, string? customStageName = "") {
                ThrowIfNotLoaded();

                StageSettingsActions += (settings, currentStage) => {
                    if (currentStage.stage == stage) {
                        if (currentStage.CheckStage(stage, customStageName)) {
                            settings.SceneDirectorInteractableCredits *= multiplier;
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
            public static void ReduceSceneMonsterCredits(int divisor, Stage stage, string? customStageName = "") {
                ThrowIfNotLoaded();

                StageSettingsActions += (settings, currentStage) => {
                    if (currentStage.stage == stage) {
                        if (currentStage.CheckStage(stage, customStageName)) {
                            settings.SceneDirectorMonsterCredits /= divisor;
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
            public static void ReduceSceneInteractableCredits(int divisor, Stage stage, string? customStageName = "") {
                ThrowIfNotLoaded();

                StageSettingsActions += (settings, currentStage) => {
                    if (currentStage.stage == stage) {
                        if (currentStage.CheckStage(stage, customStageName)) {
                            settings.SceneDirectorInteractableCredits /= divisor;
                        }
                    }
                };
            }

            /// <summary>
            /// For each <see cref="DccsPool.PoolEntry"/> in a <see cref="DccsPool"/>, call the given <see cref="Action"/>.
            /// </summary>
            /// <param name="dccsPool"></param>
            /// <param name="action"></param>
            public static void ForEachPoolEntryInDccsPool(DccsPool dccsPool, Action<DccsPool.PoolEntry> action) {
                foreach (var poolCategory in dccsPool.poolCategories) {
                    foreach (var poolEntry in poolCategory.alwaysIncluded) {
                        action(poolEntry);
                    }

                    foreach (var poolEntry in poolCategory.includedIfConditionsMet) {
                        action(poolEntry);
                    }

                    foreach (var poolEntry in poolCategory.includedIfNoConditionsMet) {
                        action(poolEntry);
                    }
                }
            }

            /// <summary>
            /// Returns true if the <see cref="DirectorCardCategorySelection.Category.name"/> is the same as the <see cref="MonsterCategory"/>
            /// </summary>
            /// <param name="category"></param>
            /// <param name="monsterCategory"></param>
            /// <returns></returns>
            public static bool IsSameMonsterCategory(ref DirectorCardCategorySelection.Category category, MonsterCategory monsterCategory) {
                return GetMonsterCategory(category.name) == monsterCategory;
            }

            /// <summary>
            /// Returns the enum value corresponding the given string, returns <see cref="MonsterCategory.Custom"/> if the category is a custom one.
            /// </summary>
            /// <param name="categoryString"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentException"></exception>
            public static MonsterCategory GetMonsterCategory(string categoryString) {
                if (string.IsNullOrWhiteSpace(categoryString)) {
                    throw new ArgumentException(nameof(categoryString));
                }

                return categoryString switch {
                    "Champions" => MonsterCategory.Champions,
                    "Minibosses" => MonsterCategory.Minibosses,
                    "Basic Monsters" => MonsterCategory.BasicMonsters,
                    "Special" => MonsterCategory.Special,
                    _ => MonsterCategory.Custom,
                };
            }

            /// <summary>
            /// Get the string corresponding to the given vanilla <see cref="MonsterCategory"/>.
            /// Throws if the given <see cref="MonsterCategory"/> is not vanilla.
            /// </summary>
            /// <param name="monsterCategory"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentException"></exception>
            public static string GetVanillaMonsterCategoryName(MonsterCategory monsterCategory) {
                return monsterCategory switch {
                    MonsterCategory.BasicMonsters => "Basic Monsters",
                    MonsterCategory.Champions => "Champions",
                    MonsterCategory.Minibosses => "Minibosses",
                    MonsterCategory.Special => "Special",
                    _ => throw new ArgumentException(((int)monsterCategory).ToString())
                };
            }

            /// <summary>
            /// Returns the enum value corresponding the given string, returns <see cref="InteractableCategory.Custom"/> if the category is a custom one.
            /// </summary>
            /// <param name="categoryString"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentException"></exception>
            public static InteractableCategory GetInteractableCategory(string categoryString) {
                if (string.IsNullOrWhiteSpace(categoryString)) {
                    throw new ArgumentException(nameof(categoryString));
                }

                return categoryString switch {
                    "Chests" => InteractableCategory.Chests,
                    "Barrels" => InteractableCategory.Barrels,
                    "Shrines" => InteractableCategory.Shrines,
                    "Drones" => InteractableCategory.Drones,
                    "Misc" => InteractableCategory.Misc,
                    "Rare" => InteractableCategory.Rare,
                    "Duplicator" => InteractableCategory.Duplicator,
                    "Void Stuff" => InteractableCategory.VoidStuff,
                    _ => InteractableCategory.Custom,
                };
            }

            /// <summary>
            /// Get the string corresponding to the given vanilla <see cref="InteractableCategory"/>.
            /// Throws if the given <see cref="InteractableCategory"/> is not vanilla.
            /// </summary>
            /// <param name="interactableCategory"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentException"></exception>
            public static string GetVanillaInteractableCategoryName(InteractableCategory interactableCategory) {
                return interactableCategory switch {
                    InteractableCategory.Chests => "Chests",
                    InteractableCategory.Barrels => "Barrels",
                    InteractableCategory.Shrines => "Shrines",
                    InteractableCategory.Drones => "Drones",
                    InteractableCategory.Misc => "Misc",
                    InteractableCategory.Rare => "Rare",
                    InteractableCategory.Duplicator => "Duplicator",
                    InteractableCategory.VoidStuff => "Void Stuff",
                    _ => throw new ArgumentException(((int)interactableCategory).ToString())
                };
            }

            /// <summary>
            /// Returns true if the <see cref="DirectorCardCategorySelection.Category.name"/> is the same as the <see cref="InteractableCategory"/>
            /// </summary>
            /// <param name="category"></param>
            /// <param name="interactableCategory"></param>
            /// <returns></returns>
            public static bool IsSameInteractableCategory(ref DirectorCardCategorySelection.Category category, InteractableCategory interactableCategory) {
                return GetInteractableCategory(category.name) == interactableCategory;
            }
        }
    }
}
