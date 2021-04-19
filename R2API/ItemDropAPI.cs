using BepInEx.Logging;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.ItemDrop;
using R2API.ItemDropAPITools;
using R2API.MiscHelpers;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API {

    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class ItemDropAPI {
        /*
            This class has two purposes.
            The first is the front end that allows mods to register which items should be added and removed from the vanilla drop lists for players.
            The second is to contain all the hooks required for the game to implement and utilize these modified drop lists without causing errors.
        */

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

        private const string ScavengerBackpackSpawnCardName = "iscScavBackpack";

        public static int nextElementUniformHookCount = 0;
        public static Hook nextElementUniformHook;
        public static MethodInfo nextElementUniformOrigInfo = Reflection.GetGenericMethod(typeof(Xoroshiro128Plus), "NextElementUniform", new Type[] { typeof(System.Collections.Generic.List<int>) }).MakeGenericMethod(typeof(PickupIndex));
        public static MethodInfo nextElementUniformNewInfo = typeof(ItemDropAPI).GetMethod("NextElementUniform", BindingFlags.Public | BindingFlags.Static);

        private static readonly DropList PlayerDropList = new DropList();
        internal static readonly InteractableCalculator PlayerInteractables = new InteractableCalculator();

        //  ----------------------------------------------------------------------------------------------------
        //  ----------------------------------------------------------------------------------------------------
        //  This secion is my attempt at the api's front end.

        /*
            These are the modifications that are going to be performed to the original drop lists.
            These lists are cleared after they are used to generate the drop lists so subsequent runs can be varied.
            Pickups in the PickupsSpecial lists will be added to the drop lists, ignoring if they are special items.
            This will allow mods to easily add the captains defence matrix and elite elemental drops to regular interactables.
        */
        private static List<PickupIndex> PickupsToAdd = new List<PickupIndex>();
        private static List<PickupIndex> PickupsToRemove = new List<PickupIndex>();
        private static List<PickupIndex> PickupsSpecialToAdd = new List<PickupIndex>();
        private static List<PickupIndex> PickupsSpecialToRemove = new List<PickupIndex>();

        //  This will add a pickup to the appropriate vanilla drop lists
        public static void AddPickup(PickupIndex pickupIndex, bool special = false) {
            if (pickupIndex != PickupIndex.none) {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef.itemIndex != ItemIndex.None || pickupDef.equipmentIndex != EquipmentIndex.None) {
                    List<PickupIndex> addList = PickupsToAdd;
                    List<PickupIndex> removeList = PickupsToRemove;
                    if (special) {
                        addList = PickupsSpecialToAdd;
                        removeList = PickupsSpecialToRemove;
                    }
                    AlterLists(addList, removeList, pickupIndex);
                }
            }
        }

        //  This will remove a pickup from the appropriate vanilla drop lists
        public static void RemovePickup(PickupIndex pickupIndex, bool special = false) {
            if (pickupIndex != PickupIndex.none) {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef.itemIndex != ItemIndex.None || pickupDef.equipmentIndex != EquipmentIndex.None) {
                    List<PickupIndex> addList = PickupsToRemove;
                    List<PickupIndex> removeList = PickupsToAdd;
                    if (special) {
                        addList = PickupsSpecialToRemove;
                        removeList = PickupsSpecialToAdd;
                    }
                    AlterLists(addList, removeList, pickupIndex);
                }
            }
        }

        private static void AlterLists(List<PickupIndex> addList, List<PickupIndex> removeList, PickupIndex pickupIndex) {
            if (addList.Contains(pickupIndex) == false) {
                addList.Add(pickupIndex);
                if (removeList.Contains(pickupIndex)) {
                    removeList.Remove(pickupIndex);
                }
            }
        }

        //  This will make sure there are no alterations to this pickup in the vanilla drop lists
        public static void RevertPickup(PickupIndex pickupIndex, bool special = false) {
            List<List<PickupIndex>> alterations = new List<List<PickupIndex>>() { PickupsToAdd, PickupsToRemove };
            if (special) {
                alterations = new List<List<PickupIndex>>() { PickupsSpecialToAdd, PickupsSpecialToRemove };
            }
            foreach (List<PickupIndex> alterationList in alterations) {
                if (alterationList.Contains(pickupIndex)) {
                    alterationList.Remove(pickupIndex);
                }
            }
        }

        //  ----------------------------------------------------------------------------------------------------
        //  ----------------------------------------------------------------------------------------------------
        //
        //  This point onwards is all the hooks required for the game to utilize these modified drop lists without causing errors.

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.Run.Start += DropList.OnRunStart;
            RoR2.RoR2Application.onLoad += Catalog.PopulateCatalog;
            On.RoR2.Run.BuildDropTable += RunOnBuildDropTable;
            On.RoR2.SceneDirector.PopulateScene += PopulateScene;
            On.RoR2.ShopTerminalBehavior.GenerateNewPickupServer += GenerateNewPickupServer;
            On.RoR2.DirectorCore.TrySpawnObject += CheckForInvalidInteractables;
            On.RoR2.BossGroup.DropRewards += DropRewards;
            On.RoR2.PickupPickerController.SetOptionsServer += SetOptionsServer;
            On.RoR2.ArenaMissionController.EndRound += EndRound;
            On.RoR2.GlobalEventManager.OnCharacterDeath += OnCharacterDeath;
            HookNextElementUniform();
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.Run.Start -= DropList.OnRunStart;
            RoR2.RoR2Application.onLoad -= Catalog.PopulateCatalog;
            On.RoR2.Run.BuildDropTable -= RunOnBuildDropTable;
            On.RoR2.SceneDirector.PopulateScene -= PopulateScene;
            On.RoR2.ShopTerminalBehavior.GenerateNewPickupServer -= GenerateNewPickupServer;
            On.RoR2.DirectorCore.TrySpawnObject -= CheckForInvalidInteractables;
            On.RoR2.BossGroup.DropRewards -= DropRewards;
            On.RoR2.PickupPickerController.SetOptionsServer -= SetOptionsServer;
            On.RoR2.ArenaMissionController.EndRound -= EndRound;
            On.RoR2.GlobalEventManager.OnCharacterDeath -= OnCharacterDeath;
            UnhookNextElementUniform();
        }

        public static void HookNextElementUniform() {
            if (nextElementUniformHookCount == 0) {
                nextElementUniformHook = new Hook(nextElementUniformOrigInfo, nextElementUniformNewInfo);
            }
            nextElementUniformHookCount += 1;
        }

        public static void UnhookNextElementUniform() {
            nextElementUniformHookCount -= 1;
            if (nextElementUniformHookCount == 0) {
                nextElementUniformHook.Dispose();
            }
        }

        //  Triggers all the functions required to adjust the drop lists
        private static void RunOnBuildDropTable(On.RoR2.Run.orig_BuildDropTable orig, Run run) {
            orig(run);

            PlayerDropList.DuplicateDropLists(run);
            PlayerDropList.ClearAllLists(run);
            PlayerDropList.GenerateDropLists(PickupsToAdd, PickupsToRemove, PickupsSpecialToAdd, PickupsSpecialToRemove);
            PlayerDropList.SetItems(run);
            PlayerInteractables.CalculateInvalidInteractables(PlayerDropList);

            ClearPickupOpterations();
        }

        //  Clears add and remove lists
        public static void ClearPickupOpterations() {
            PickupsToAdd.Clear();
            PickupsToRemove.Clear();
            PickupsSpecialToAdd.Clear();
            PickupsSpecialToRemove.Clear();
        }

        /*
            This will prevent interactables without droppable items from being spawned.
            This will trigger the functions in DropOdds to update the subset selection odds for interactables.
        */
        private static void PopulateScene(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector sceneDirector) {
            if (ClassicStageInfo.instance != null) {
                var categoriesLength = ClassicStageInfo.instance.interactableCategories.categories.Length;
                for (var categoryIndex = 0; categoryIndex < categoriesLength; categoryIndex++) {
                    var directorCards = new List<DirectorCard>();
                    foreach (var directorCard in ClassicStageInfo.instance.interactableCategories.categories[categoryIndex].cards) {
                        var interactableName = InteractableCalculator.GetSpawnCardName(directorCard.spawnCard);
                        //if (new List<string>().Contains(interactableName)) {
                        //} else 
                        if (PlayerInteractables.InvalidInteractables.Contains(interactableName)) {
                        } else {
                            DropOdds.UpdateChestTierOdds(directorCard.spawnCard, interactableName);
                            DropOdds.UpdateDropTableTierOdds(directorCard.spawnCard, interactableName);
                            DropOdds.UpdateDropTableItemOdds(PlayerDropList, directorCard.spawnCard, interactableName);
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

        //  This prevents errors from occuring when a drop list has no valid entries.
        static public PickupIndex NextElementUniform(Func<Xoroshiro128Plus, List<PickupIndex>, PickupIndex> orig, Xoroshiro128Plus xoroshiro128Plus, List<PickupIndex> list) {
            if (list.Count > 0) {
                return orig(xoroshiro128Plus, list);
            }
            return PickupIndex.none;
        }

        /*
            This will prevent shop terminals from displaying items, charging for items or giving items if the relevant drop list is unpopulated.
            This mainly effects the bazaar.
        */
        private static void GenerateNewPickupServer(On.RoR2.ShopTerminalBehavior.orig_GenerateNewPickupServer orig, ShopTerminalBehavior shopTerminalBehavior) {
            var dropType = InteractableCalculator.DropType.none;
            foreach (InteractableCalculator.DropType key in InteractableCalculator.TierConversion.Keys) {
                if (InteractableCalculator.TierConversion[key] == shopTerminalBehavior.itemTier) {
                    dropType = key;
                }
            }
            if (PlayerInteractables.TiersPresent[dropType] || shopTerminalBehavior.dropTable != null) {
                orig(shopTerminalBehavior);
            } else {
                shopTerminalBehavior.SetNoPickup();
                var purchaseInteraction = shopTerminalBehavior.GetComponent<PurchaseInteraction>();
                if (purchaseInteraction != null) {
                    purchaseInteraction.SetAvailable(false);
                }
            }
        }

        //  This will prevent the scav backpack from being spawned if all of its drop lists are unpopulated.
        private static GameObject CheckForInvalidInteractables(On.RoR2.DirectorCore.orig_TrySpawnObject orig, DirectorCore directorCore, DirectorSpawnRequest directorSpawnRequest) {
            if (directorSpawnRequest.spawnCard.name == ScavengerBackpackSpawnCardName) {
                if (PlayerInteractables.InvalidInteractables.Contains(InteractableCalculator.GetSpawnCardName(ScavengerBackpackSpawnCardName))) {
                    return null;
                }
            }
            return orig(directorCore, directorSpawnRequest);
        }

        //  This is to prevent bosses from dropping items that are not part of the drop lists.
        private static void DropRewards(On.RoR2.BossGroup.orig_DropRewards orig, BossGroup bossGroup) {
            var bossDrops = new List<PickupIndex>();
            var bossDropsAdjusted = new List<PickupIndex>();
            foreach (var bossDrop in bossGroup.bossDrops) {
                bossDrops.Add(bossDrop);
                bool worldUnique = false;
                if (PickupCatalog.GetPickupDef(bossDrop).itemIndex != ItemIndex.None && ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(bossDrop).itemIndex).ContainsTag(ItemTag.WorldUnique)) {
                    worldUnique = true;
                }
                if ((PlayerDropList.AvailableBossDropList.Contains(bossDrop) && !worldUnique) || (PlayerDropList.AvailableSpecialItems.Contains(bossDrop) && worldUnique)) {
                    bossDropsAdjusted.Add(bossDrop);
                }
            }

            var dropType = InteractableCalculator.DropType.tier2;
            if (bossGroup.forceTier3Reward) {
                dropType = InteractableCalculator.DropType.tier3;
            }
            bool normalListValid = PlayerInteractables.TiersPresent[dropType];

            if (normalListValid || bossDropsAdjusted.Count != 0) {
                var bossDropChanceOld = bossGroup.bossDropChance;
                if (!normalListValid) {
                    bossGroup.bossDropChance = 1;
                } else if (bossDropsAdjusted.Count == 0) {
                    bossGroup.bossDropChance = 0;
                }

                bossGroup.bossDrops = bossDropsAdjusted;
                orig(bossGroup);

                bossGroup.bossDrops = bossDrops;
                bossGroup.bossDropChance = bossDropChanceOld;
            }
        }

        //  This will prevent the scrapper from displaying items when the scrap item of the same tier is now available.
        private static void SetOptionsServer(On.RoR2.PickupPickerController.orig_SetOptionsServer orig, PickupPickerController pickupPickerController, PickupPickerController.Option[] options) {
            var optionsAdjusted = new List<PickupPickerController.Option>();
            if (pickupPickerController.contextString.Contains(ScrapperContextString)) {
                foreach (var option in options) {
                    if (PlayerDropList.AvailableSpecialItems.Contains(PickupCatalog.FindPickupIndex(Catalog.GetScrapIndex(ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(option.pickupIndex).itemIndex).tier)))) {
                        optionsAdjusted.Add(option);
                    }
                }
            } else if (pickupPickerController.contextString.Contains(CommandCubeContextString)) {
                foreach (var option in options) {
                    optionsAdjusted.Add(option);
                }
            }
            /*
            foreach (var option in options) {
                if (pickupPickerController.contextString.Contains(ScrapperContextString)) {
                    if (PlayerDropList.AvailableSpecialItems.Contains(PickupCatalog.FindPickupIndex(Catalog.GetScrapIndex(ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(option.pickupIndex).itemIndex).tier)))) {
                        optionsAdjusted.Add(option);
                    }
                } else {
                    //optionsAdjusted.Add(option);
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
            */
            options = new PickupPickerController.Option[optionsAdjusted.Count];
            for (var optionIndex = 0; optionIndex < optionsAdjusted.Count; optionIndex++) {
                options[optionIndex] = optionsAdjusted[optionIndex];
            }
            orig(pickupPickerController, options);
        }

        /*
            This will prevent arena's from dropping items from tiers of drop lists that are unpopulated.
            This mainly effects the void fields.
        */
        private static void EndRound(On.RoR2.ArenaMissionController.orig_EndRound orig, ArenaMissionController arenaMissionController) {
            var dropType = InteractableCalculator.DropType.tier1;
            if (arenaMissionController.currentRound > 4) {
                dropType = InteractableCalculator.DropType.tier2;
            }
            if (arenaMissionController.currentRound == arenaMissionController.totalRoundsMax) {
                dropType = InteractableCalculator.DropType.tier3;
            }
            if (!PlayerInteractables.TiersPresent[dropType]) {
                var rewardSpawnPositionOld = arenaMissionController.rewardSpawnPosition;
                arenaMissionController.rewardSpawnPosition = null;
                orig(arenaMissionController);
                arenaMissionController.rewardSpawnPosition = rewardSpawnPositionOld;
            } else {
                orig(arenaMissionController);
            }
        }

        //  This prevents elites from dropping items that are not part of the drop list.
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
    }
}
