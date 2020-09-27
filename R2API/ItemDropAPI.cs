using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using BepInEx.Logging;
using R2API.Utils;
using R2API.ItemDropAPITools;
using UnityEngine;
using UnityEngine.Events;

namespace R2API {
    public class PickupSelection {
        public List<PickupIndex> Pickups { get; set; }
        public float DropChance { get; set; } = 1.0f;
        public bool IsDefaults { get; internal set; } = false;
    }

    // TODO: Add loaded checks and throw when not loaded

    public static class DefaultItemDrops {
        public static void AddDefaults() {
            AddDefaultShrineDrops();
            AddChestDefaultDrops();
            AddLunarChestDefaultDrops();
            AddEquipmentChestDefaultDrops();
            AddBossDefaultDrops();
        }

        public static void AddDefaultShrineDrops() {
            var t1 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier1);
            var t2 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier2);
            var t3 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier3);
            var eq = ItemDropAPI.GetDefaultEquipmentDropList();

            var shrineSelections = new[] {
                new List<ItemIndex> {ItemIndex.None}.ToSelection(ItemDropAPI.DefaultShrineFailureWeight),
                t1.ToSelection(ItemDropAPI.DefaultShrineTier1Weight),
                t2.ToSelection(ItemDropAPI.DefaultShrineTier2Weight),
                t3.ToSelection(ItemDropAPI.DefaultShrineTier3Weight),
                eq.ToSelection(ItemDropAPI.DefaultShrineEquipmentWeight)
            };

            foreach (var sel in shrineSelections)
                sel.IsDefaults = true;

            RemoveDefaultDrops(ItemDropLocation.Shrine);
            ItemDropAPI.AddDrops(ItemDropLocation.Shrine, shrineSelections);
        }

        public static void AddChestDefaultDrops() {
            var t1 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier1);
            var t2 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier2);
            var t3 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier3);

            var chestSelections = new[] {
                t1.ToSelection(ItemDropAPI.DefaultSmallChestTier1DropChance),
                t2.ToSelection(ItemDropAPI.DefaultSmallChestTier2DropChance),
                t3.ToSelection(ItemDropAPI.DefaultSmallChestTier3DropChance)
            };

            var lockboxSelections = new[] {
                ItemDropAPI.GetDefaultDropList(ItemTier.Tier1).ToSelection(ItemDropAPI.DefaultSmallChestTier1DropChance),
                ItemDropAPI.GetDefaultDropList(ItemTier.Tier2).ToSelection(ItemDropAPI.DefaultSmallChestTier2DropChance),
                ItemDropAPI.GetDefaultDropList(ItemTier.Tier3).ToSelection(ItemDropAPI.DefaultSmallChestTier3DropChance)
            };

            var utilitySelections = new[] {
                ItemDropAPI.GetDefaultDropList(ItemTier.Tier1, ItemTag.Utility).ToSelection(ItemDropAPI.DefaultSmallChestTier1DropChance),
                ItemDropAPI.GetDefaultDropList(ItemTier.Tier2, ItemTag.Utility).ToSelection(ItemDropAPI.DefaultSmallChestTier2DropChance),
                ItemDropAPI.GetDefaultDropList(ItemTier.Tier3, ItemTag.Utility).ToSelection(ItemDropAPI.DefaultSmallChestTier3DropChance),
            };

            var damageSelections = new[] {
                ItemDropAPI.GetDefaultDropList(ItemTier.Tier1, ItemTag.Damage).ToSelection(ItemDropAPI.DefaultSmallChestTier1DropChance),
                ItemDropAPI.GetDefaultDropList(ItemTier.Tier2, ItemTag.Damage).ToSelection(ItemDropAPI.DefaultSmallChestTier2DropChance),
                ItemDropAPI.GetDefaultDropList(ItemTier.Tier3, ItemTag.Damage).ToSelection(ItemDropAPI.DefaultSmallChestTier3DropChance),
            };

            var healingSelections = new[] {
                ItemDropAPI.GetDefaultDropList(ItemTier.Tier1, ItemTag.Healing).ToSelection(ItemDropAPI.DefaultSmallChestTier1DropChance),
                ItemDropAPI.GetDefaultDropList(ItemTier.Tier2, ItemTag.Healing).ToSelection(ItemDropAPI.DefaultSmallChestTier2DropChance),
                ItemDropAPI.GetDefaultDropList(ItemTier.Tier3, ItemTag.Healing).ToSelection(ItemDropAPI.DefaultSmallChestTier3DropChance),
            };

            var scavSelections = new[] {
                ItemDropAPI.GetDefaultDropList(ItemTier.Tier1).ToSelection(ItemDropAPI.DefaultScavBackpackTier1DropChance),
                ItemDropAPI.GetDefaultDropList(ItemTier.Tier2).ToSelection(ItemDropAPI.DefaultScavBackpackTier2DropChance),
                ItemDropAPI.GetDefaultDropList(ItemTier.Tier3).ToSelection(ItemDropAPI.DefaultScavBackpackTier3DropChance),
                ItemDropAPI.GetDefaultDropList(ItemTier.Lunar).ToSelection(ItemDropAPI.DefaultScavBackpackLunarDropChance),
            };

            var allSelections = new[] {chestSelections, lockboxSelections, utilitySelections, damageSelections, healingSelections, scavSelections};
            foreach (var selGroup in allSelections)
                foreach (var sel in selGroup)
                    sel.IsDefaults = true;

            var allLocations = new[] {ItemDropLocation.UtilityChest, ItemDropLocation.DamageChest, ItemDropLocation.HealingChest,
                ItemDropLocation.Lockbox, ItemDropLocation.SmallChest, ItemDropLocation.MediumChest, ItemDropLocation.LargeChest, ItemDropLocation.ScavBackPack};

            foreach (var selLoc in allLocations)
                RemoveDefaultDrops(selLoc);

            ItemDropAPI.AddDrops(ItemDropLocation.UtilityChest, utilitySelections);
            ItemDropAPI.AddDrops(ItemDropLocation.DamageChest, damageSelections);
            ItemDropAPI.AddDrops(ItemDropLocation.HealingChest, healingSelections);

            ItemDropAPI.AddDrops(ItemDropLocation.Lockbox, lockboxSelections);
            ItemDropAPI.AddDrops(ItemDropLocation.SmallChest, chestSelections);
            ItemDropAPI.AddDrops(ItemDropLocation.MediumChest, t2.ToSelection(ItemDropAPI.DefaultMediumChestTier2DropChance), t3.ToSelection(ItemDropAPI.DefaultMediumChestTier3DropChance));
            ItemDropAPI.AddDrops(ItemDropLocation.LargeChest, t3.ToSelection(ItemDropAPI.DefaultLargeChestTier3DropChance));
            ItemDropAPI.AddDrops(ItemDropLocation.ScavBackPack, scavSelections);
        }

        public static void AddEquipmentChestDefaultDrops() {
            var eq = ItemDropAPI.GetDefaultEquipmentDropList();

            var equipmentSelections = eq.ToSelection();
            equipmentSelections.IsDefaults = true;

            RemoveDefaultDrops(ItemDropLocation.EquipmentChest);
            ItemDropAPI.AddDrops(ItemDropLocation.EquipmentChest, eq.ToSelection());
        }

        public static void AddLunarChestDefaultDrops() {
            var lun = ItemDropAPI.GetDefaultLunarDropList();

            var lunarSelections = lun.ToSelection();
            lunarSelections.IsDefaults = true;

            RemoveDefaultDrops(ItemDropLocation.LunarChest);
            ItemDropAPI.AddDrops(ItemDropLocation.LunarChest, lunarSelections);
        }

        public static void AddBossDefaultDrops() {
            ItemDropAPI.IncludeSpecialBossDrops = true;

            var t2 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier2);

            var t2selections = t2.ToSelection();
            t2selections.IsDefaults = true;

            RemoveDefaultDrops(ItemDropLocation.Boss);
            ItemDropAPI.AddDrops(ItemDropLocation.Boss, t2selections);
        }

        private static void RemoveDefaultDrops(ItemDropLocation location) {
            if (ItemDropAPI.Selection.ContainsKey(location))
                ItemDropAPI.RemoveDrops(location,
                    ItemDropAPI.Selection[location]
                    .Where(sel => sel.IsDefaults)
                    .ToArray());
        }
    }

    public enum ItemDropLocation {
        //Mobs,
        Boss,
        EquipmentChest,
        LunarChest,
        SmallChest,
        MediumChest,
        LargeChest,
        Lockbox,
        Shrine,
        UtilityChest,
        HealingChest,
        DamageChest,
        ScavBackPack
        //SmallChestSelector,
        //MediumChestSelector,
        //LargeChestSelector
    }

    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class ItemDropAPI {
        public static ManualLogSource Logger => R2API.Logger;

        public static int? BossDropParticipatingPlayerCount = null;
        public static bool IncludeSpecialBossDrops = true;

        public static float DefaultSmallChestTier1DropChance = 0.8f;
        public static float DefaultSmallChestTier2DropChance = 0.2f;
        public static float DefaultSmallChestTier3DropChance = 0.01f;

        public static float DefaultMediumChestTier2DropChance = 0.8f;
        public static float DefaultMediumChestTier3DropChance = 0.2f;

        public static float DefaultLargeChestTier3DropChance = 1.0f;

        public static float DefaultShrineEquipmentWeight = 2f;
        public static float DefaultShrineFailureWeight = 10.1f;
        public static float DefaultShrineTier1Weight = 8f;
        public static float DefaultShrineTier2Weight = 2f;
        public static float DefaultShrineTier3Weight = 0.2f;

        public static float DefaultSmallChestTier1SelectorDropChance = 0.8f;
        public static float DefaultSmallChestTier2SelectorDropChance = 0.2f;
        public static float DefaultSmallChestTier3SelectorDropChance = 0.01f;

        public static float DefaultMediumChestTier1SelectorDropChance = 0.8f;
        public static float DefaultMediumChestTier2SelectorDropChance = 0.2f;

        public static float DefaultScavBackpackTier1DropChance = 0.8f;
        public static float DefaultScavBackpackTier2DropChance = 0.2f;
        public static float DefaultScavBackpackTier3DropChance = 0.01f;
        public static float DefaultScavBackpackLunarDropChance = 0f;

        static public DropList playerDropList = new DropList();
        static public InteractableCalculator playerInteractables = new InteractableCalculator();

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }
        private static bool _loaded;


        public static bool DefaultDrops { get; set; } = true;

        public static Dictionary<ItemDropLocation, List<PickupSelection>> Selection { get; set; } =
            new Dictionary<ItemDropLocation, List<PickupSelection>>();

        private static readonly Dictionary<ItemTier, HashSet<ItemIndex>> AdditionalTierItems = new Dictionary<ItemTier, HashSet<ItemIndex>> {
            { ItemTier.Tier1, new HashSet<ItemIndex>() },
            { ItemTier.Tier2, new HashSet<ItemIndex>() },
            { ItemTier.Tier3, new HashSet<ItemIndex>() },
            { ItemTier.Boss, new HashSet<ItemIndex>() },
            { ItemTier.Lunar, new HashSet<ItemIndex>() }
        };

        private static readonly HashSet<EquipmentIndex> AdditionalEquipment = new HashSet<EquipmentIndex>();

        private static Dictionary<ItemTier, List<PickupIndex>> _itemsToAdd = new Dictionary<ItemTier, List<PickupIndex>>() {
            { ItemTier.Tier1, new List<PickupIndex>() },
            { ItemTier.Tier2, new List<PickupIndex>() },
            { ItemTier.Tier3, new List<PickupIndex>() },
            { ItemTier.Boss, new List<PickupIndex>() },
            { ItemTier.Lunar, new List<PickupIndex>() },
            { ItemTier.NoTier, new List<PickupIndex>() },
        };

        private static Dictionary<ItemTier, List<PickupIndex>> _itemsToRemove = new Dictionary<ItemTier, List<PickupIndex>>() {
            { ItemTier.Tier1, new List<PickupIndex>() },
            { ItemTier.Tier2, new List<PickupIndex>() },
            { ItemTier.Tier3, new List<PickupIndex>() },
            { ItemTier.Boss, new List<PickupIndex>() },
            { ItemTier.Lunar, new List<PickupIndex>() },
            { ItemTier.NoTier, new List<PickupIndex>() },
        };

        private static Dictionary<ItemTier, List<PickupIndex>> _equipmentToAdd = new Dictionary<ItemTier, List<PickupIndex>>() {
            { ItemTier.Tier1, new List<PickupIndex>() },
            { ItemTier.Lunar, new List<PickupIndex>() },
            { ItemTier.NoTier, new List<PickupIndex>() },
        };

        private static Dictionary<ItemTier, List<PickupIndex>> _equipmentToRemove = new Dictionary<ItemTier, List<PickupIndex>>() {
            { ItemTier.Tier1, new List<PickupIndex>() },
            { ItemTier.Lunar, new List<PickupIndex>() },
            { ItemTier.NoTier, new List<PickupIndex>() },
        };

        static public Dictionary<ItemTier, List<PickupIndex>> itemsToAdd {
            get { return _itemsToAdd; }
            private set { _itemsToAdd = value; }
        }

        static public Dictionary<ItemTier, List<PickupIndex>> itemsToRemove {
            get { return _itemsToRemove; }
            private set { _itemsToRemove = value; }
        }

        static public Dictionary<ItemTier, List<PickupIndex>> equipmentToAdd {
            get { return _equipmentToAdd; }
            private set { _equipmentToAdd = value; }
        }

        static public Dictionary<ItemTier, List<PickupIndex>> equipmentToRemove {
            get { return _equipmentToRemove; }
            private set { _equipmentToRemove = value; }
        }

        // THIS MODULE MUST ONLY BE INITIALIZED AFTER CUSTOM ITEMS HAVE BEEN ADDED
        // IF MONSTERDROPAPI IS GOING TO BE INITIALIZED IT MUST BE INITIALIZED BEFORE THIS MODULE IS
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.Run.BuildDropTable += RunOnBuildDropTable;
            On.RoR2.SceneDirector.PopulateScene += PopulateScene;
            On.RoR2.ShopTerminalBehavior.GenerateNewPickupServer += GenerateNewPickupServer;
            On.RoR2.DirectorCore.TrySpawnObject += TrySpawnObject;
            On.RoR2.ShrineChanceBehavior.AddShrineStack += AddShrineStack;
            On.RoR2.BossGroup.DropRewards += DropRewards;
            IL.RoR2.BossGroup.DropRewards += DropRewards;
            On.RoR2.PickupPickerController.SetOptionsServer += SetOptionsServer;
            On.RoR2.ArenaMissionController.EndRound += EndRound;
            On.RoR2.GlobalEventManager.OnCharacterDeath += OnCharacterDeath;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.Run.BuildDropTable -= RunOnBuildDropTable;
            On.RoR2.SceneDirector.PopulateScene -= PopulateScene;
            On.RoR2.ShopTerminalBehavior.GenerateNewPickupServer -= GenerateNewPickupServer;
            On.RoR2.DirectorCore.TrySpawnObject -= TrySpawnObject;
            On.RoR2.ShrineChanceBehavior.AddShrineStack -= AddShrineStack;
            On.RoR2.BossGroup.DropRewards -= DropRewards;
            IL.RoR2.BossGroup.DropRewards -= DropRewards;
            On.RoR2.PickupPickerController.SetOptionsServer -= SetOptionsServer;
            On.RoR2.ArenaMissionController.EndRound -= EndRound;
            On.RoR2.GlobalEventManager.OnCharacterDeath -= OnCharacterDeath;
        }


        private static void RunOnBuildDropTable(On.RoR2.Run.orig_BuildDropTable orig, Run run) {
            Catalogue.PopulateItemCatalogues();
            orig(run);
            playerDropList.DuplicateDropLists(run);
            playerDropList.ClearAllLists(run);
            playerDropList.GenerateItems(AdditionalTierItems, AdditionalEquipment, itemsToAdd, itemsToRemove, equipmentToAdd, equipmentToRemove);
            playerDropList.SetItems(run);
            playerInteractables.CalculateInvalidInteractables(playerDropList);
        }

        static private void PopulateScene(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector sceneDirector) {
            RoR2.InteractableSpawnCard[] allInteractables = UnityEngine.Resources.LoadAll<RoR2.InteractableSpawnCard>("SpawnCards/InteractableSpawnCard");
            foreach (RoR2.InteractableSpawnCard spawnCard in allInteractables) {
                string interactableName = InteractableCalculator.GetSpawnCardName(spawnCard);
                if (interactableName == "Lockbox" || interactableName == "ScavBackpack") {
                    DropOdds.UpdateChestTierOdds(spawnCard, interactableName);
                } else if (interactableName == "CasinoChest") {
                    DropOdds.UpdateDropTableTierOdds(spawnCard, interactableName);
                } else if (interactableName == "ShrineCleanse") {
                    ExplicitPickupDropTable dropTable = spawnCard.prefab.GetComponent<RoR2.ShopTerminalBehavior>().dropTable as ExplicitPickupDropTable;
                    DropOdds.UpdateDropTableItemOdds(playerDropList, dropTable, interactableName);
                }
            }

            if (ClassicStageInfo.instance != null) {
                int categoriesLength = ClassicStageInfo.instance.interactableCategories.categories.Length;
                for (int categoryIndex = 0; categoryIndex < categoriesLength; categoryIndex++) {
                    List<DirectorCard> directorCards = new List<DirectorCard>();
                    foreach (DirectorCard directorCard in ClassicStageInfo.instance.interactableCategories.categories[categoryIndex].cards) {
                        string interactableName = InteractableCalculator.GetSpawnCardName(directorCard.spawnCard);
                        if (new List<string>() { }.Contains(interactableName)) {
                        }
                        if (playerInteractables.interactablesInvalid.Contains(interactableName)) {
                        } else {
                            DropOdds.UpdateChestTierOdds(directorCard.spawnCard, interactableName);
                            DropOdds.UpdateShrineTierOdds(directorCard, interactableName);
                            directorCards.Add(directorCard);
                        }
                    }
                    DirectorCard[] directorCardArray = new DirectorCard[directorCards.Count];
                    for (int cardIndex = 0; cardIndex < directorCards.Count; cardIndex++) {
                        directorCardArray[cardIndex] = directorCards[cardIndex];
                    }
                    if (directorCardArray.Length == 0) {
                        ClassicStageInfo.instance.interactableCategories.categories[categoryIndex].selectionWeight = 0;
                    }
                    ClassicStageInfo.instance.interactableCategories.categories[categoryIndex].cards = directorCardArray;
                }
            }
            orig(sceneDirector);
        }

        static private void GenerateNewPickupServer(On.RoR2.ShopTerminalBehavior.orig_GenerateNewPickupServer orig, ShopTerminalBehavior shopTerminalBehavior) {
            List<PickupIndex> shopList = new List<PickupIndex>();
            if (shopTerminalBehavior.itemTier == ItemTier.Tier1) {
                shopList = Run.instance.availableTier1DropList;
            } else if (shopTerminalBehavior.itemTier == ItemTier.Tier2) {
                shopList = Run.instance.availableTier2DropList;
            } else if (shopTerminalBehavior.itemTier == ItemTier.Tier3) {
                shopList = Run.instance.availableTier3DropList;
            } else if (shopTerminalBehavior.itemTier == ItemTier.Boss) {
                shopList = Run.instance.availableBossDropList;
            } else if (shopTerminalBehavior.itemTier == ItemTier.Lunar) {
                shopList = Run.instance.availableLunarDropList;
            }
            if (shopList.Count > 0) {
                orig(shopTerminalBehavior);
            } else {
                shopTerminalBehavior.SetNoPickup();
                RoR2.PurchaseInteraction purchaseInteraction = shopTerminalBehavior.GetComponent<RoR2.PurchaseInteraction>();
                if (purchaseInteraction != null) {
                    purchaseInteraction.SetAvailable(false);
                }
            }
        }

        static private GameObject TrySpawnObject(On.RoR2.DirectorCore.orig_TrySpawnObject orig, DirectorCore directorCore, DirectorSpawnRequest directorSpawnRequest) {
            if (directorSpawnRequest.spawnCard.name == "iscScavBackpack") {
                if (playerInteractables.interactablesInvalid.Contains("ScavBackpack")) {
                    return null;
                }
            }
            return orig(directorCore, directorSpawnRequest);
        }

        static private void AddShrineStack(On.RoR2.ShrineChanceBehavior.orig_AddShrineStack orig, ShrineChanceBehavior shrineChangeBehavior, Interactor interactor) {
            List<PickupIndex> tier1Adjusted = playerDropList.availableTier1DropList;
            if (tier1Adjusted.Count == 0) {
                tier1Adjusted = DropList.tier1DropListOriginal;
            }
            List<PickupIndex> tier2Adjusted = playerDropList.availableTier2DropList;
            if (tier2Adjusted.Count == 0) {
                tier2Adjusted = DropList.tier2DropListOriginal;
            }
            List<PickupIndex> tier3Adjusted = playerDropList.availableTier3DropList;
            if (tier3Adjusted.Count == 0) {
                tier3Adjusted = DropList.tier3DropListOriginal;
            }
            List<PickupIndex> equipmentAdjusted = playerDropList.availableEquipmentDropList;
            if (equipmentAdjusted.Count == 0) {
                equipmentAdjusted = DropList.equipmentDropListOriginal;
            }

            DropList.SetDropLists(tier1Adjusted, tier2Adjusted, tier3Adjusted, equipmentAdjusted);
            orig(shrineChangeBehavior, interactor);
            DropList.RevertDropLists();
        }

        static private void DropRewards(On.RoR2.BossGroup.orig_DropRewards orig, BossGroup bossGroup) {
            System.Reflection.FieldInfo info = typeof(BossGroup).GetField("bossDrops", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ICollection collection = info.GetValue(bossGroup) as ICollection;
            List<PickupIndex> bossDrops = new List<PickupIndex>();
            List<PickupIndex> bossDropsAdjusted = new List<PickupIndex>();
            foreach (object bossDrop in collection) {
                PickupIndex pickupIndex = (PickupIndex)bossDrop;
                bossDrops.Add(pickupIndex);
                if (PickupCatalog.GetPickupDef(pickupIndex).itemIndex != ItemIndex.None && playerDropList.availableBossDropList.Contains(pickupIndex)) {
                    bossDropsAdjusted.Add(pickupIndex);
                }
            }
            int normalCount = Run.instance.availableTier2DropList.Count;
            if (bossGroup.forceTier3Reward) {
                normalCount = Run.instance.availableTier3DropList.Count;
            }
            if (normalCount != 0 || bossDropsAdjusted.Count != 0) {
                float bossDropChanceOld = bossGroup.bossDropChance;
                if (normalCount == 0) {
                    DropList.SetDropLists(new List<PickupIndex>(), new List<PickupIndex>(), new List<PickupIndex>(), new List<PickupIndex>());
                    bossGroup.bossDropChance = 1;
                } else if (bossDropsAdjusted.Count == 0) {
                    bossGroup.bossDropChance = 0;
                }
                info.SetValue(bossGroup, bossDropsAdjusted);
                orig(bossGroup);
                info.SetValue(bossGroup, bossDrops);
                bossGroup.bossDropChance = bossDropChanceOld;
                if (normalCount == 0) {
                    DropList.RevertDropLists();
                }
            }
        }

        static private void SetOptionsServer(On.RoR2.PickupPickerController.orig_SetOptionsServer orig, RoR2.PickupPickerController pickupPickerController, RoR2.PickupPickerController.Option[] options) {
            List<RoR2.PickupPickerController.Option> optionsAdjusted = new List<PickupPickerController.Option>();
            foreach (RoR2.PickupPickerController.Option option in options) {
                if (pickupPickerController.contextString.Contains("SCRAPPER")) {
                    if (playerDropList.availableSpecialItems.Contains(PickupCatalog.FindPickupIndex(Catalogue.GetScrapIndex(ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(option.pickupIndex).itemIndex).tier)))) {
                        optionsAdjusted.Add(option);
                    }
                } else {
                    optionsAdjusted.Add(option);
                }
            }
            if (pickupPickerController.contextString.Contains("COMMAND_CUBE")) {
                if (options.Length > 0) {
                    ItemIndex itemIndex = PickupCatalog.GetPickupDef(options[0].pickupIndex).itemIndex;
                    ItemTier itemTier = ItemTier.NoTier;
                    if (itemIndex != ItemIndex.None) {
                        itemTier = ItemCatalog.GetItemDef(itemIndex).tier;
                    }
                    List<PickupIndex> tierList = playerDropList.GetDropList(itemTier);
                    optionsAdjusted.Clear();
                    foreach (PickupIndex pickupIndex in tierList) {
                        PickupPickerController.Option newOption = new PickupPickerController.Option();
                        newOption.available = true;
                        newOption.pickupIndex = pickupIndex;
                        optionsAdjusted.Add(newOption);
                    }
                }
            }
            options = new RoR2.PickupPickerController.Option[optionsAdjusted.Count];
            for (int optionIndex = 0; optionIndex < optionsAdjusted.Count; optionIndex++) {
                options[optionIndex] = optionsAdjusted[optionIndex];
            }
            orig(pickupPickerController, options);
        }

        static private void EndRound(On.RoR2.ArenaMissionController.orig_EndRound orig, RoR2.ArenaMissionController arenaMissionController) {
            List<PickupIndex> list = Run.instance.availableTier1DropList;
            if (arenaMissionController.currentRound > 4) {
                list = Run.instance.availableTier2DropList;
            }
            if (arenaMissionController.currentRound == arenaMissionController.totalRoundsMax) {
                list = Run.instance.availableTier3DropList;
            }
            if (list.Count == 0) {
                GameObject rewardSpawnPositionOld = arenaMissionController.rewardSpawnPosition;
                arenaMissionController.rewardSpawnPosition = null;
                orig(arenaMissionController);
                arenaMissionController.rewardSpawnPosition = rewardSpawnPositionOld;
            } else {
                orig(arenaMissionController);
            }
        }

        static private void OnCharacterDeath(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, RoR2.GlobalEventManager globalEventManager, RoR2.DamageReport damageReport) {
            TeamIndex teamIndex = TeamIndex.None;
            if (damageReport.victimBody.teamComponent != null) {
                teamIndex = damageReport.victimBody.teamComponent.teamIndex;
            }
            System.Reflection.PropertyInfo propertyInfo = null;
            if (teamIndex == TeamIndex.Monster) {
                if (damageReport.victimBody.isElite) {
                    if (damageReport.victimBody.equipmentSlot != null) {
                        if (!playerDropList.availableEliteEquipment.Contains(PickupCatalog.FindPickupIndex(damageReport.victimBody.equipmentSlot.equipmentIndex))) {
                            propertyInfo = typeof(RoR2.CharacterBody).GetProperty("isElite", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            propertyInfo.SetValue(damageReport.victimBody, false);
                        }
                    }
                }
            }
            orig(globalEventManager, damageReport);
            if (propertyInfo != null) {
                propertyInfo.SetValue(damageReport.victimBody, true);
            }
        }


        private static void DropRewards(ILContext il) {
            var cursor = new ILCursor(il).Goto(0);

            cursor.GotoNext(MoveType.After, x => x.MatchCallvirt(typeof(Run).GetMethod("get_participatingPlayerCount")));
            cursor.Index += 1;

            cursor.EmitDelegate<Func<int>>(() => BossDropParticipatingPlayerCount ?? Run.instance.participatingPlayerCount);

            cursor.Emit(OpCodes.Stloc_0);

            var nextElementUniform = typeof(Xoroshiro128Plus)
                .GetMethodWithConstructedGenericParameter("NextElementUniform", typeof(List<>))
                .MakeGenericMethod(typeof(PickupIndex));
            cursor.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(nextElementUniform));
            var pickupIndex = Reflection.ReadLocalIndex(cursor.Next.OpCode, cursor.Next.Operand);

            cursor.Goto(0);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Dup);
            cursor.Emit(OpCodes.Ldfld, typeof(BossGroup).GetFieldCached("rng"));
            cursor.EmitDelegate<Func<BossGroup, Xoroshiro128Plus, PickupIndex>>((self, rng) => {
                var norm = rng.nextNormalizedFloat;

                if (self.forceTier3Reward) {
                    var t3List = GetDefaultDropList(ItemTier.Tier3);
                    var selection = t3List.ToSelection();
                    return GetSelection(new List<PickupSelection> { selection }, norm);
                } else {
                    return GetSelection(ItemDropLocation.Boss, norm);
                }
            });

            cursor.Emit(OpCodes.Stloc_S, pickupIndex);
        }

        public static void AddItemByTier(ItemTier itemTier, ItemIndex itemIndex) {
            if (itemsToAdd.ContainsKey(itemTier)) {
                if (!itemsToAdd[itemTier].Contains(PickupCatalog.FindPickupIndex(itemIndex))) {
                    itemsToAdd[itemTier].Add(PickupCatalog.FindPickupIndex(itemIndex));
                }
            }
        }

        public static void UnaddItemByTier(ItemTier itemTier, ItemIndex itemIndex) {
            if (itemsToAdd.ContainsKey(itemTier)) {
                if (itemsToAdd[itemTier].Contains(PickupCatalog.FindPickupIndex(itemIndex))) {
                    itemsToAdd[itemTier].Remove(PickupCatalog.FindPickupIndex(itemIndex));
                }
            }
        }

        public static void RemoveItemByTier(ItemTier itemTier, ItemIndex itemIndex) {
            if (itemsToRemove.ContainsKey(itemTier)) {
                if (!itemsToRemove[itemTier].Contains(PickupCatalog.FindPickupIndex(itemIndex))) {
                    itemsToRemove[itemTier].Add(PickupCatalog.FindPickupIndex(itemIndex));
                }
            }
        }

        public static void UnremoveItemByTier(ItemTier itemTier, ItemIndex itemIndex) {
            if (itemsToRemove.ContainsKey(itemTier)) {
                if (itemsToRemove[itemTier].Contains(PickupCatalog.FindPickupIndex(itemIndex))) {
                    itemsToRemove[itemTier].Remove(PickupCatalog.FindPickupIndex(itemIndex));
                }
            }
        }

        public static void AddEquipmentByTier(ItemTier itemTier, EquipmentIndex equipmentIndex) {
            if (equipmentToAdd.ContainsKey(itemTier)) {
                if (!equipmentToAdd[itemTier].Contains(PickupCatalog.FindPickupIndex(equipmentIndex))) {
                    equipmentToAdd[itemTier].Add(PickupCatalog.FindPickupIndex(equipmentIndex));
                }
            }
        }

        public static void UnaddEquipmentByTier(ItemTier itemTier, EquipmentIndex equipmentIndex) {
            if (equipmentToAdd.ContainsKey(itemTier)) {
                if (equipmentToAdd[itemTier].Contains(PickupCatalog.FindPickupIndex(equipmentIndex))) {
                    equipmentToAdd[itemTier].Remove(PickupCatalog.FindPickupIndex(equipmentIndex));
                }
            }
        }

        public static void RemoveEquipmentByTier(ItemTier itemTier, EquipmentIndex equipmentIndex) {
            if (equipmentToRemove.ContainsKey(itemTier)) {
                if (!equipmentToRemove[itemTier].Contains(PickupCatalog.FindPickupIndex(equipmentIndex))) {
                    equipmentToRemove[itemTier].Add(PickupCatalog.FindPickupIndex(equipmentIndex));
                }
            }
        }

        public static void UnremoveEquipmentByTier(ItemTier itemTier, EquipmentIndex equipmentIndex) {
            if (equipmentToRemove.ContainsKey(itemTier)) {
                if (equipmentToRemove[itemTier].Contains(PickupCatalog.FindPickupIndex(equipmentIndex))) {
                    equipmentToRemove[itemTier].Remove(PickupCatalog.FindPickupIndex(equipmentIndex));
                }
            }
        }




        public static void AddDrops(ItemDropLocation dropLocation, params PickupSelection[] pickups) {
            if (!Selection.ContainsKey(dropLocation)) {
                Selection[dropLocation] = new List<PickupSelection>();
            }
            Selection[dropLocation].AddRange(pickups);
        }

        public static void RemoveDrops(ItemDropLocation dropLocation, params PickupSelection[] pickups) {
            if (!Selection.ContainsKey(dropLocation))
                return;

            foreach (var pickup in pickups)
                Selection[dropLocation].Remove(pickup);
        }

        public static void AddToDefaultByTier(ItemTier itemTier, params ItemIndex[] items) {
            if (itemTier == ItemTier.NoTier) {
                return;
            }

            AdditionalTierItems[itemTier].UnionWith(items);
        }
        
        public static void RemoveFromDefaultByTier(ItemTier itemTier, params ItemIndex[] items) {
            if (itemTier == ItemTier.NoTier) {
                return;
            }

            AdditionalTierItems[itemTier].ExceptWith(items);
        }

        public static void AddToDefaultByTier(params KeyValuePair<ItemIndex, ItemTier>[] items) {
            foreach (var list in AdditionalTierItems)
                AddToDefaultByTier(list.Key,
                    items.Where(item => list.Key == item.Value)
                    .Select(item => item.Key)
                    .ToArray());
        }

        public static void RemoveFromDefaultByTier(params KeyValuePair<ItemIndex, ItemTier>[] items) {
            foreach (var list in AdditionalTierItems)
                RemoveFromDefaultByTier(list.Key,
                    items.Where(item => list.Key == item.Value)
                    .Select(item => item.Key)
                    .ToArray());
        }



        public static void AddToDefaultEquipment(params EquipmentIndex[] equipment) {
            AdditionalEquipment.UnionWith(equipment);
        }

        public static void RemoveFromDefaultEquipment(params EquipmentIndex[] equipments) {
            AdditionalEquipment.ExceptWith(equipments);
        }

        public static void ReplaceDrops(ItemDropLocation dropLocation,
            params PickupSelection[] pickupSelections) {
            Logger.LogInfo(
                $"Adding drop information for {dropLocation}: {pickupSelections.Sum(x => x.Pickups.Count)} items");

            Selection[dropLocation] = pickupSelections.ToList();
        }

        public static void ReplaceDrops(ItemDropLocation dropLocation, List<PickupSelection> pickupSelections) {
            Logger.LogInfo(
                $"Adding drop information for {dropLocation}: {pickupSelections.Sum(x => x.Pickups.Count)} items");

            Selection[dropLocation] = pickupSelections;
        }

        public static PickupIndex GetSelection(ItemDropLocation dropLocation, float normalizedIndex) {
            if (!Selection.ContainsKey(dropLocation))
                return PickupCatalog.FindPickupIndex(ItemIndex.None);

            return GetSelection(Selection[dropLocation], normalizedIndex);
        }

        public static PickupIndex GetSelection(List<PickupSelection> selections, float normalizedIndex) {
            var weightedSelection = new WeightedSelection<PickupIndex>();
            foreach (var selection in selections.Where(x => x != null))
                foreach (var pickup in selection.Pickups)
                    weightedSelection.AddChoice(pickup, selection.DropChance / selection.Pickups.Count);

            return weightedSelection.Evaluate(normalizedIndex);
        }

        public static List<ItemIndex> GetDefaultDropList(ItemTier itemTier) {
            if (itemTier == ItemTier.NoTier) {
                return null;
            }

            var list = new List<ItemIndex>();

            for (var itemIndex = ItemIndex.Syringe; itemIndex < ItemIndex.Count; itemIndex++) {
                if (!Run.instance.availableItems.Contains(itemIndex))
                    continue;

                var itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (itemDef.tier == itemTier && itemDef.DoesNotContainTag(ItemTag.WorldUnique)) {
                    list.Add(itemIndex);
                }
            }

            list.AddRange(AdditionalTierItems[itemTier]);
            return list;
        }


        public static List<ItemIndex> GetDefaultDropList(ItemTier itemTier, ItemTag requiredTag) {
            var list = new List<ItemIndex>();

            for (var itemIndex = ItemIndex.Syringe; itemIndex < ItemIndex.Count; itemIndex++) {
                if (!Run.instance.availableItems.Contains(itemIndex))
                    continue;

                var itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (itemDef.tier == itemTier && itemDef.ContainsTag(requiredTag) && itemDef.DoesNotContainTag(ItemTag.WorldUnique)) {
                    list.Add(itemIndex);
                }
            }

            return list;
        }

        public static List<PickupIndex> GetDefaultLunarDropList() {
            var list = new List<PickupIndex>();

            for (var equipmentIndex = EquipmentIndex.CommandMissile;
                equipmentIndex < EquipmentIndex.Count;
                equipmentIndex++) {
                if (!Run.instance.availableEquipment.Contains(equipmentIndex))
                    continue;

                var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                if (equipmentDef.canDrop && equipmentDef.isLunar) {
                    list.Add(PickupCatalog.FindPickupIndex(equipmentIndex));
                }
            }

            for (var itemIndex = ItemIndex.Syringe;
                itemIndex < ItemIndex.Count;
                itemIndex++) {
                if (!Run.instance.availableItems.Contains(itemIndex))
                    continue;

                var itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (itemDef.tier == ItemTier.Lunar && itemDef.DoesNotContainTag(ItemTag.WorldUnique)) {
                    list.Add(PickupCatalog.FindPickupIndex(itemIndex));
                }
            }

            list.AddRange(AdditionalTierItems[ItemTier.Lunar].Select(x => PickupCatalog.FindPickupIndex(x)));
            list.AddRange(AdditionalEquipment
                .Where(x => EquipmentCatalog.GetEquipmentDef(x).isLunar)
                .Select(x => PickupCatalog.FindPickupIndex(x)));
            return list;
        }

        public static List<EquipmentIndex> GetDefaultEquipmentDropList() {
            var list = new List<EquipmentIndex>();

            for (var equipmentIndex = EquipmentIndex.CommandMissile;
                equipmentIndex < EquipmentIndex.Count;
                equipmentIndex++) {
                if (!Run.instance.availableEquipment.Contains(equipmentIndex))
                    continue;

                var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                if (equipmentDef.canDrop && !equipmentDef.isLunar) {
                    list.Add(equipmentIndex);
                }
            }

            list.AddRange(AdditionalEquipment.Where(x => !EquipmentCatalog.GetEquipmentDef(x).isLunar));
            return list;
        }

        public static PickupSelection ToSelection(this List<ItemIndex> indices, float dropChance = 1.0f) {
            return indices == null ? null : new PickupSelection {
                DropChance = dropChance,
                Pickups = indices.Select(x => PickupCatalog.FindPickupIndex(x)).ToList()
            };
        }

        public static PickupSelection ToSelection(this List<EquipmentIndex> indices, float dropChance = 1.0f) {
            return indices == null ? null : new PickupSelection {
                DropChance = dropChance,
                Pickups = indices.Select(x => PickupCatalog.FindPickupIndex(x)).ToList()
            };
        }

        public static PickupSelection ToSelection(this List<PickupIndex> pickups, float dropChance = 1.0f) {
            return pickups == null ? null : new PickupSelection {
                Pickups = pickups,
                DropChance = dropChance
            };
        }
    }
}
