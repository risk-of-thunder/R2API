using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

// Changing namespace to R2API.Director would be breaking
namespace R2API;

//[R2APISubmodule]
public static partial class DirectorAPI
{

    /// <summary>
    /// This subclass contains helper methods for use with DirectorAPI.
    /// Note that there is much more flexibility by working with the API directly through its event system.
    /// The primary purpose of these helpers is to serve as example code, and to assist with very simple tasks.
    /// </summary>
    public static class Helpers
    {

        /// <summary>
        /// This class contains static strings for each <see cref="CharacterSpawnCard"/> in the base game.
        /// These can be used for matching names.
        /// </summary>
        public static class MonsterNames
        {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            public static readonly string Beetle = "cscbeetle";
            public static readonly string BeetleGuard = "cscbeetleguard";
            public static readonly string BeetleQueen = "cscbeetlequeen";
            public static readonly string BrassContraption = "cscbell";
            public static readonly string BighornBison = "cscbison";
            public static readonly string Mithrix = "cscbrother";
            public static readonly string MithrixHurt = "cscbrotherhurt";
            public static readonly string ClayDunestrider = "cscclayboss";
            public static readonly string ClayTemplar = "cscclaybruiser";
            public static readonly string StrikeDrone = "cscbackupdrone";
            public static readonly string GunnerDrone = "cscdrone1";
            public static readonly string HealingDrone = "cscdrone2";
            public static readonly string TC280Prototype = "cscmegadrone";
            public static readonly string OverloadingWorm = "cscelectricworm";
            public static readonly string StoneGolem = "cscgolem";
            public static readonly string Grandparent = "cscgrandparent";
            public static readonly string Grovetender = "cscgravekeeper";
            public static readonly string GreaterWisp = "cscgreaterwisp";
            public static readonly string HermitCrab = "cschermitcrab";
            public static readonly string Imp = "cscimp";
            public static readonly string ImpOverlord = "cscimpboss";
            public static readonly string Jellyfish = "cscjellyfish";
            public static readonly string Lemurian = "csclemurian";
            public static readonly string ElderLemurian = "csclemurianbruiser";
            public static readonly string LunarChimeraExploder = "csclunarexploder";
            public static readonly string LunarChimeraGolem = "csclunargolem";
            public static readonly string LunarChimeraWisp = "csclunarwisp";
            public static readonly string MagmaWorm = "cscmagmaworm";
            public static readonly string MiniMushrum = "cscminimushroom";
            public static readonly string VoidReaver = "cscnullifier";
            public static readonly string VoidReaverAlly = "cscnullifierally";
            public static readonly string Parent = "cscparent";
            public static readonly string SolusControlUnit = "cscroboballboss";
            public static readonly string SolusProbe = "cscroboballmini";
            public static readonly string AlloyWorshipUnit = "cscsuperroboballboss";
            public static readonly string Scavenger = "cscscav";
            public static readonly string ScavengerBoss = "cscscavboss";
            public static readonly string TwiptwiptheDevotee = "cscscavlunar";
            public static readonly string StoneTitan = "csctitanblackbeach";
            public static readonly string StoneTitanAbyssalDepths = "csctitandampcave";
            public static readonly string StoneTitanGolemPlains = "csctitangolemplains";
            public static readonly string StoneTitanAbandonedAqueduct = "csctitangoolake";
            public static readonly string Aurelionite = "csctitangold";
            public static readonly string WanderingVagrant = "cscvagrant";
            public static readonly string AlloyVulture = "cscvulture";
            public static readonly string LesserWisp = "csclesserwisp";
            public static readonly string BeetleGuardAlly = "cscbeetleguardally";
            public static readonly string DelightedProbe = "cscroboballgreenbuddy";
            public static readonly string QuietProbe = "cscroboballredbuddy";
            public static readonly string SquidTurret = "cscsquidturret";
            public static readonly string AurelioniteAlly = "csctitangoldally";
            public static readonly string Larva = "cscacidlarva";
            public static readonly string Assassin = "cscassassin2";
            public static readonly string ClayApothecary = "cscclaygrenadier";
            public static readonly string ColDroneman = "cscdronecommander";
            public static readonly string BlindPest = "cscflyingvermin";
            public static readonly string BlindPestSnowy = "cscflyingverminsnowy";
            public static readonly string Geep = "cscgeepbody";
            public static readonly string Gip = "cscgipbody";
            public static readonly string Gup = "cscgupbody";
            public static readonly string MAJORCONSTRUCT_BODY_NAME = "cscmajorconstruct";
            public static readonly string XiConstruct = "cscmegaconstruct";
            public static readonly string AlphaConstruct = "cscminorconstruct";
            public static readonly string AlphaConstructAttachable = "cscminorconstructattachable";
            public static readonly string AlphaConstructOnKill = "cscminorconstructonkill";
            public static readonly string BlindVermin = "cscvermin";
            public static readonly string BlindVerminSnowy = "cscverminsnowy";
            public static readonly string VoidBarnacle = "cscvoidbarnacle";
            public static readonly string VoidBarnacleAlly = "cscvoidbarnacleally";
            public static readonly string VoidBarnacleNoCast = "cscvoidbarnaclenocast";
            public static readonly string VoidJailer = "cscvoidjailer";
            public static readonly string VoidJailerAlly = "cscvoidjailerally";
            public static readonly string VoidDevastator = "cscvoidmegacrab";
            public static readonly string Voidling = "cscvoidraidcrab";
            public static readonly string VoidlingJoint = "cscvoidraidcrabjoint";
            public static readonly string VoidlingBase = "cscminivoidraidcrabbase";
            public static readonly string VoidlingPhase1 = "cscminivoidraidcrabphase1";
            public static readonly string VoidlingPhase2 = "cscminivoidraidcrabphase2";
            public static readonly string VoidlingPhase3 = "cscminivoidraidcrabphase3";
            public static readonly string VoidInfestor = "cscvoidinfestor";
            public static readonly string MithrixInfiniteTower = "cscbrotherit";
            public static readonly string ArchWisp = "cscarchwisp";
            public static readonly string BeetleCrystal = "cscbeetlecrystal";
            public static readonly string BeetleGuardCrystal = "cscbeetleguardcrystal";
            public static readonly string MithrixGlass = "cscbrotherglass";
            public static readonly string AncestralPod = "cscparentpod";

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        }

        /// <summary>
        /// Most vanilla <see cref="ClassicStageInfo.monsterDccsPool"/> have (right now) 3 <see cref="DccsPool.Category"/> in them,
        /// their name are inside this class.
        /// </summary>
        public static class MonsterPoolCategories
        {

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
        public static class InteractablePoolCategories
        {

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
        public static class InteractableNames
        {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            public static readonly string BrokenGunnerDrone = "iscbrokendrone1";
            public static readonly string BrokenHealingDrone = "iscbrokendrone2";
            public static readonly string BrokenEmergencyDrone = "iscbrokenemergencydrone";
            public static readonly string BrokenEquipmentDrone = "iscbrokenequipmentdrone";
            public static readonly string BrokenIncineratorDrone = "iscbrokenflamedrone";
            public static readonly string BrokenTC280 = "iscbrokenmegadrone";
            public static readonly string BrokenMissileDrone = "iscbrokenmissiledrone";
            public static readonly string BrokenGunnerTurret = "iscbrokenturret1";
            public static readonly string ScavengersSack = "iscscavbackpack";
            public static readonly string ScavengersLunarSack = "iscscavlunarbackpack";
            public static readonly string Barrel = "iscbarrel1";
            public static readonly string AdaptiveChest = "isccasinochest";
            public static readonly string ChestDamage = "isccategorychestdamage";
            public static readonly string ChestHealing = "isccategorychesthealing";
            public static readonly string ChestUtility = "isccategorychestutility";
            public static readonly string Chest = "iscchest1";
            public static readonly string CloakedChest = "iscchest1stealthed";
            public static readonly string LargeChest = "iscchest2";
            public static readonly string Printer3D = "iscduplicator";
            public static readonly string Printer3DLarge = "iscduplicatorlarge";
            public static readonly string PrinterMiliTech = "iscduplicatormilitary";
            public static readonly string PrinterOvergrown3D = "iscduplicatorwild";
            public static readonly string EquipmentBarrel = "iscequipmentbarrel";
            public static readonly string LegendaryChest = "iscgoldchest";
            public static readonly string LunarPod = "isclunarchest";
            public static readonly string GoldPortal = "iscgoldshoresportal";
            public static readonly string CelestialPortal = "iscmsportal";
            public static readonly string BluePortal = "iscshopportal";
            public static readonly string RadioScanner = "iscradartower";
            public static readonly string Scrapper = "iscscrapper";
            public static readonly string ShrineOfBlood = "iscshrineblood";
            public static readonly string ShrineOfBloodSandy = "iscshrinebloodsandy";
            public static readonly string ShrineOfBloodSnowy = "iscshrinebloodsnowy";
            public static readonly string ShrineOftheMountain = "iscshrineboss";
            public static readonly string ShrineOftheMountainSandy = "iscshrinebosssandy";
            public static readonly string ShrineOftheMountainSnowy = "iscshrinebosssnowy";
            public static readonly string ShrineOfChance = "iscshrinechance";
            public static readonly string ShrineOfChanceSandy = "iscshrinechancesandy";
            public static readonly string ShrineOfChanceSnowy = "iscshrinechancesnowy";
            public static readonly string CleansingPool = "iscshrinecleanse";
            public static readonly string CleansingPoolSandy = "iscshrinecleansesandy";
            public static readonly string CleansingPoolSnowy = "iscshrinecleansesnowy";
            public static readonly string ShrineOfCombat = "iscshrinecombat";
            public static readonly string ShrineOfCombatSandy = "iscshrinecombatsandy";
            public static readonly string ShrineOfCombatSnowy = "iscshrinecombatsnowy";
            public static readonly string AltarOfGold = "iscshrinegoldshoresaccess";
            public static readonly string ShrineOftheWoods = "iscshrinehealing";
            public static readonly string ShrineOfOrder = "iscshrinerestack";
            public static readonly string ShrineOfOrderSandy = "iscshrinerestacksandy";
            public static readonly string ShrineOfOrderSnowy = "iscshrinerestacksnowy";
            public static readonly string PrimordialTeleporter = "isclunarteleporter";
            public static readonly string Teleporter = "iscteleporter";
            public static readonly string TripleShop = "isctripleshop";
            public static readonly string TripleShopEquipment = "isctripleshopequipment";
            public static readonly string TripleShopLarge = "isctripleshoplarge";
            public static readonly string HalcyonBeacon = "iscgoldshoresbeacon";
            public static readonly string VoidRaidSafeWard = "iscvoidraidsafeward";
            public static readonly string InfinitePortal = "iscinfinitetowerportal";
            public static readonly string AssessmentFocus = "iscinfinitetowersafeward";
            public static readonly string AssessmentFocusAwaitingInteraction = "iscinfinitetowersafewardawaitinginteraction";
            public static readonly string LargeChestDamage = "isccategorychest2damage";
            public static readonly string LargeChestHealing = "isccategorychest2healing";
            public static readonly string LargeChestUtility = "isccategorychest2utility";
            public static readonly string DeepVoidPortal = "iscdeepvoidportal";
            public static readonly string DeepVoidSignal = "iscdeepvoidportalbattery";
            public static readonly string VoidPortal = "iscvoidportal";
            public static readonly string VoidSeed = "iscvoidcamp";
            public static readonly string VoidCradle = "iscvoidchest";
            public static readonly string Stalk = "iscvoidcoinbarrel";
            public static readonly string VoidOutroPortal = "iscvoidoutroportal";
            public static readonly string NewtAltar = "iscvoidsuppressor";
            public static readonly string VoidPotential = "iscvoidtriple";
            public static readonly string FreeChestMultiShop = "iscfreechest";
            public static readonly string EncrustedLockbox = "isclockboxvoid";
            public static readonly string GauntletEntranceOrb = "iscgauntletentrance";
            public static readonly string SquidTurretMaster = "iscsquidturret";
            public static readonly string RustyLockbox = "isclockbox";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        }

        /// <summary>
        /// Try applying changes for the current stage (hot swap) for monster and family changes
        /// </summary>
        public static void TryApplyChangesNow()
        {
            DirectorAPI.SetHooks();
            ClassicStageInfo.instance.ApplyChanges();
            ClassicStageInfo.instance.RebuildCards();
        }

        /// <summary>
        /// Enables or disables elite spawns for a specific monster.
        /// </summary>
        /// <param name="monsterName">The name of the monster to edit</param>
        /// <param name="elitesAllowed">Should elites be allowed?</param>
        public static void PreventElites(string? monsterName, bool elitesAllowed)
        {
            DirectorAPI.SetHooks();
            MonsterActions += (dccsPool, mixEnemyArtifactMonsters, currentStage) =>
            {

                if (dccsPool)
                {
                    ForEachPoolEntryInDccsPool(dccsPool, (poolEntry) =>
                    {
                        PreventElitesForPoolEntry(monsterName, elitesAllowed, poolEntry);
                    });
                }

                foreach (var holder in mixEnemyArtifactMonsters)
                {
                    PreventElite(monsterName, elitesAllowed, holder.Card);
                }
            };

            static void PreventElitesForPoolEntry(string? monsterName, bool elitesAllowed, DccsPool.PoolEntry poolEntry)
            {
                foreach (var category in poolEntry.dccs.categories)
                {
                    foreach (var card in category.cards)
                    {
                        PreventElite(monsterName, elitesAllowed, card);
                    }
                }
            }

            static void PreventElite(string? monsterName, bool elitesAllowed, DirectorCard card)
            {
                if (string.Equals(card.spawnCard.name, monsterName, StringComparison.InvariantCultureIgnoreCase))
                {
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
        public static void AddNewMonster(DirectorCard? monsterCard, MonsterCategory monsterCategory)
        {
            DirectorAPI.SetHooks();

            var monsterCardHolder = new DirectorCardHolder
            {
                Card = monsterCard,
                MonsterCategory = monsterCategory
            };

            AddNewMonster(monsterCardHolder, false);
        }

        /// <summary>
        /// Adds a new monster to all stages.
        /// Also add to each existing monster families if second parameter is true.
        /// </summary>
        /// <param name="monsterCard"></param>
        /// <param name="addToFamilies"></param>
        public static void AddNewMonster(DirectorCardHolder? monsterCard, bool addToFamilies)
        {
            DirectorAPI.SetHooks();

            AddNewMonster(monsterCard, addToFamilies, null);
        }

        /// <summary>
        /// Adds a new monster to all stages.
        /// Also add to each existing monster families if second parameter is true.
        /// If a valid predicate is provided the monster will only be added to the given DirectorCardCategorySelection if the predicate return true.
        /// </summary>
        /// <param name="monsterCard"></param>
        /// <param name="addToFamilies"></param>
        /// <param name="predicate"></param>
        public static void AddNewMonster(DirectorCardHolder? monsterCard, bool addToFamilies, Predicate<DirectorCardCategorySelection> predicate)
        {
            DirectorAPI.SetHooks();

            MonsterActions += (dccsPool, mixEnemyArtifactMonsters, currentStage) =>
            {
                AddNewMonster(dccsPool, mixEnemyArtifactMonsters, monsterCard, addToFamilies, predicate);
            };
        }

        /// <summary>
        /// Adds a new monster to a pool of monsters that can spawn in a given stage (see <see cref="MonsterActions"/>).
        /// </summary>
        /// <param name="dccsPool"></param>
        /// <param name="mixEnemyArtifactMonsters"></param>
        /// <param name="monsterCardHolder"></param>
        /// <param name="addToFamilies">Whether to also add to each existing monster family</param>
        public static void AddNewMonster(
            DccsPool dccsPool,
            List<DirectorCardHolder> mixEnemyArtifactMonsters,
            DirectorCardHolder monsterCardHolder,
            bool addToFamilies
        )
        {
            DirectorAPI.SetHooks();

            AddNewMonster(dccsPool, mixEnemyArtifactMonsters, monsterCardHolder, addToFamilies, null);
        }

        /// <summary>
        /// Adds a new monster to a pool of monsters that can spawn in a given stage (see <see cref="MonsterActions"/>).
        /// If a valid (non null) predicate is provided the monster will only be added to the given DirectorCardCategorySelection if the predicate return true.
        /// </summary>
        /// <param name="dccsPool"></param>
        /// <param name="mixEnemyArtifactMonsters"></param>
        /// <param name="monsterCardHolder"></param>
        /// <param name="addToFamilies">Whether to also add to each existing monster family</param>
        /// <param name="predicate"></param>
        public static void AddNewMonster(
            DccsPool dccsPool,
            List<DirectorCardHolder> mixEnemyArtifactMonsters,
            DirectorCardHolder monsterCardHolder,
            bool addToFamilies,
            Predicate<DirectorCardCategorySelection> predicate
        )
        {
            DirectorAPI.SetHooks();

            if (dccsPool)
            {
                ForEachPoolCategoryInDccsPool(dccsPool, (poolCategory) =>
                {
                    var isNotAFamilyCategory = IsNotAFamilyCategory(poolCategory, dccsPool.poolCategories.Length);
                    var isAFamilyCategory = !isNotAFamilyCategory;
                    var isAFamilyCategoryAndShouldAddToIt = addToFamilies && isAFamilyCategory;
                    if (isNotAFamilyCategory || isAFamilyCategoryAndShouldAddToIt)
                    {
                        ForEachElementInPoolEntryArray(SelectPoolEntryArrayGearboxStyle(poolCategory), (poolEntry) =>
                        {
                            AddMonsterToPoolEntry(monsterCardHolder, poolEntry, predicate);
                        });
                    }
                });
            }

            mixEnemyArtifactMonsters?.Add(monsterCardHolder);
        }

        private static bool IsNotAFamilyCategory(DccsPool.Category poolCategory, int poolCategoryCount)
        {
            // As of July 2024, we are considering a "normal" stage a stage that has more than 1 poolCategory and that the category is properly named.
            var isNormalStage = poolCategoryCount > 1 && !string.IsNullOrWhiteSpace(poolCategory.name);
            if (isNormalStage)
            {
                return poolCategory.name == MonsterPoolCategories.Standard;
            }

            return true;
        }

        private static DccsPool.PoolEntry[] SelectPoolEntryArrayGearboxStyle(DccsPool.Category poolCategory)
        {
            // With SoTS Phase 2 update (1.3.7) DCCSBlender has been added, so we want to add cards primarily to alwaysIncluded to avoid card duplication.
            // If alwaysIncluded doesn't have any pool entries then we add cards to includedIfNoConditionsMet, since some Simulacrum DccsPools are like that.
            // And if includedIfNoConditionsMet doesn't have any pool entries then this is most likely a modded stage made before 1.3.7
            // and we just add to includedIfConditionsMet for backwards compatibility.
            if (poolCategory.alwaysIncluded.Length > 0)
            {
                return poolCategory.alwaysIncluded;
            } else if(poolCategory.includedIfNoConditionsMet.Length > 0)
            {
                return poolCategory.includedIfNoConditionsMet;
            } else
            {
                return poolCategory.includedIfConditionsMet;
            }
        }

        private static void AddMonsterToPoolEntry(DirectorCardHolder monsterCardHolder, DccsPool.PoolEntry poolEntry, Predicate<DirectorCardCategorySelection> predicate)
        {
            if ((predicate != null && predicate(poolEntry.dccs)) || predicate == null)
            {
                poolEntry.dccs.AddCard(monsterCardHolder);
            }
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
        public static void AddNewMonsterToStage(DirectorCard? monsterCard, MonsterCategory monsterCategory, Stage stage, string? customStageName = "")
        {
            DirectorAPI.SetHooks();

            var monsterCardHolder = new DirectorCardHolder
            {
                Card = monsterCard,
                MonsterCategory = monsterCategory
            };

            AddNewMonsterToStage(monsterCardHolder, false, stage, customStageName);
        }

        /// <summary>
        /// Adds a new monster to a specific stage.
        /// Also add to each existing monster families if second parameter is true.
        /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
        /// </summary>
        /// <param name="monsterCard"></param>
        /// <param name="addToFamilies"></param>
        /// <param name="stage"></param>
        /// <param name="customStageName"></param>
        public static void AddNewMonsterToStage(DirectorCardHolder monsterCard, bool addToFamilies, Stage stage, string? customStageName = "")
        {
            DirectorAPI.SetHooks();

            AddNewMonsterToStage(monsterCard, addToFamilies, null, stage, customStageName);
        }

        /// <summary>
        /// Adds a new monster to a specific stage.
        /// Also add to each existing monster families if second parameter is true.
        /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
        /// If a valid (non null) predicate is provided the monster will only be added to the given DirectorCardCategorySelection if the predicate return true.
        /// </summary>
        /// <param name="monsterCard"></param>
        /// <param name="addToFamilies"></param>
        /// <param name="predicate"></param>
        /// <param name="stage"></param>
        /// <param name="customStageName"></param>
        public static void AddNewMonsterToStage(DirectorCardHolder monsterCard, bool addToFamilies, Predicate<DirectorCardCategorySelection> predicate, Stage stage, string? customStageName = "")
        {
            DirectorAPI.SetHooks();

            MonsterActions += (dccsPool, mixEnemyArtifactMonsters, currentStage) =>
            {
                if (currentStage.stage == stage)
                {
                    if (currentStage.CheckStage(stage, customStageName))
                    {
                        AddNewMonster(dccsPool, mixEnemyArtifactMonsters, monsterCard, addToFamilies, predicate);
                    }
                }
            };
        }

        /// <summary>
        /// Adds a new monster to matching stages.
        /// Also add to each existing monster families if second parameter is true.
        /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
        /// </summary>
        /// <param name="monsterCard"></param>
        /// <param name="addToFamilies"></param>
        /// <param name="matchStage"></param>
        public static void AddNewMonsterToStagesWhere(DirectorCardHolder monsterCard, bool addToFamilies, Predicate<StageInfo> matchStage)
        {
            DirectorAPI.SetHooks();

            AddNewMonsterToStagesWhere(monsterCard, addToFamilies, matchStage, null);
        }

        /// <summary>
        /// Adds a new monster to matching stages.
        /// Also add to each existing monster families if second parameter is true.
        /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
        /// If a valid (non null) predicate is provided the monster will only be added to the given DirectorCardCategorySelection if the predicate return true.
        /// </summary>
        /// <param name="monsterCard"></param>
        /// <param name="addToFamilies"></param>
        /// <param name="matchStage"></param>
        /// <param name="predicate"></param>
        public static void AddNewMonsterToStagesWhere(DirectorCardHolder monsterCard,
            bool addToFamilies,
            Predicate<StageInfo> matchStage,
            Predicate<DirectorCardCategorySelection> predicate)
        {
            DirectorAPI.SetHooks();
            MonsterActions += (dccsPool, mixEnemyArtifactMonsters, currentStage) =>
            {
                if (matchStage(currentStage))
                {
                    AddNewMonster(dccsPool, mixEnemyArtifactMonsters, monsterCard, addToFamilies, predicate);
                }
            };
        }

        /// <summary>
        /// Adds a new interactable to all stages.
        /// </summary>
        /// <param name="interactableCard">The DirectorCard for the interactable</param>
        /// <param name="interactableCategory">The category of the interactable</param>
        [Obsolete("Use the overload with the DirectorCardHolder instead")]
        public static void AddNewInteractable(DirectorCard? interactableCard, InteractableCategory interactableCategory)
        {
            DirectorAPI.SetHooks();

            var interactableCardHolder = new DirectorCardHolder
            {
                Card = interactableCard,
                InteractableCategory = interactableCategory,
            };

            AddNewInteractable(interactableCardHolder);
        }

        /// <summary>
        /// Adds a new interactable to all stages.
        /// </summary>
        /// <param name="interactableCardHolder"></param>
        public static void AddNewInteractable(DirectorCardHolder? interactableCardHolder)
        {
            DirectorAPI.SetHooks();

            AddNewInteractable(interactableCardHolder, null);
        }

        /// <summary>
        /// Adds a new interactable to all stages.
        /// If a valid (non null) predicate is provided the interactable will only be added to the given DirectorCardCategorySelection if the predicate return true.
        /// </summary>
        /// <param name="interactableCardHolder"></param>
        /// <param name="predicate"></param>
        public static void AddNewInteractable(DirectorCardHolder? interactableCardHolder, Predicate<DirectorCardCategorySelection> predicate)
        {
            DirectorAPI.SetHooks();

            InteractableActions += (interactablesDccsPool, currentStage) =>
            {
                AddNewInteractableToStage(interactablesDccsPool, interactableCardHolder, predicate);
            };
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
        public static void AddNewInteractableToStage(DirectorCard? interactableCard, InteractableCategory interactableCategory, Stage stage, string? customStageName = "")
        {
            DirectorAPI.SetHooks();

            var interactableCardHolder = new DirectorCardHolder
            {
                Card = interactableCard,
                InteractableCategory = interactableCategory
            };

            AddNewInteractableToStage(interactableCardHolder, stage, customStageName);
        }

        /// <summary>
        /// Adds a new interactable to a specific stage.
        /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
        /// </summary>
        /// <param name="interactableCardHolder">The DirectorCardHolder, should have its Card and InteractableCategory members correctly filled</param>
        /// <param name="stage">The stage to add the interactable to</param>
        /// <param name="customStageName">The name of the custom stage</param>
        public static void AddNewInteractableToStage(DirectorCardHolder interactableCardHolder, Stage stage, string customStageName = "")
        {
            DirectorAPI.SetHooks();

            AddNewInteractableToStage(interactableCardHolder, null, stage, customStageName);
        }

        /// <summary>
        /// Adds a new interactable to a specific stage.
        /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
        /// If a valid (non null) predicate is provided the interactable will only be added to the given DirectorCardCategorySelection if the predicate return true.
        /// </summary>
        /// <param name="interactableCardHolder">The DirectorCardHolder, should have its Card and InteractableCategory members correctly filled</param>
        /// <param name="predicate"></param>
        /// <param name="stage">The stage to add the interactable to</param>
        /// <param name="customStageName">The name of the custom stage</param>
        public static void AddNewInteractableToStage(DirectorCardHolder interactableCardHolder,
            Predicate<DirectorCardCategorySelection> predicate,
            Stage stage, string customStageName = "")
        {
            DirectorAPI.SetHooks();

            InteractableActions += (interactablesDccsPool, currentStage) =>
            {
                if (currentStage.stage == stage)
                {
                    if (currentStage.CheckStage(stage, customStageName))
                    {
                        AddNewInteractableToStage(interactablesDccsPool, interactableCardHolder, predicate);
                    }
                }
            };
        }

        private static void AddNewInteractableToStage(
            DccsPool interactablesDccsPool,
            DirectorCardHolder interactableCardHolder,
            Predicate<DirectorCardCategorySelection> predicate)
        {
            if (interactablesDccsPool)
            {
                ForEachPoolCategoryInDccsPool(interactablesDccsPool, (poolCategory) =>
                {
                    ForEachElementInPoolEntryArray(SelectPoolEntryArrayGearboxStyle(poolCategory), (poolEntry) =>
                    {
                        AddInteractableToPoolEntry(interactableCardHolder, poolEntry, predicate);
                    });
                });
            }
        }

        private static void AddInteractableToPoolEntry(DirectorCardHolder interactableCardHolder, DccsPool.PoolEntry poolEntry, Predicate<DirectorCardCategorySelection> predicate)
        {
            if ((predicate != null && predicate(poolEntry.dccs)) || predicate == null)
                poolEntry.dccs.AddCard(interactableCardHolder);
        }

        /// <summary>
        /// Removes a monster from spawns on all stages.
        /// </summary>
        /// <param name="monsterName">The name of the monster card to remove</param>
        public static void RemoveExistingMonster(string? monsterName)
        {
            DirectorAPI.SetHooks();

            RemoveExistingMonster(monsterName, true, null);
        }

        /// <summary>
        /// Removes a monster from spawns on all stages.
        /// If a valid (non null) predicate is provided the monster will only be removed from the given DirectorCardCategorySelection if the predicate return true.
        /// </summary>
        /// <param name="monsterName">The name of the monster card to remove</param>
        /// <param name="removeFromFamilies">Whether or not it the monster should be removed from familiy DCCSs</param>
        /// <param name="predicate">If a valid (non null) predicate is provided the monster will only be removed from the given DirectorCardCategorySelection if the predicate return true.</param>
        public static void RemoveExistingMonster(string? monsterName, bool removeFromFamilies, Predicate<DirectorCardCategorySelection> predicate)
        {
            DirectorAPI.SetHooks();

            StringUtils.ThrowIfStringIsNullOrWhiteSpace(monsterName, nameof(monsterName));
            var monsterNameLowered = monsterName.ToLowerInvariant();

            MonsterActions += (dccsPool, mixEnemyArtifactMonsters, currentStage) =>
            {
                RemoveExistingMonster(dccsPool, mixEnemyArtifactMonsters, monsterNameLowered, removeFromFamilies, predicate);
            };
        }

        private static void RemoveExistingMonster(
            DccsPool dccsPool,
            List<DirectorCardHolder> mixEnemyArtifactMonsters,
            string monsterNameLowered,
            bool removeFromFamilies,
            Predicate<DirectorCardCategorySelection> predicate)
        {
            if (dccsPool)
            {
                ForEachPoolCategoryInDccsPool(dccsPool, (poolCategory) =>
                {
                    var isNotAFamilyCategory = IsNotAFamilyCategory(poolCategory, dccsPool.poolCategories.Length);
                    var isAFamilyCategory = !isNotAFamilyCategory;
                    var isAFamilyCategoryAndShouldRemoveFromIt = removeFromFamilies && isAFamilyCategory;
                    if (isNotAFamilyCategory || isAFamilyCategoryAndShouldRemoveFromIt)
                    {
                        ForEachPoolEntryInDccsPoolCategory(poolCategory, (poolEntry) =>
                        {
                            RemoveMonsterFromPoolEntry(monsterNameLowered, poolEntry, predicate);
                        });
                    }
                });
            }

            mixEnemyArtifactMonsters.RemoveAll((card) => card.Card.spawnCard.name.ToLowerInvariant() == monsterNameLowered);
        }

        private static void RemoveMonsterFromPoolEntry(string monsterNameLowered, DccsPool.PoolEntry poolEntry, Predicate<DirectorCardCategorySelection> predicate)
        {
            if ((predicate != null && predicate(poolEntry.dccs)) || predicate == null)
            {
                for (int i = 0; i < poolEntry.dccs.categories.Length; i++)
                {
                    var cards = poolEntry.dccs.categories[i].cards.ToList();
                    cards.RemoveAll((card) => card.spawnCard.name.ToLowerInvariant() == monsterNameLowered);
                    poolEntry.dccs.categories[i].cards = cards.ToArray();
                }
            }
        }

        /// <summary>
        /// Removes a monster from spawns on a specific stage.
        /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
        /// </summary>
        /// <param name="monsterName">The name of the monster card to remove</param>
        /// <param name="stage">The stage to remove on</param>
        /// <param name="customStageName">The name of the custom stage</param>
        public static void RemoveExistingMonsterFromStage(string? monsterName, Stage stage, string? customStageName = "")
        {
            DirectorAPI.SetHooks();

            RemoveExistingMonsterFromStage(monsterName, false, null, stage, customStageName);
        }

        /// <summary>
        /// Removes a monster from spawns on a specific stage.
        /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
        /// If a valid (non null) predicate is provided the monster will only be removed from the given DirectorCardCategorySelection if the predicate return true.
        /// </summary>
        /// <param name="monsterName">The name of the monster card to remove</param>
        /// <param name="removeFromFamilies">Whether or not it the monster should be removed from family DCCSs</param>
        /// <param name="predicate">If a valid (non null) predicate is provided the monster will only be removed from the given DirectorCardCategorySelection if the predicate return true.</param>
        /// <param name="stage">The stage to remove on</param>
        /// <param name="customStageName">The name of the custom stage</param>
        public static void RemoveExistingMonsterFromStage(string? monsterName,
            bool removeFromFamilies,
            Predicate<DirectorCardCategorySelection> predicate,
            Stage stage, string? customStageName = "")
        {
            DirectorAPI.SetHooks();

            StringUtils.ThrowIfStringIsNullOrWhiteSpace(monsterName, nameof(monsterName));
            var monsterNameLowered = monsterName.ToLowerInvariant();

            MonsterActions += (dccsPool, mixEnemyArtifactMonsters, currentStage) =>
            {
                if (currentStage.stage == stage)
                {
                    if (currentStage.CheckStage(stage, customStageName))
                    {
                        RemoveExistingMonster(dccsPool, mixEnemyArtifactMonsters, monsterNameLowered, removeFromFamilies, predicate);
                    }
                }
            };
        }

        /// <summary>
        /// Remove an interactable from spawns on all stages.
        /// </summary>
        /// <param name="interactableName">Name of the interactable to remove</param>
        public static void RemoveExistingInteractable(string? interactableName)
        {
            DirectorAPI.SetHooks();

            RemoveExistingInteractable(interactableName, null);
        }

        /// <summary>
        /// Remove an interactable from spawns on all stages.
        /// If a valid (non null) predicate is provided the interactable will only be removed from the given DirectorCardCategorySelection if the predicate return true.
        /// </summary>
        /// <param name="interactableName">Name of the interactable to remove</param>
        /// <param name="predicate">If a valid (non null) predicate is provided the interactable will only be removed from the given DirectorCardCategorySelection if the predicate return true.</param>
        public static void RemoveExistingInteractable(string? interactableName, Predicate<DirectorCardCategorySelection> predicate)
        {
            DirectorAPI.SetHooks();

            StringUtils.ThrowIfStringIsNullOrWhiteSpace(interactableName, nameof(interactableName));
            var interactableNameLowered = interactableName.ToLowerInvariant();

            InteractableActions += (interactablesDccsPool, currentStage) =>
            {
                RemoveExistingInteractable(interactablesDccsPool, interactableNameLowered, predicate);
            };
        }

        /// <summary>
        /// Remove an interactable from spawns on a specific stage.
        /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
        /// </summary>
        /// <param name="interactableName">The name of the interactable to remove</param>
        /// <param name="stage">The stage to remove on</param>
        /// <param name="customStageName">The name of the custom stage</param>
        public static void RemoveExistingInteractableFromStage(string? interactableName, Stage stage, string? customStageName = "")
        {
            DirectorAPI.SetHooks();

            RemoveExistingInteractableFromStage(interactableName, null, stage, customStageName);
        }

        /// <summary>
        /// Remove an interactable from spawns on a specific stage.
        /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
        /// If a valid (non null) predicate is provided the interactable will only be removed from the given DirectorCardCategorySelection if the predicate return true.
        /// </summary>
        /// <param name="interactableName">The name of the interactable to remove</param>
        /// <param name="predicate"></param>
        /// <param name="stage">The stage to remove on</param>
        /// <param name="customStageName">The name of the custom stage</param>
        public static void RemoveExistingInteractableFromStage(string? interactableName, Predicate<DirectorCardCategorySelection> predicate, Stage stage, string? customStageName = "")
        {
            DirectorAPI.SetHooks();

            StringUtils.ThrowIfStringIsNullOrWhiteSpace(interactableName, nameof(interactableName));
            var interactableNameLowered = interactableName.ToLowerInvariant();

            InteractableActions += (interactablesDccsPool, currentStage) =>
            {
                if (currentStage.stage == stage)
                {
                    if (currentStage.CheckStage(stage, customStageName))
                    {
                        RemoveExistingInteractable(interactablesDccsPool, interactableNameLowered, predicate);
                    }
                }
            };
        }

        private static void RemoveExistingInteractable(DccsPool interactablesDccsPool, string interactableNameLowered, Predicate<DirectorCardCategorySelection> predicate)
        {
            if (interactablesDccsPool)
            {
                ForEachPoolEntryInDccsPool(interactablesDccsPool, (poolEntry) =>
                {
                    RemoveInteractableFromPoolEntry(interactableNameLowered, poolEntry, predicate);
                });
            }
        }

        private static void RemoveInteractableFromPoolEntry(string interactableNameLowered, DccsPool.PoolEntry poolEntry, Predicate<DirectorCardCategorySelection> predicate)
        {
            if ((predicate != null && predicate(poolEntry.dccs)) || predicate == null)
            {
                for (int i = 0; i < poolEntry.dccs.categories.Length; i++)
                {
                    var cards = poolEntry.dccs.categories[i].cards.ToList();
                    cards.RemoveAll((card) => card.spawnCard.name.ToLowerInvariant() == interactableNameLowered);
                    poolEntry.dccs.categories[i].cards = cards.ToArray();
                }
            }
        }

        /// <summary>
        /// Adds a flat amount of monster credits to the scene director on a specific stage.
        /// For custom stages use Stage.Custom and enter the name of the stage in customStageName.
        /// </summary>
        /// <param name="increase">The quantity to add</param>
        /// <param name="stage">The stage to add on</param>
        /// <param name="customStageName">The name of the custom stage</param>
        public static void AddSceneMonsterCredits(int increase, Stage stage, string? customStageName = "")
        {
            DirectorAPI.SetHooks();
            StageSettingsActions += (settings, currentStage) =>
            {
                if (currentStage.stage == stage)
                {
                    if (currentStage.CheckStage(stage, customStageName))
                    {
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
        public static void AddSceneInteractableCredits(int increase, Stage stage, string? customStageName = "")
        {
            DirectorAPI.SetHooks();
            StageSettingsActions += (settings, currentStage) =>
            {
                if (currentStage.stage == stage)
                {
                    if (currentStage.CheckStage(stage, customStageName))
                    {
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
        public static void MultiplySceneMonsterCredits(int multiplier, Stage stage, string? customStageName = "")
        {
            DirectorAPI.SetHooks();
            StageSettingsActions += (settings, currentStage) =>
            {
                if (currentStage.stage == stage)
                {
                    if (currentStage.CheckStage(stage, customStageName))
                    {
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
        public static void MultiplySceneInteractableCredits(int multiplier, Stage stage, string? customStageName = "")
        {
            DirectorAPI.SetHooks();
            StageSettingsActions += (settings, currentStage) =>
            {
                if (currentStage.stage == stage)
                {
                    if (currentStage.CheckStage(stage, customStageName))
                    {
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
        public static void ReduceSceneMonsterCredits(int divisor, Stage stage, string? customStageName = "")
        {
            DirectorAPI.SetHooks();
            StageSettingsActions += (settings, currentStage) =>
            {
                if (currentStage.stage == stage)
                {
                    if (currentStage.CheckStage(stage, customStageName))
                    {
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
        public static void ReduceSceneInteractableCredits(int divisor, Stage stage, string? customStageName = "")
        {
            DirectorAPI.SetHooks();
            StageSettingsActions += (settings, currentStage) =>
            {
                if (currentStage.stage == stage)
                {
                    if (currentStage.CheckStage(stage, customStageName))
                    {
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
        public static void ForEachPoolEntryInDccsPool(DccsPool dccsPool, Action<DccsPool.PoolEntry> action)
        {
            DirectorAPI.SetHooks();
            ForEachPoolCategoryInDccsPool(dccsPool, (poolCategory) =>
            {
                ForEachPoolEntryInDccsPoolCategory(poolCategory, action);
            });
        }

        /// <summary>
        /// For each <see cref="DccsPool.Category"/> in a <see cref="DccsPool"/>, call the given <see cref="Action"/>.
        /// </summary>
        /// <param name="dccsPool"></param>
        /// <param name="action"></param>
        public static void ForEachPoolCategoryInDccsPool(DccsPool dccsPool, Action<DccsPool.Category> action)
        {
            DirectorAPI.SetHooks();
            foreach (var poolCategory in dccsPool.poolCategories)
            {
                try
                {
                    action(poolCategory);
                }
                catch (Exception e)
                {
                    DirectorPlugin.Logger.LogError(e);
                }
            }
        }

        /// <summary>
        /// For each <see cref="DccsPool.PoolEntry"/> in a <see cref="DccsPool.Category"/>, call the given <see cref="Action"/>.
        /// </summary>
        /// <param name="dccsPoolCategory"></param>
        /// <param name="action"></param>
        public static void ForEachPoolEntryInDccsPoolCategory(DccsPool.Category dccsPoolCategory, Action<DccsPool.PoolEntry> action)
        {
            DirectorAPI.SetHooks();

            ForEachElementInPoolEntryArray(dccsPoolCategory.alwaysIncluded, action);
            ForEachElementInPoolEntryArray(dccsPoolCategory.includedIfNoConditionsMet, action);
            ForEachElementInPoolEntryArray(dccsPoolCategory.includedIfConditionsMet, action);
        }

        private static void ForEachElementInPoolEntryArray(DccsPool.PoolEntry[] poolEntries, Action<DccsPool.PoolEntry> action)
        {
            foreach (var poolEntry in poolEntries)
            {
                try
                {
                    action(poolEntry);
                }
                catch (Exception e)
                {
                    DirectorPlugin.Logger.LogError(e);
                }
            }
        }

        /// <summary>
        /// Returns true if the <see cref="DirectorCardCategorySelection.Category.name"/> is the same as the <see cref="MonsterCategory"/>
        /// </summary>
        /// <param name="category"></param>
        /// <param name="monsterCategory"></param>
        /// <returns></returns>
        public static bool IsSameMonsterCategory(ref DirectorCardCategorySelection.Category category, MonsterCategory monsterCategory)
        {
            DirectorAPI.SetHooks();
            return GetMonsterCategory(category.name) == monsterCategory;
        }

        /// <summary>
        /// Returns the enum value corresponding the given string, returns <see cref="MonsterCategory.Custom"/> if the category is a custom one.
        /// </summary>
        /// <param name="categoryString"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static MonsterCategory GetMonsterCategory(string categoryString)
        {
            DirectorAPI.SetHooks();
            if (string.IsNullOrWhiteSpace(categoryString))
            {
                throw new ArgumentException(nameof(categoryString));
            }

            return categoryString switch
            {
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
        public static string GetVanillaMonsterCategoryName(MonsterCategory monsterCategory)
        {
            DirectorAPI.SetHooks();
            return monsterCategory switch
            {
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
        public static InteractableCategory GetInteractableCategory(string categoryString)
        {
            DirectorAPI.SetHooks();
            if (string.IsNullOrWhiteSpace(categoryString))
            {
                throw new ArgumentException(nameof(categoryString));
            }

            // Vanilla has the same category named differently for some reason (Storm Stuff suffix is shared though)
            if (categoryString.Contains("Storm Stuff"))
            {
                return InteractableCategory.StormStuff;
            }

            return categoryString switch
            {
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
        public static string GetVanillaInteractableCategoryName(InteractableCategory interactableCategory)
        {
            DirectorAPI.SetHooks();
            return interactableCategory switch
            {
                InteractableCategory.Chests => "Chests",
                InteractableCategory.Barrels => "Barrels",
                InteractableCategory.Shrines => "Shrines",
                InteractableCategory.Drones => "Drones",
                InteractableCategory.Misc => "Misc",
                InteractableCategory.Rare => "Rare",
                InteractableCategory.Duplicator => "Duplicator",
                InteractableCategory.VoidStuff => "Void Stuff",
                InteractableCategory.StormStuff => "Storm Stuff",
                _ => throw new ArgumentException(((int)interactableCategory).ToString())
            };
        }

        /// <summary>
        /// Returns true if the <see cref="DirectorCardCategorySelection.Category.name"/> is the same as the <see cref="InteractableCategory"/>
        /// </summary>
        /// <param name="category"></param>
        /// <param name="interactableCategory"></param>
        /// <returns></returns>
        public static bool IsSameInteractableCategory(ref DirectorCardCategorySelection.Category category, InteractableCategory interactableCategory)
        {
            DirectorAPI.SetHooks();
            return GetInteractableCategory(category.name) == interactableCategory;
        }
    }
}
