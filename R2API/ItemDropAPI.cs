using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using R2API.ItemDrop;
using R2API.ItemDropAPITools;
using R2API.MiscHelpers;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class ItemDropAPI {
        private static ManualLogSource Logger => R2API.Logger;

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        private const string ScrapperContextString = "SCRAPPER";
        private const string CommandCubeContextString = "COMMAND_CUBE";

        private const string AllInteractablesResourcesPath = "SpawnCards/InteractableSpawnCard";

        private const string LockboxInteractableName = "Lockbox";
        private const string ScavengerBackpackInteractableName = "ScavBackpack";
        private const string AdaptiveChestInteractableName = "CasinoChest";
        private const string CleansingPoolInteractableName = "ShrineCleanse";
        private const string ScavengerBackpackSpawnCardName = "iscScavBackpack";

        private static readonly DropList PlayerDropList = new DropList();
        internal static readonly InteractableCalculator PlayerInteractables = new InteractableCalculator();

        public static Dictionary<ItemTier, List<ItemIndex>> AdditionalItemsReadOnly =>
            ItemsToAdd.Except(ItemsToRemove).ToDictionary(p => p.Key, p => p.Value);
        public static Dictionary<EquipmentDropType, List<EquipmentIndex>> AdditionalEquipmentsReadOnly =>
            EquipmentsToAdd.Except(EquipmentsToRemove).ToDictionary(p => p.Key, p => p.Value);

        private static Dictionary<ItemTier, List<ItemIndex>> ItemsToAdd { get; } = new Dictionary<ItemTier, List<ItemIndex>> {
            { ItemTier.Tier1, new List<ItemIndex>() },
            { ItemTier.Tier2, new List<ItemIndex>() },
            { ItemTier.Tier3, new List<ItemIndex>() },
            { ItemTier.Boss, new List<ItemIndex>() },
            { ItemTier.Lunar, new List<ItemIndex>() },
            { ItemTier.NoTier, new List<ItemIndex>() }
        };

        private static Dictionary<ItemTier, List<ItemIndex>> ItemsToRemove { get; } = new Dictionary<ItemTier, List<ItemIndex>> {
            { ItemTier.Tier1, new List<ItemIndex>() },
            { ItemTier.Tier2, new List<ItemIndex>() },
            { ItemTier.Tier3, new List<ItemIndex>() },
            { ItemTier.Boss, new List<ItemIndex>() },
            { ItemTier.Lunar, new List<ItemIndex>() },
            { ItemTier.NoTier, new List<ItemIndex>() }
        };

        private static Dictionary<EquipmentDropType, List<EquipmentIndex>> EquipmentsToAdd { get; } = new Dictionary<EquipmentDropType, List<EquipmentIndex>> {
            { EquipmentDropType.Normal, new List<EquipmentIndex>() },
            { EquipmentDropType.Boss, new List<EquipmentIndex>() },
            { EquipmentDropType.Lunar, new List<EquipmentIndex>() },
            { EquipmentDropType.Elite, new List<EquipmentIndex>() }
        };

        private static Dictionary<EquipmentDropType, List<EquipmentIndex>> EquipmentsToRemove { get; } = new Dictionary<EquipmentDropType, List<EquipmentIndex>> {
            { EquipmentDropType.Normal, new List<EquipmentIndex>() },
            { EquipmentDropType.Boss, new List<EquipmentIndex>() },
            { EquipmentDropType.Lunar, new List<EquipmentIndex>() },
            { EquipmentDropType.Elite, new List<EquipmentIndex>() }
        };

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.Run.BuildDropTable += RunOnBuildDropTable;
            On.RoR2.SceneDirector.PopulateScene += PopulateScene;
            On.RoR2.ShopTerminalBehavior.GenerateNewPickupServer += GenerateNewPickupServer;
            On.RoR2.DirectorCore.TrySpawnObject += CheckForInvalidInteractables;
            On.RoR2.ShrineChanceBehavior.AddShrineStack += FixShrineBehaviour;
            On.RoR2.BossGroup.DropRewards += DropRewards;
            On.RoR2.PickupPickerController.SetOptionsServer += SetOptionsServer;
            On.RoR2.ArenaMissionController.EndRound += EndRound;
            On.RoR2.GlobalEventManager.OnCharacterDeath += OnCharacterDeath;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.Run.BuildDropTable -= RunOnBuildDropTable;
            On.RoR2.SceneDirector.PopulateScene -= PopulateScene;
            On.RoR2.ShopTerminalBehavior.GenerateNewPickupServer -= GenerateNewPickupServer;
            On.RoR2.DirectorCore.TrySpawnObject -= CheckForInvalidInteractables;
            On.RoR2.ShrineChanceBehavior.AddShrineStack -= FixShrineBehaviour;
            On.RoR2.BossGroup.DropRewards -= DropRewards;
            On.RoR2.PickupPickerController.SetOptionsServer -= SetOptionsServer;
            On.RoR2.ArenaMissionController.EndRound -= EndRound;
            On.RoR2.GlobalEventManager.OnCharacterDeath -= OnCharacterDeath;
        }

        private static void RunOnBuildDropTable(On.RoR2.Run.orig_BuildDropTable orig, Run run) {
            Catalog.PopulateItemCatalog();
            orig(run);

            PlayerDropList.DuplicateDropLists(run);
            PlayerDropList.ClearAllLists(run);
            PlayerDropList.GenerateDropLists(ItemsToAdd, ItemsToRemove, EquipmentsToAdd, EquipmentsToRemove);
            PlayerDropList.SetItems(run);
            PlayerInteractables.CalculateInvalidInteractables(PlayerDropList);

            ClearItemOperations(ItemsToAdd);
            ClearItemOperations(ItemsToRemove);
            ClearEquipmentOperations(EquipmentsToAdd);
            ClearEquipmentOperations(EquipmentsToRemove);
        }

        public static void ClearItemOperations(Dictionary<ItemTier, List<ItemIndex>> givenDict) {
            foreach (ItemTier itemTier in givenDict.Keys) {
                givenDict[itemTier].Clear();
            }
        }
        public static void ClearEquipmentOperations(Dictionary<EquipmentDropType, List<EquipmentIndex>> givenDict) {
            foreach (EquipmentDropType equipmentDropType in givenDict.Keys) {
                givenDict[equipmentDropType].Clear();
            }
        }

        private static void PopulateScene(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector sceneDirector) {
            var allInteractables = Resources.LoadAll<InteractableSpawnCard>(AllInteractablesResourcesPath);
            foreach (var spawnCard in allInteractables) {
                var interactableName = InteractableCalculator.GetSpawnCardName(spawnCard);
                if (interactableName == LockboxInteractableName || interactableName == ScavengerBackpackInteractableName) {
                    DropOdds.UpdateChestTierOdds(spawnCard, interactableName);
                } else if (interactableName == AdaptiveChestInteractableName) {
                    DropOdds.UpdateDropTableTierOdds(spawnCard, interactableName);
                } else if (interactableName == CleansingPoolInteractableName) {
                    var dropTable = spawnCard.prefab.GetComponent<ShopTerminalBehavior>().dropTable as ExplicitPickupDropTable;
                    DropOdds.UpdateDropTableItemOdds(PlayerDropList, dropTable, interactableName);
                }
            }

            if (ClassicStageInfo.instance != null) {
                var categoriesLength = ClassicStageInfo.instance.interactableCategories.categories.Length;
                for (var categoryIndex = 0; categoryIndex < categoriesLength; categoryIndex++) {
                    var directorCards = new List<DirectorCard>();
                    foreach (var directorCard in ClassicStageInfo.instance.interactableCategories.categories[categoryIndex].cards) {
                        var interactableName = InteractableCalculator.GetSpawnCardName(directorCard.spawnCard);
                        if (new List<string>().Contains(interactableName)) {
                        }
                        if (PlayerInteractables.InvalidInteractables.Contains(interactableName)) {
                        } else {
                            DropOdds.UpdateChestTierOdds(directorCard.spawnCard, interactableName);
                            DropOdds.UpdateShrineTierOdds(directorCard, interactableName);
                            directorCards.Add(directorCard);
                        }
                    }
                    var directorCardArray = new DirectorCard[directorCards.Count];
                    for (var cardIndex = 0; cardIndex < directorCards.Count; cardIndex++) {
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

        private static void GenerateNewPickupServer(On.RoR2.ShopTerminalBehavior.orig_GenerateNewPickupServer orig, ShopTerminalBehavior shopTerminalBehavior) {
            var shopList = new List<PickupIndex>();
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
            if (shopList.Count > 0 || shopTerminalBehavior.dropTable != null) {
                orig(shopTerminalBehavior);
            } else {
                shopTerminalBehavior.SetNoPickup();
                var purchaseInteraction = shopTerminalBehavior.GetComponent<PurchaseInteraction>();
                if (purchaseInteraction != null) {
                    purchaseInteraction.SetAvailable(false);
                }
            }
        }

        private static GameObject CheckForInvalidInteractables(On.RoR2.DirectorCore.orig_TrySpawnObject orig, DirectorCore directorCore, DirectorSpawnRequest directorSpawnRequest) {
            if (directorSpawnRequest.spawnCard.name == ScavengerBackpackSpawnCardName) {
                if (PlayerInteractables.InvalidInteractables.Contains(ScavengerBackpackInteractableName)) {
                    return null;
                }
            }
            return orig(directorCore, directorSpawnRequest);
        }

        private static void FixShrineBehaviour(On.RoR2.ShrineChanceBehavior.orig_AddShrineStack orig, ShrineChanceBehavior shrineChangeBehavior, Interactor interactor) {
            var tier1Adjusted = PlayerDropList.AvailableTier1DropList;
            if (tier1Adjusted.Count == 0) {
                tier1Adjusted = DropList.Tier1DropListOriginal;
            }
            var tier2Adjusted = PlayerDropList.AvailableTier2DropList;
            if (tier2Adjusted.Count == 0) {
                tier2Adjusted = DropList.Tier2DropListOriginal;
            }
            var tier3Adjusted = PlayerDropList.AvailableTier3DropList;
            if (tier3Adjusted.Count == 0) {
                tier3Adjusted = DropList.Tier3DropListOriginal;
            }
            var equipmentAdjusted = PlayerDropList.AvailableEquipmentDropList;
            if (equipmentAdjusted.Count == 0) {
                equipmentAdjusted = DropList.EquipmentDropListOriginal;
            }

            DropList.SetDropLists(tier1Adjusted, tier2Adjusted, tier3Adjusted, equipmentAdjusted);
            orig(shrineChangeBehavior, interactor);
            DropList.RevertDropLists();
        }

        private static void DropRewards(On.RoR2.BossGroup.orig_DropRewards orig, BossGroup bossGroup) {
            var bossDrops = new List<PickupIndex>();
            var bossDropsAdjusted = new List<PickupIndex>();
            foreach (var bossDrop in bossGroup.bossDrops) {
                var pickupIndex = bossDrop;
                bossDrops.Add(pickupIndex);
                bool worldUnique = false;
                if (PickupCatalog.GetPickupDef(pickupIndex).itemIndex != ItemIndex.None && ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(pickupIndex).itemIndex).ContainsTag(ItemTag.WorldUnique)) {
                    worldUnique = true;
                }
                if ((PlayerDropList.AvailableBossDropList.Contains(pickupIndex) && !worldUnique ) || (PlayerDropList.AvailableSpecialItems.Contains(pickupIndex) && worldUnique)) {
                    bossDropsAdjusted.Add(pickupIndex);
                }
            }

            var dropList = Run.instance.availableTier2DropList;
            if (bossGroup.forceTier3Reward) {
                dropList = Run.instance.availableTier3DropList;
            }
            bool normalListValid = DropList.IsValidList(dropList);

            if (normalListValid || bossDropsAdjusted.Count != 0) {
                var bossDropChanceOld = bossGroup.bossDropChance;
                if (!normalListValid) {
                    DropList.SetDropLists(new List<PickupIndex>(), new List<PickupIndex>(), new List<PickupIndex>(), new List<PickupIndex>());
                    bossGroup.bossDropChance = 1;
                } else if (bossDropsAdjusted.Count == 0) {
                    bossGroup.bossDropChance = 0;
                }

                bossGroup.bossDrops = bossDropsAdjusted;
                orig(bossGroup);

                bossGroup.bossDrops = bossDrops;
                bossGroup.bossDropChance = bossDropChanceOld;
                if (!normalListValid) {
                    DropList.RevertDropLists();
                }
            }
        }

        private static void SetOptionsServer(On.RoR2.PickupPickerController.orig_SetOptionsServer orig, PickupPickerController pickupPickerController, PickupPickerController.Option[] options) {
            var optionsAdjusted = new List<PickupPickerController.Option>();
            foreach (var option in options) {
                if (pickupPickerController.contextString.Contains(ScrapperContextString)) {
                    if (PlayerDropList.AvailableSpecialItems.Contains(PickupCatalog.FindPickupIndex(Catalog.GetScrapIndex(ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(option.pickupIndex).itemIndex).tier)))) {
                        optionsAdjusted.Add(option);
                    }
                } else {
                    optionsAdjusted.Add(option);
                }
            }
            if (pickupPickerController.contextString.Contains(CommandCubeContextString)) {
                if (options.Length > 0) {
                    optionsAdjusted.Clear();
                    var itemIndex = PickupCatalog.GetPickupDef(options[0].pickupIndex).itemIndex;

                    var itemTier = ItemTier.NoTier;
                    if (itemIndex != ItemIndex.None) {
                        itemTier = ItemCatalog.GetItemDef(itemIndex).tier;
                    }
                    var tierList = PlayerDropList.GetDropList(itemTier);

                    bool addEntireTier = true;
                    if (options.Length == 1 && itemIndex != ItemIndex.None && ItemCatalog.GetItemDef(itemIndex).ContainsTag(ItemTag.WorldUnique)) {
                        addEntireTier = false;
                        if (itemTier != ItemTier.NoTier && tierList.Contains(options[0].pickupIndex)) {
                            addEntireTier = true;
                        }
                    }

                    if (addEntireTier) {
                        foreach (var pickupIndex in tierList) {
                            ItemIndex pickupItemIndex = PickupCatalog.GetPickupDef(pickupIndex).itemIndex;

                            if (true || pickupItemIndex == ItemIndex.None || ItemCatalog.GetItemDef(pickupItemIndex).DoesNotContainTag(ItemTag.WorldUnique)) {
                                var newOption = new PickupPickerController.Option {
                                    available = true,
                                    pickupIndex = pickupIndex
                                };
                                optionsAdjusted.Add(newOption);
                            }
                        }
                    } else {
                        optionsAdjusted.Add(options[0]);
                    }
                }
            }
            options = new PickupPickerController.Option[optionsAdjusted.Count];
            for (var optionIndex = 0; optionIndex < optionsAdjusted.Count; optionIndex++) {
                options[optionIndex] = optionsAdjusted[optionIndex];
            }
            orig(pickupPickerController, options);
        }

        private static void EndRound(On.RoR2.ArenaMissionController.orig_EndRound orig, ArenaMissionController arenaMissionController) {
            var list = Run.instance.availableTier1DropList;
            if (arenaMissionController.currentRound > 4) {
                list = Run.instance.availableTier2DropList;
            }
            if (arenaMissionController.currentRound == arenaMissionController.totalRoundsMax) {
                list = Run.instance.availableTier3DropList;
            }
            if (list.Count == 0) {
                var rewardSpawnPositionOld = arenaMissionController.rewardSpawnPosition;
                arenaMissionController.rewardSpawnPosition = null;
                orig(arenaMissionController);
                arenaMissionController.rewardSpawnPosition = rewardSpawnPositionOld;
            } else {
                orig(arenaMissionController);
            }
        }

        private static void OnCharacterDeath(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager globalEventManager, DamageReport damageReport) {
            var teamIndex = TeamIndex.None;
            if (damageReport.victimBody.teamComponent != null) {
                teamIndex = damageReport.victimBody.teamComponent.teamIndex;
            }

            var isAnElite = damageReport.victimBody.isElite;
            if (teamIndex == TeamIndex.Monster) {
                if (damageReport.victimBody.isElite) {
                    if (damageReport.victimBody.equipmentSlot != null) {
                        if (!PlayerDropList.AvailableSpecialEquipment.Contains(PickupCatalog.FindPickupIndex(damageReport.victimBody.equipmentSlot.equipmentIndex))) {
                            damageReport.victimBody.isElite = false;
                        }
                    }
                }
            }
            orig(globalEventManager, damageReport);
            if (isAnElite) {
                damageReport.victimBody.isElite = true;
            }
        }

        /// <summary>
        /// Add the given items to the given drop table.
        /// </summary>
        /// <param name="itemTier">The drop table to add the items to.</param>
        /// <param name="items">The item indices to add to the given drop table.</param>
        public static void AddItemByTier(ItemTier itemTier, params ItemIndex[] items) {
            if (ItemsToAdd.ContainsKey(itemTier)) {
                foreach (var itemIndex in items) {
                    if (!ItemsToAdd[itemTier].Contains(itemIndex)) {
                        ItemsToAdd[itemTier].Add(itemIndex);
                    }
                }
            }
            else if (ItemsToRemove.ContainsKey(itemTier)) {
                foreach (var itemIndex in items) {
                    if (ItemsToRemove[itemTier].Contains(itemIndex)) {
                        ItemsToRemove[itemTier].Remove(itemIndex);
                    }   
                }
            }
        }

        /// <summary>
        /// Remove the given items to the given drop table.
        /// </summary>
        /// <param name="itemTier">The drop table to remove the items from.</param>
        /// <param name="items">The item indices to remove from the given drop table.</param>
        public static void RemoveItemByTier(ItemTier itemTier, params ItemIndex[] items) {
            if (ItemsToRemove.ContainsKey(itemTier)) {
                foreach (var itemIndex in items) {
                    if (!ItemsToRemove[itemTier].Contains(itemIndex)) {
                        ItemsToRemove[itemTier].Add(itemIndex);
                    }   
                }
            }
            else if (ItemsToAdd.ContainsKey(itemTier)) {
                foreach (var itemIndex in items) {
                    if (ItemsToAdd[itemTier].Contains(itemIndex)) {
                        ItemsToAdd[itemTier].Remove(itemIndex);
                    }   
                }
            }
        }

        /// <summary>
        /// Add the given equipments to the given drop table.
        /// </summary>
        /// <param name="equipmentDropType">The drop table to add the items to.</param>
        /// <param name="equipments">The equipments indices to add to the given drop table.</param>
        public static void AddEquipmentByDropType(EquipmentDropType equipmentDropType, params EquipmentIndex[] equipments) {
            if (EquipmentsToAdd.ContainsKey(equipmentDropType)) {
                foreach (var equipmentIndex in equipments) {
                    if (!EquipmentsToAdd[equipmentDropType].Contains(equipmentIndex)) {
                        EquipmentsToAdd[equipmentDropType].Add(equipmentIndex);
                    }
                }
            }
            else if (EquipmentsToRemove.ContainsKey(equipmentDropType)) {
                foreach (var equipmentIndex in equipments) {
                    if (EquipmentsToRemove[equipmentDropType].Contains(equipmentIndex)) {
                        EquipmentsToRemove[equipmentDropType].Remove(equipmentIndex);
                    }   
                }
            }
        }


        /// <summary>
        /// Remove the given equipments from the given drop table.
        /// </summary>
        /// <param name="equipmentDropType">The drop table to remove the items from.</param>
        /// <param name="equipments">The equipments indices to remove from the given drop table.</param>
        public static void RemoveEquipmentByDropType(EquipmentDropType equipmentDropType, params EquipmentIndex[] equipments) {
            if (EquipmentsToRemove.ContainsKey(equipmentDropType)) {
                foreach (var equipmentIndex in equipments) {
                    if (!EquipmentsToRemove[equipmentDropType].Contains(equipmentIndex)) {
                        EquipmentsToRemove[equipmentDropType].Add(equipmentIndex);
                    }   
                }
            }
            else if (EquipmentsToAdd.ContainsKey(equipmentDropType)) {
                foreach (var equipmentIndex in equipments) {
                    if (EquipmentsToAdd[equipmentDropType].Contains(equipmentIndex)) {
                        EquipmentsToAdd[equipmentDropType].Remove(equipmentIndex);
                    }   
                }
            }
        }

        /// <summary>
        /// Add the given equipments to the given drop tables.
        /// </summary>
        /// <param name="equipmentDropTypes">The drop tables to add the items to.</param>
        /// <param name="equipments">The equipments indices to add to the given drop tables.</param>
        public static void AddEquipmentByDropType(IEnumerable<EquipmentDropType> equipmentDropTypes, params EquipmentIndex[] equipments) {
            foreach (var equipmentDropType in equipmentDropTypes) {
                AddEquipmentByDropType(equipmentDropType, equipments);
            }
        }

        /// <summary>
        /// Remove the given equipments from the given drop tables.
        /// </summary>
        /// <param name="equipmentDropTypes">The drop tables to remove the items from.</param>
        /// <param name="equipments">The equipments indices to remove from the given drop tables.</param>
        public static void RemoveEquipmentByDropType(IEnumerable<EquipmentDropType> equipmentDropTypes, params EquipmentIndex[] equipments) {
            foreach (var equipmentDropType in equipmentDropTypes) {
                RemoveEquipmentByDropType(equipmentDropType, equipments);
            }
        }

        /// <summary>
        /// Add the given equipments to the drop tables automatically, the api will look up the equipmentDefs from the indices
        /// and add the equipment depending on the information provided from the EquipmentDef. (isLunar, isElite, etc)
        /// </summary>
        /// <param name="equipments">Equipment Indices to add.</param>
        public static void AddEquipment(params EquipmentIndex[] equipments) {
            foreach (var equipmentIndex in equipments) {
                var equipmentDropTypes = EquipmentDropTypeUtil.GetEquipmentTypesFromIndex(equipmentIndex);
                foreach (var equipmentDropType in equipmentDropTypes) {
                    AddEquipmentByDropType(equipmentDropType, equipmentIndex);   
                }
            }
        }

        /// <summary>
        /// Remove the given equipments from the drop tables.
        /// </summary>
        /// <param name="equipments">Equipment Indices to remove.</param>
        public static void RemoveEquipment(params EquipmentIndex[] equipments) {
            foreach (var equipmentIndex in equipments) {
                var equipmentDropTypes = EquipmentDropTypeUtil.GetEquipmentTypesFromIndex(equipmentIndex);
                foreach (var equipmentDropType in equipmentDropTypes) {
                    RemoveEquipmentByDropType(equipmentDropType, equipmentIndex);   
                }
            }
        }

        [Obsolete("Use the AddItemByTier method instead.")]
        public static void AddToDefaultByTier(ItemTier itemTier, params ItemIndex[] items) {
            AddItemByTier(itemTier, items);
        }

        [Obsolete("Use the RemoveItemByTier method instead.")]
        public static void RemoveFromDefaultByTier(ItemTier itemTier, params ItemIndex[] items) {
            RemoveItemByTier(itemTier, items);
        }

        [Obsolete("Use the AddItemByTier method instead.")]
        public static void AddToDefaultByTier(params KeyValuePair<ItemIndex, ItemTier>[] items) {
            foreach (var (itemIndex, itemTier) in items) {
                AddItemByTier(itemTier, itemIndex);
            }
        }

        [Obsolete("Use the RemoveItemByTier method instead.")]
        public static void RemoveFromDefaultByTier(params KeyValuePair<ItemIndex, ItemTier>[] items) {
            foreach (var (itemIndex, itemTier) in items) {
                RemoveItemByTier(itemTier, itemIndex);
            }
        }

        [Obsolete("Use the AddEquipment method instead.")]
        public static void AddToDefaultEquipment(params EquipmentIndex[] equipments) {
            AddEquipment(equipments);
        }

        [Obsolete("Use the RemoveEquipment method instead.")]
        public static void RemoveFromDefaultEquipment(params EquipmentIndex[] equipments) {
            RemoveEquipment(equipments);
        }

        public static List<ItemIndex> GetDefaultDropList(ItemTier itemTier) {
            if (itemTier == ItemTier.NoTier) {
                return null;
            }

            var list = new List<ItemIndex>();

            foreach (var (_, itemIndex) in ItemCatalog.itemNameToIndex) {
                if (!Run.instance.availableItems.Contains(itemIndex))
                    continue;

                var itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (itemDef.tier == itemTier && itemDef.DoesNotContainTag(ItemTag.WorldUnique)) {
                    list.Add(itemIndex);
                }
            }

            return list;
        }


        public static List<ItemIndex> GetDefaultDropList(ItemTier itemTier, ItemTag requiredTag) {
            var list = new List<ItemIndex>();

            foreach (var (_, itemIndex) in ItemCatalog.itemNameToIndex) {
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

            foreach (var equipmentIndex in EquipmentCatalog.equipmentList) {
                if (!Run.instance.availableEquipment.Contains(equipmentIndex))
                    continue;

                var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                if (equipmentDef.isLunar) {
                    list.Add(PickupCatalog.FindPickupIndex(equipmentIndex));
                }   
            }

            foreach (var (_, itemIndex) in ItemCatalog.itemNameToIndex) {
                if (!Run.instance.availableItems.Contains(itemIndex))
                    continue;

                var itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (itemDef.tier == ItemTier.Lunar && itemDef.DoesNotContainTag(ItemTag.WorldUnique)) {
                    list.Add(PickupCatalog.FindPickupIndex(itemIndex));
                }
            }

            return list;
        }

        public static List<PickupIndex> GetDefaultEquipmentDropList() {
            var list = new List<PickupIndex>();

            foreach (var equipmentIndex in EquipmentCatalog.equipmentList) {
                if (!Run.instance.availableEquipment.Contains(equipmentIndex))
                    continue;

                var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                if (!equipmentDef.isLunar) {
                    list.Add(PickupCatalog.FindPickupIndex(equipmentIndex));
                }   
            }

            return list;
        }
    }
}
