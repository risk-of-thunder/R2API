using BepInEx.Logging;
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
using UnityEngine;
using UnityEngine.Networking;

namespace R2API {

    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class ItemDropAPI {

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
        private static bool commandArtifact = false;
        private static readonly System.Reflection.MethodInfo rouletteGetEntryIndexForTime = typeof(RouletteChestController).GetMethod("GetEntryIndexForTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

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
            IL.RoR2.ShopTerminalBehavior.GenerateNewPickupServer += GenerateNewPickupServer;

            IL.RoR2.ChestBehavior.RollItem += RollItem;
            On.RoR2.ChestBehavior.RollEquipment += RollEquipment;
            On.RoR2.ChestBehavior.ItemDrop += ItemDrop;
            IL.RoR2.ShrineChanceBehavior.AddShrineStack += AddShrineStack;
            On.RoR2.RouletteChestController.GenerateEntriesServer += GenerateEntriesServer;
            IL.RoR2.BasicPickupDropTable.GenerateDrop += GenerateDrop;
            On.RoR2.RouletteChestController.EjectPickupServer += EjectPickupServer;
            IL.RoR2.BossGroup.DropRewards += DropRewards;
            IL.RoR2.GlobalEventManager.OnCharacterDeath += OnCharacterDeath;

            On.RoR2.Run.Start += RunStart;
            On.RoR2.MultiShopController.CreateTerminals += CreateTerminals;
            IL.RoR2.ShopTerminalBehavior.OnSerialize += ShopTerminalOnSerialize;
            IL.RoR2.ShopTerminalBehavior.OnDeserialize += ShopTerminalOnDeserialize;
            On.RoR2.PickupDisplay.RebuildModel += RebuildModel;

            IL.RoR2.PickupDropletController.CreatePickupDroplet += CreatePickupDroplet;
            On.RoR2.PickupDropletController.OnCollisionEnter += OnCollisionEnter;
            On.RoR2.PickupDropletController.CreatePickupDroplet += CreatePickupDroplet;
            IL.RoR2.UI.PickupPickerPanel.SetPickupOptions += SetPickupOptions;
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
            IL.RoR2.ShopTerminalBehavior.GenerateNewPickupServer -= GenerateNewPickupServer;

            IL.RoR2.ChestBehavior.RollItem -= RollItem;
            On.RoR2.ChestBehavior.RollEquipment -= RollEquipment;
            On.RoR2.ChestBehavior.ItemDrop -= ItemDrop;
            IL.RoR2.ShrineChanceBehavior.AddShrineStack -= AddShrineStack;
            On.RoR2.RouletteChestController.GenerateEntriesServer -= GenerateEntriesServer;
            IL.RoR2.BasicPickupDropTable.GenerateDrop -= GenerateDrop;
            On.RoR2.RouletteChestController.EjectPickupServer -= EjectPickupServer;
            IL.RoR2.BossGroup.DropRewards -= DropRewards;
            IL.RoR2.GlobalEventManager.OnCharacterDeath -= OnCharacterDeath;

            On.RoR2.Run.Start -= RunStart;
            On.RoR2.MultiShopController.CreateTerminals -= CreateTerminals;
            IL.RoR2.ShopTerminalBehavior.OnSerialize -= ShopTerminalOnSerialize;
            IL.RoR2.ShopTerminalBehavior.OnDeserialize -= ShopTerminalOnDeserialize;
            On.RoR2.PickupDisplay.RebuildModel -= RebuildModel;

            IL.RoR2.PickupDropletController.CreatePickupDroplet -= CreatePickupDroplet;
            On.RoR2.PickupDropletController.OnCollisionEnter -= OnCollisionEnter;
            On.RoR2.PickupDropletController.CreatePickupDroplet -= CreatePickupDroplet;
            IL.RoR2.UI.PickupPickerPanel.SetPickupOptions -= SetPickupOptions;
        }

        private static void RunStart(On.RoR2.Run.orig_Start orig, Run run) {
            PopulateSafePickups();
            orig(run);
        }

        private static void RunOnBuildDropTable(On.RoR2.Run.orig_BuildDropTable orig, Run run) {
            commandArtifact = run.GetComponent<RunArtifactManager>().IsArtifactEnabled(RoR2Content.Artifacts.commandArtifactDef);

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
            pickupDropletCommandArtifactLists.Clear();
            rouletteCommandArtifactLists.Clear();
            chestCommandArtifactLists.Clear();

            var allInteractables = Resources.LoadAll<InteractableSpawnCard>(AllInteractablesResourcesPath);
            foreach (var spawnCard in allInteractables) {
                var interactableName = InteractableCalculator.GetSpawnCardName(spawnCard);
                if (interactableName == LockboxInteractableName || interactableName == ScavengerBackpackInteractableName) {
                    DropOdds.UpdateChestTierOdds(spawnCard, interactableName);
                }
                else if (interactableName == AdaptiveChestInteractableName) {
                    DropOdds.UpdateDropTableTierOdds(spawnCard, interactableName);
                }
                else if (interactableName == CleansingPoolInteractableName) {
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
                        }
                        else {
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

        // RETREIVES THE BACKED UP DROP LISTS TO SET THE COMMAND ARTIFACT MENU OPTIONS

        private static List<PickupIndex> currentPickupList = new List<PickupIndex>();
        private static bool uniquePickup = false;
        private static readonly Dictionary<PickupDropletController, List<PickupIndex>> pickupDropletCommandArtifactLists = new Dictionary<PickupDropletController, List<PickupIndex>>();
        private static readonly Dictionary<PickupDropletController, bool> pickupDropletCommandArtifactUniquePickup = new Dictionary<PickupDropletController, bool>();

        private static void CreatePickupDroplet(ILContext ilContext) {
            var spawnMethodInfo = typeof(UnityEngine.Networking.NetworkServer).GetMethod("Spawn", new[] { typeof(GameObject) });

            var cursor = new ILCursor(ilContext);
            cursor.GotoNext(
                x => x.MatchCall(spawnMethodInfo)
            );
            cursor.Emit(OpCodes.Dup);
            cursor.EmitDelegate<System.Action<GameObject>>((dropletGameObject) => {
                pickupDropletCommandArtifactLists.Add(dropletGameObject.GetComponent<PickupDropletController>(), currentPickupList);
                pickupDropletCommandArtifactUniquePickup.Add(dropletGameObject.GetComponent<PickupDropletController>(), uniquePickup);
            });
        }

        private static void OnCollisionEnter(On.RoR2.PickupDropletController.orig_OnCollisionEnter orig, PickupDropletController pickupDropletController, Collision collision) {
            if (pickupDropletCommandArtifactLists.ContainsKey(pickupDropletController)) {
                currentPickupList = pickupDropletCommandArtifactLists[pickupDropletController];
                uniquePickup = pickupDropletCommandArtifactUniquePickup[pickupDropletController];
            }
            orig(pickupDropletController, collision);
        }

        private static void CreatePickupDroplet(On.RoR2.PickupDropletController.orig_CreatePickupDroplet orig, PickupIndex pickupIndex, Vector3 position, Vector3 velocity) {
            if (commandArtifact) {
                pickupIndex = AdjustCommandPickupIndex(currentPickupList, pickupIndex);
            }
            orig(pickupIndex, position, velocity);
        }

        private static void SetPickupOptions(ILContext ilContext) {
            var pickupIndexFieldInfo = typeof(PickupPickerController.Option).GetField("pickupIndex", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var getPickupDefMethodInfo = typeof(PickupCatalog).GetMethod("GetPickupDef", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            var cursor = new ILCursor(ilContext);
            cursor.GotoNext(
                x => x.MatchLdarg(1),
                x => x.MatchLdcI4(0),
                x => x.MatchLdelema("RoR2.PickupPickerController/Option"),
                x => x.MatchLdfld(pickupIndexFieldInfo),
                x => x.MatchCall(getPickupDefMethodInfo),
                x => x.MatchDup()
            );
            cursor.Index += 1;
            cursor.RemoveRange(3);
            cursor.EmitDelegate<Func<PickupPickerController.Option[], PickupIndex>>((options) => {
                List<PickupIndex> pickupList = new List<PickupIndex>();
                foreach (PickupPickerController.Option option in options) {
                    pickupList.Add(option.pickupIndex);
                }
                return AdjustCommandPickupIndex(pickupList, pickupList[0]);
            });
        }

        private static Dictionary<ItemTier, PickupIndex> safePickups;

        private static void PopulateSafePickups() {
            safePickups = new Dictionary<ItemTier, PickupIndex>() {
                { ItemTier.Tier1, PickupCatalog.FindPickupIndex(ItemIndex.ScrapWhite) },
                { ItemTier.Tier2, PickupCatalog.FindPickupIndex(ItemIndex.ScrapGreen) },
                { ItemTier.Tier3, PickupCatalog.FindPickupIndex(ItemIndex.ScrapRed) },
                { ItemTier.Boss, PickupCatalog.FindPickupIndex(ItemIndex.ScrapYellow) },
                { ItemTier.Lunar, PickupCatalog.FindPickupIndex(ItemIndex.LunarDagger) },
                { ItemTier.NoTier, PickupCatalog.FindPickupIndex(EquipmentIndex.CritOnUse) },
            };
        }

        private static PickupIndex AdjustCommandPickupIndex(List<PickupIndex> pickupList, PickupIndex givenPickupIndex) {
            if (PickupListsEqual(Run.instance.availableTier1DropList, pickupList)) {
                return safePickups[ItemTier.Tier1];
            }
            else if (PickupListsEqual(Run.instance.availableTier2DropList, pickupList)) {
                return safePickups[ItemTier.Tier2];
            }
            else if (PickupListsEqual(Run.instance.availableTier3DropList, pickupList)) {
                return safePickups[ItemTier.Tier3];
            }
            else if (PickupListsEqual(Run.instance.availableBossDropList, pickupList)) {
                return safePickups[ItemTier.Boss];
            }
            else if (PickupListsEqual(Run.instance.availableLunarDropList, pickupList)) {
                return safePickups[ItemTier.Lunar];
            }
            else if (PickupListsEqual(Run.instance.availableEquipmentDropList, pickupList)) {
                return safePickups[ItemTier.NoTier];
            }
            return givenPickupIndex;
        }

        private static bool PickupListsEqual(List<PickupIndex> listA, List<PickupIndex> listB) {
            if (listA.Count == listB.Count) {
                bool listsEqual = true;
                for (int listIndex = 0; listIndex < listA.Count; listIndex++) {
                    if (listA[listIndex] != listB[listIndex]) {
                        listsEqual = false;
                        break;
                    }
                }
                if (listsEqual) {
                    return true;
                }
            }
            return false;
        }

        // WILL BACKUP UP THE DROP LIST SELECTED BY CHESTS FOR USE WITH THE COMMAND ARTIFACT

        private static readonly Dictionary<ChestBehavior, List<PickupIndex>> chestCommandArtifactLists = new Dictionary<ChestBehavior, List<PickupIndex>>();

        private static void RollItem(ILContext ilContext) {
            var findPickupIndexMethodInfo = typeof(PickupCatalog).GetMethod("FindPickupIndex", new[] { typeof(string) });
            var addMethodInfo = typeof(List<PickupIndex>).GetMethod("Add", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            var rollItemAddMethodInfo = Utils.Reflection.GetNestedMethod(typeof(ChestBehavior), "<RollItem>g__Add|1");
            var selectorFieldInfo = Utils.Reflection.GetNestedField(typeof(ChestBehavior), "selector");
            var filterListMethodInfo = typeof(ItemDropAPI).GetMethod("FilterList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var instanceMethodInfo = typeof(Run).GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetMethod;
            var treasureRngFieldInfo = typeof(Run).GetField("treasureRng", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var nextNormalizedMethodInfo = typeof(Xoroshiro128Plus).GetProperty("nextNormalizedFloat", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetMethod;
            var evaluateMethodInfo = typeof(WeightedSelection<List<PickupIndex>>).GetMethod("Evaluate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            var cursor = new ILCursor(ilContext);

            cursor.GotoNext(
                x => x.MatchLdloc(0),
                x => x.MatchLdstr("LunarCoin.Coin0"),
                x => x.MatchCall(findPickupIndexMethodInfo),
                x => x.MatchCallvirt(addMethodInfo),
                x => x.MatchDup()
            );

            while (cursor.TryGotoNext(x => x.MatchDup())) {
                cursor.Index += 1;
                cursor.Emit(OpCodes.Ldfld, selectorFieldInfo);
            }

            cursor.Index = 0;
            while (cursor.TryGotoNext(x => x.MatchCallvirt(rollItemAddMethodInfo))) {
                cursor.Remove();
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Callvirt, filterListMethodInfo);
            }

            cursor.GotoNext(
                x => x.MatchLdfld(selectorFieldInfo),
                x => x.MatchCall(instanceMethodInfo),
                x => x.MatchLdfld(treasureRngFieldInfo),
                x => x.MatchCallvirt(nextNormalizedMethodInfo),
                x => x.MatchCallvirt(evaluateMethodInfo),
                x => x.MatchStloc(1)
            );
            cursor.Index += 6;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<System.Action<ChestBehavior, List<PickupIndex>>>((chestBehavior, dropList) => {
                chestCommandArtifactLists.Add(chestBehavior, dropList);
            });
        }

        private static void FilterList(WeightedSelection<List<PickupIndex>> selector, List<PickupIndex> dropList, float dropChance, ChestBehavior chestBehavior) {
            if ((double)dropChance <= 0) {
                return;
            }
            List<PickupIndex> filteredDropList = new List<PickupIndex>();
            foreach (PickupIndex pickupIndex in dropList) {
                if (chestBehavior.requiredItemTag == ItemTag.Any || (PickupCatalog.GetPickupDef(pickupIndex).itemIndex != ItemIndex.None && ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(pickupIndex).itemIndex).ContainsTag(chestBehavior.requiredItemTag))) {
                    filteredDropList.Add(pickupIndex);
                }
            }
            selector.AddChoice(filteredDropList, dropChance);
        }

        private static void RollEquipment(On.RoR2.ChestBehavior.orig_RollEquipment orig, ChestBehavior chestBehavior) {
            chestCommandArtifactLists.Add(chestBehavior, Run.instance.availableEquipmentDropList);
            orig(chestBehavior);
        }

        private static void ItemDrop(On.RoR2.ChestBehavior.orig_ItemDrop orig, ChestBehavior chestBehavior) {
            if (chestCommandArtifactLists.ContainsKey(chestBehavior)) {
                currentPickupList = chestCommandArtifactLists[chestBehavior];
                chestCommandArtifactLists.Remove(chestBehavior);
                uniquePickup = false;
            }
            orig(chestBehavior);
        }

        //Backs up the drop list selected by shrines of chance for use with the command Artifact.

        private static readonly List<List<PickupIndex>> shrineChanceDropLists = new List<List<PickupIndex>>();
        private static WeightedSelection<List<PickupIndex>> shrineChanceWeightedSelection;

        private static void AddShrineStack(ILContext ilContext) {
            var rngFieldInfo = typeof(ShrineChanceBehavior).GetField("rng", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var instanceMethodInfo = typeof(Run).GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetMethod;
            var addChoiceMethodInfo = typeof(WeightedSelection<PickupIndex>).GetMethod("AddChoice", new[] { typeof(PickupIndex), typeof(float) });
            var nextNormalizedMethodInfo = typeof(Xoroshiro128Plus).GetProperty("nextNormalizedFloat", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetMethod;
            var evaluateMethodInfo = typeof(WeightedSelection<PickupIndex>).GetMethod("Evaluate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            var cursor = new ILCursor(ilContext);

            cursor.EmitDelegate<Action>(() => {
                shrineChanceDropLists.Clear();
                shrineChanceWeightedSelection = new WeightedSelection<List<PickupIndex>>(8);
                shrineChanceDropLists.Add(new List<PickupIndex>() { PickupIndex.none });
            });

            while (cursor.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(rngFieldInfo),
                x => x.MatchCall(instanceMethodInfo),
                x => x.OpCode == OpCodes.Ldfld,
                x => x.OpCode == OpCodes.Callvirt
                )) {
                cursor.Index += 4;
                cursor.Emit(OpCodes.Dup);
                cursor.EmitDelegate<Action<List<PickupIndex>>>((dropList) => {
                    shrineChanceDropLists.Add(dropList);
                });
            }

            List<OpCode> ldloc = new List<OpCode>() { OpCodes.Ldloc, OpCodes.Ldloc_0, OpCodes.Ldloc_1, OpCodes.Ldloc_2, OpCodes.Ldloc_3, OpCodes.Ldloc_S };
            while (cursor.TryGotoNext(
                x => ldloc.Contains(x.OpCode),
                x => x.MatchLdarg(0),
                x => x.OpCode == OpCodes.Ldfld,
                x => x.MatchCallvirt(addChoiceMethodInfo)
                )) {
                cursor.Index += 3;
                cursor.Emit(OpCodes.Dup);
                cursor.EmitDelegate<Action<float>>((dropChance) => {
                    shrineChanceWeightedSelection.AddChoice(shrineChanceDropLists[0], dropChance);
                    shrineChanceDropLists.RemoveAt(0);
                });
            }

            cursor.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(rngFieldInfo),
                x => x.MatchCallvirt(nextNormalizedMethodInfo),
                x => x.MatchCallvirt(evaluateMethodInfo)
            );
            cursor.Emit(OpCodes.Pop);
            cursor.Index += 3;
            cursor.Remove();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, rngFieldInfo);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<float, Xoroshiro128Plus, ShrineChanceBehavior, PickupIndex>>((treasureNormalizedFloat, rng, shrineChanceBehavior) => {
                currentPickupList = shrineChanceWeightedSelection.Evaluate(treasureNormalizedFloat);
                uniquePickup = false;
                return rng.NextElementUniform(currentPickupList);
            });
        }

        //WILL BACKUP UP THE DROP LIST SELECTED BY ROULETTE CHESTS FOR USE WITH THE COMMAND ARTIFACT
        //WILL REMOVE DROP LIST FILTERING FOR CHESTS

        private static bool rouletteChestEntriesAdding = false;
        private static RouletteChestController currentRouletteChestController;
        private static readonly Dictionary<RouletteChestController, List<List<PickupIndex>>> rouletteCommandArtifactLists = new Dictionary<RouletteChestController, List<List<PickupIndex>>>();

        private static void GenerateEntriesServer(On.RoR2.RouletteChestController.orig_GenerateEntriesServer orig, RouletteChestController rouletteChestController, Run.FixedTimeStamp fixedTimeStamp) {
            if (commandArtifact) {
                rouletteChestEntriesAdding = true;
                currentRouletteChestController = rouletteChestController;
            }
            orig(rouletteChestController, fixedTimeStamp);
            rouletteChestEntriesAdding = false;
        }

        private static void GenerateDrop(ILContext ilContext) {
            var selectorFieldInfo = typeof(BasicPickupDropTable).GetField("selector", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var nextNormalizedMethodInfo = typeof(Xoroshiro128Plus).GetProperty("nextNormalizedFloat", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetMethod;
            var evaluateMethodInfo = typeof(WeightedSelection<List<PickupIndex>>).GetMethod("Evaluate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            var cursor = new ILCursor(ilContext);

            cursor.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(selectorFieldInfo),
                x => x.MatchLdarg(1),
                x => x.MatchCallvirt(nextNormalizedMethodInfo),
                x => x.MatchCallvirt(evaluateMethodInfo)
            );
            cursor.Index += 5;
            cursor.Emit(OpCodes.Dup);
            cursor.EmitDelegate<Action<List<PickupIndex>>>((dropList) => {
                if (rouletteChestEntriesAdding) {
                    if (!rouletteCommandArtifactLists.ContainsKey(currentRouletteChestController)) {
                        rouletteCommandArtifactLists.Add(currentRouletteChestController, new List<List<PickupIndex>>());
                    }
                    rouletteCommandArtifactLists[currentRouletteChestController].Add(dropList);
                }
            });
        }

        private static void EjectPickupServer(On.RoR2.RouletteChestController.orig_EjectPickupServer orig, RouletteChestController rouletteChestController, PickupIndex pickupIndex) {
            if (commandArtifact) {
                object entryIndexForTimeObject = rouletteGetEntryIndexForTime.Invoke(rouletteChestController, new object[] { Run.FixedTimeStamp.now });
                currentPickupList = rouletteCommandArtifactLists[currentRouletteChestController][(int)entryIndexForTimeObject];
                uniquePickup = false;
            }
            orig(rouletteChestController, pickupIndex);
        }

        //WILL BACKUP UP THE DROP LIST SELECTED BY BOSSES FOR USE WITH THE COMMAND ARTIFACT

        private static void DropRewards(ILContext ilContext) {
            var bossDropsFieldInfo = typeof(BossGroup).GetField("bossDrops", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var countMethodInfo = typeof(List<PickupIndex>).GetProperty("Count", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetMethod;

            var rngFieldInfo = typeof(BossGroup).GetField("rng", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var selectorFieldInfo = typeof(BasicPickupDropTable).GetField("selector", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var nextNormalizedMethodInfo = typeof(Xoroshiro128Plus).GetProperty("nextNormalizedFloat", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetMethod;
            var evaluateMethodInfo = typeof(WeightedSelection<List<PickupIndex>>).GetMethod("Evaluate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            var cursor = new ILCursor(ilContext);

            cursor.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(bossDropsFieldInfo),
                x => x.MatchCallvirt(countMethodInfo),
                x => x.MatchLdcI4(0),
                x => x.OpCode == OpCodes.Ble_S
            );
            cursor.Emit(OpCodes.Ldloc, 1);
            cursor.EmitDelegate<Action<List<PickupIndex>>>((dropList) => {
                uniquePickup = false;
                currentPickupList = dropList;
            });

            cursor.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(rngFieldInfo),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(bossDropsFieldInfo),
                x => x.OpCode == OpCodes.Callvirt,
                x => x.MatchStloc(2)
            );
            cursor.Index += 5;
            cursor.Emit(OpCodes.Dup);
            cursor.EmitDelegate<Action<PickupIndex>>((pickupIndex) => {
                if (PlayerDropList.AvailableBossDropList.Contains(pickupIndex)) {
                    currentPickupList = PlayerDropList.AvailableBossDropList;
                }
                else {
                    currentPickupList = new List<PickupIndex>() { pickupIndex };
                    uniquePickup = true;
                }
            });
        }

        //WILL BACKUP UP THE DROP LIST SELECTED BY ELITE MONSTERS FOR USE WITH THE COMMAND ARTIFACT

        private static void OnCharacterDeath(ILContext ilContext) {
            var checkRollMethodInfo = typeof(RoR2.Util).GetMethod("CheckRoll", new[] { typeof(float), typeof(RoR2.CharacterMaster) });
            var implicitMethodInfo = typeof(UnityEngine.Object).GetMethod("op_Implicit", new[] { typeof(UnityEngine.Object) });
            var isEliteMethodInfo = typeof(RoR2.CharacterBody).GetProperty("isElite", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetMethod;

            ILLabel ilLabel = null;
            ILCursor cursor = new ILCursor(ilContext);
            cursor.GotoNext(
                //x => x.MatchLdcR4(0.025f),
                x => x.OpCode == OpCodes.Ldc_R4,
                x => x.OpCode == OpCodes.Ldloc_S,
                x => x.MatchCall(checkRollMethodInfo),
                x => x.MatchBrfalse(out ilLabel),
                x => x.MatchLdloc(2),
                x => x.MatchCall(implicitMethodInfo),
                x => x.MatchBrfalse(out ilLabel),
                x => x.MatchLdloc(2),
                x => x.MatchCallvirt(isEliteMethodInfo),
                x => x.MatchBrfalse(out ilLabel)
            );
            //cursor.Next.Operand = 100f;

            cursor.Index += 10;
            var onCharacterDeathLocalVariables = typeof(RoR2.GlobalEventManager).GetMethod("OnCharacterDeath", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetMethodBody().LocalVariables;
            var equipmentIndexIndex = 0;
            foreach (System.Reflection.LocalVariableInfo localVariableInfo in onCharacterDeathLocalVariables) {
                if (localVariableInfo.LocalType == typeof(EquipmentIndex)) {
                    equipmentIndexIndex = localVariableInfo.LocalIndex;
                    break;
                }
            }
            cursor.Emit(OpCodes.Ldloc, equipmentIndexIndex);
            cursor.EmitDelegate<Action<EquipmentIndex>>((equipmentIndex) => {
                currentPickupList = new List<PickupIndex>() { PickupCatalog.FindPickupIndex(equipmentIndex) };
                uniquePickup = true;
            });
        }

        //WILL REMOVE DROP LIST FILTERING FOR SHOP TERMINALS

        private static void GenerateNewPickupServer(ILContext ilContext) {
            var dropTableFieldInfo = typeof(ShopTerminalBehavior).GetField("dropTable", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var instanceMethodInfo = typeof(Run).GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetMethod;
            var treasureRngFieldInfo = typeof(Run).GetField("treasureRng", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var generateDropMethodInfo = typeof(PickupDropTable).GetMethod("GenerateDrop", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            ILCursor cursor = new ILCursor(ilContext);
            cursor.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdloc(1),
                x => x.OpCode == OpCodes.Callvirt,
                x => x.MatchStloc(0)
            );
            cursor.Index += 1;
            cursor.Emit(OpCodes.Pop);
            cursor.Index += 1;
            cursor.Remove();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<List<PickupIndex>, ShopTerminalBehavior, PickupIndex>>((dropList, shopTerminalBehavior) => {
                return Run.instance.treasureRng.NextElementUniform<PickupIndex>(dropList);
            });
        }

        private static void CreateTerminals(On.RoR2.MultiShopController.orig_CreateTerminals orig, MultiShopController multiShopController) {
            orig(multiShopController);
            ItemTier itemTier = multiShopController.itemTier;
            if (multiShopController.doEquipmentInstead) {
                itemTier = ItemTier.NoTier;
            }
            foreach (GameObject terminal in multiShopController.terminalGameObjects) {
                terminal.GetComponent<ShopTerminalBehavior>().itemTier = itemTier;
            }
        }

        private static void ShopTerminalOnSerialize(ILContext ilContext) {
            var hasBeenPurchasedInfo = typeof(ShopTerminalBehavior).GetField("hasBeenPurchased", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var writeInfo = typeof(UnityEngine.Networking.NetworkWriter).GetMethod("Write", new Type[] { typeof(bool) });

            ILCursor cursor = new ILCursor(ilContext);
            cursor.GotoNext(
                x => x.MatchLdarg(1),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(hasBeenPurchasedInfo),
                x => x.MatchCallvirt(writeInfo)
            );
            cursor.Index += 4;
            cursor.Emit(OpCodes.Ldarg, 0);
            cursor.Emit(OpCodes.Ldarg, 1);
            cursor.EmitDelegate<Action<ShopTerminalBehavior, NetworkWriter>>((shopTerminalBehavior, writer) => {
                writer.WritePackedUInt32((uint)shopTerminalBehavior.itemTier);
            });
        }

        private static void ShopTerminalOnDeserialize(ILContext ilContext) {
            var readBooleanInfo = typeof(UnityEngine.Networking.NetworkReader).GetMethod("ReadBoolean", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var hasBeenPurchasedInfo = typeof(ShopTerminalBehavior).GetField("hasBeenPurchased", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            ILCursor cursor = new ILCursor(ilContext);
            cursor.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(1),
                x => x.MatchCallvirt(readBooleanInfo),
                x => x.MatchStfld(hasBeenPurchasedInfo)
            );
            cursor.Index += 4;
            cursor.Emit(OpCodes.Ldarg, 0);
            cursor.Emit(OpCodes.Ldarg, 1);
            cursor.EmitDelegate<Action<ShopTerminalBehavior, NetworkReader>>((shopTerminalBehavior, reader) => {
                shopTerminalBehavior.itemTier = (ItemTier)reader.ReadPackedUInt32();
            });
        }

        private static void RebuildModel(On.RoR2.PickupDisplay.orig_RebuildModel orig, PickupDisplay pickupDisplay) {
            System.Reflection.FieldInfo hiddenInfo = typeof(RoR2.PickupDisplay).GetField("hidden", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo pickupIndexInfo = typeof(RoR2.PickupDisplay).GetField("pickupIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool hidden = (bool)hiddenInfo.GetValue(pickupDisplay);
            PickupIndex pickupIndex = (PickupIndex)pickupIndexInfo.GetValue(pickupDisplay);
            ShopTerminalBehavior shopTerminalBehavior = null;
            if (hidden) {
                if (pickupDisplay.transform.parent != null && pickupDisplay.transform.parent.parent != null && pickupDisplay.transform.parent.parent.parent != null) {
                    shopTerminalBehavior = pickupDisplay.transform.parent.parent.parent.GetComponent<ShopTerminalBehavior>();
                    if (shopTerminalBehavior != null) {
                        pickupIndexInfo.SetValue(pickupDisplay, safePickups[shopTerminalBehavior.itemTier]);
                    }
                }
            }
            orig(pickupDisplay);
            if (hidden) {
                if (shopTerminalBehavior != null) {
                    pickupIndexInfo.SetValue(pickupDisplay, pickupIndex);
                }
            }
        }

        private static void GenerateNewPickupServer(On.RoR2.ShopTerminalBehavior.orig_GenerateNewPickupServer orig, ShopTerminalBehavior shopTerminalBehavior) {
            var dropType = InteractableCalculator.DropType.none;
            if (shopTerminalBehavior.itemTier == ItemTier.Tier1) {
                dropType = InteractableCalculator.DropType.tier1;
            }
            else if (shopTerminalBehavior.itemTier == ItemTier.Tier2) {
                dropType = InteractableCalculator.DropType.tier2;
            }
            else if (shopTerminalBehavior.itemTier == ItemTier.Tier3) {
                dropType = InteractableCalculator.DropType.tier3;
            }
            else if (shopTerminalBehavior.itemTier == ItemTier.Boss) {
                dropType = InteractableCalculator.DropType.boss;
            }
            else if (shopTerminalBehavior.itemTier == ItemTier.Lunar) {
                dropType = InteractableCalculator.DropType.lunar;
            }

            if (PlayerInteractables.TiersPresent[dropType] || shopTerminalBehavior.dropTable != null) {
                orig(shopTerminalBehavior);
            }
            else {
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
            if (!PlayerInteractables.TiersPresent[InteractableCalculator.DropType.tier1]) {
                tier1Adjusted = DropList.Tier1DropListOriginal;
            }
            var tier2Adjusted = PlayerDropList.AvailableTier2DropList;
            if (!PlayerInteractables.TiersPresent[InteractableCalculator.DropType.tier2]) {
                tier2Adjusted = DropList.Tier2DropListOriginal;
            }
            var tier3Adjusted = PlayerDropList.AvailableTier3DropList;
            if (!PlayerInteractables.TiersPresent[InteractableCalculator.DropType.tier3]) {
                tier3Adjusted = DropList.Tier3DropListOriginal;
            }
            var equipmentAdjusted = PlayerDropList.AvailableEquipmentDropList;
            if (!PlayerInteractables.TiersPresent[InteractableCalculator.DropType.equipment]) {
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
                if ((PlayerDropList.AvailableBossDropList.Contains(pickupIndex) && !worldUnique) || (PlayerDropList.AvailableSpecialItems.Contains(pickupIndex) && worldUnique)) {
                    bossDropsAdjusted.Add(pickupIndex);
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
                    DropList.SetDropLists(new List<PickupIndex>(), new List<PickupIndex>(), new List<PickupIndex>(), new List<PickupIndex>());
                    bossGroup.bossDropChance = 1;
                }
                else if (bossDropsAdjusted.Count == 0) {
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
            if (pickupPickerController.contextString.Contains(ScrapperContextString)) {
                foreach (var option in options) {
                    if (PlayerDropList.AvailableSpecialItems.Contains(PickupCatalog.FindPickupIndex(Catalog.GetScrapIndex(ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(option.pickupIndex).itemIndex).tier)))) {
                        optionsAdjusted.Add(option);
                    }
                }
            }
            else if (pickupPickerController.contextString.Contains(CommandCubeContextString)) {
                foreach (var pickupIndex in currentPickupList) {
                    _ = PickupCatalog.GetPickupDef(pickupIndex).itemIndex;
                    var newOption = new PickupPickerController.Option {
                        available = true,
                        pickupIndex = pickupIndex
                    };
                    optionsAdjusted.Add(newOption);
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

        private static void EndRound(On.RoR2.ArenaMissionController.orig_EndRound orig, ArenaMissionController arenaMissionController) {
            var dropType = InteractableCalculator.DropType.tier1;
            if (arenaMissionController.currentRound > 4) {
                dropType = InteractableCalculator.DropType.tier2;
            }
            if (arenaMissionController.currentRound == arenaMissionController.totalRoundsMax) {
                dropType = InteractableCalculator.DropType.tier3;
            }
            if (PlayerInteractables.TiersPresent[dropType]) {
                var rewardSpawnPositionOld = arenaMissionController.rewardSpawnPosition;
                arenaMissionController.rewardSpawnPosition = null;
                orig(arenaMissionController);
                arenaMissionController.rewardSpawnPosition = rewardSpawnPositionOld;
            }
            else {
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
