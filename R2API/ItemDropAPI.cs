using System.Collections.Generic;
using System.Linq;
using RoR2;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using BepInEx.Logging;
using R2API.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API {
    public class PickupSelection {
        public List<PickupIndex> Pickups { get; set; }
        public float DropChance { get; set; } = 1.0f;
    }

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

            var shrineSelections = new [] {
                new List<ItemIndex> {ItemIndex.None}.ToSelection(ItemDropAPI.DefaultShrineFailureWeight),
                t1.ToSelection(ItemDropAPI.DefaultShrineTier1Weight),
                t2.ToSelection(ItemDropAPI.DefaultShrineTier2Weight),
                t3.ToSelection(ItemDropAPI.DefaultShrineTier3Weight),
                eq.ToSelection(ItemDropAPI.DefaultShrineEquipmentWeight)
            };

            ItemDropAPI.AddDrops(ItemDropLocation.Shrine, shrineSelections);
        }

        public static void AddChestDefaultDrops() {
            var t1 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier1);
            var t2 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier2);
            var t3 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier3);

            var chestSelections = new [] {
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


            ItemDropAPI.AddDrops(ItemDropLocation.UtilityChest, utilitySelections);
            ItemDropAPI.AddDrops(ItemDropLocation.DamageChest, damageSelections);
            ItemDropAPI.AddDrops(ItemDropLocation.HealingChest, healingSelections);

            ItemDropAPI.AddDrops(ItemDropLocation.Lockbox, lockboxSelections);
            ItemDropAPI.AddDrops(ItemDropLocation.SmallChest, chestSelections);
            ItemDropAPI.AddDrops(ItemDropLocation.MediumChest, t2.ToSelection(ItemDropAPI.DefaultMediumChestTier2DropChance), t3.ToSelection(ItemDropAPI.DefaultMediumChestTier3DropChance));
            ItemDropAPI.AddDrops(ItemDropLocation.LargeChest, t3.ToSelection(ItemDropAPI.DefaultLargeChestTier3DropChance));
        }

        public static void AddEquipmentChestDefaultDrops() {
            var eq = ItemDropAPI.GetDefaultEquipmentDropList();

            ItemDropAPI.AddDrops(ItemDropLocation.EquipmentChest, eq.ToSelection());
        }

        public static void AddLunarChestDefaultDrops() {
            var lun = ItemDropAPI.GetDefaultLunarDropList();
            ItemDropAPI.AddDrops(ItemDropLocation.LunarChest, lun.ToSelection());
        }

        public static void AddBossDefaultDrops() {
            ItemDropAPI.IncludeSpecialBossDrops = true;

            var t2 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier2);

            ItemDropAPI.AddDrops(ItemDropLocation.Boss, t2.ToSelection());
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

        private const string SmallChest = "Chest1";
        private const string MediumChest = "Chest2";
        private const string LargeChest = "GoldChest";
        private const string LunarChest = "LunarChest";
        private const string Lockbox = "Lockbox";
        private const string UtilityChest = "CategoryChestUtility";
        private const string DamageChest = "CategoryChestDamage";
        private const string HealingChest = "CategoryChestHealing";

        private static readonly Dictionary<string, ItemDropLocation> ChestLookup =
            new Dictionary<string, ItemDropLocation>(StringComparer.OrdinalIgnoreCase) {
                { SmallChest, ItemDropLocation.SmallChest },
                { MediumChest, ItemDropLocation.MediumChest },
                { LargeChest, ItemDropLocation.LargeChest },
                { LunarChest, ItemDropLocation.LunarChest },
                { UtilityChest, ItemDropLocation.UtilityChest },
                { DamageChest, ItemDropLocation.DamageChest },
                { HealingChest, ItemDropLocation.HealingChest }
            };

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

        public static bool DefaultDrops { get; set; } = true;

        public static Dictionary<ItemDropLocation, List<PickupSelection>> Selection { get; set; } =
            new Dictionary<ItemDropLocation, List<PickupSelection>>();

        private static readonly Dictionary<ItemTier, List<ItemIndex>> AdditionalTierItems = new Dictionary<ItemTier, List<ItemIndex>> {
            { ItemTier.Tier1, new List<ItemIndex>() },
            { ItemTier.Tier2, new List<ItemIndex>() },
            { ItemTier.Tier3, new List<ItemIndex>() },
            { ItemTier.Boss, new List<ItemIndex>() },
            { ItemTier.Lunar, new List<ItemIndex>() }
        };

        private static readonly List<EquipmentIndex> AdditionalEquipment = new List<EquipmentIndex>();

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            IL.RoR2.BossGroup.DropRewards += DropRewards;
            On.RoR2.ChestBehavior.RollItem += ChestBehaviorOnRollItem;
            IL.RoR2.ShrineChanceBehavior.AddShrineStack += ShrineChanceBehaviorOnAddShrineStack;
            On.RoR2.Run.BuildDropTable += RunOnBuildDropTable;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            IL.RoR2.BossGroup.DropRewards -= DropRewards;
            On.RoR2.ChestBehavior.RollItem -= ChestBehaviorOnRollItem;
            IL.RoR2.ShrineChanceBehavior.AddShrineStack -= ShrineChanceBehaviorOnAddShrineStack;
            On.RoR2.Run.BuildDropTable -= RunOnBuildDropTable;
        }

        private static void RunOnBuildDropTable(On.RoR2.Run.orig_BuildDropTable orig, Run self) {
            if (DefaultDrops) {
                // Setup default item lists
                DefaultItemDrops.AddDefaults();
            }
            // These lists should be replaced soon.
            self.availableTier1DropList.Clear();
            self.availableTier1DropList.AddRange(GetDefaultDropList(ItemTier.Tier1).Select(x => PickupCatalog.FindPickupIndex(x))
                .ToList());

            self.availableTier2DropList.Clear();
            self.availableTier2DropList.AddRange(GetDefaultDropList(ItemTier.Tier2).Select(x => PickupCatalog.FindPickupIndex(x))
                .ToList());

            self.availableTier3DropList.Clear();
            self.availableTier3DropList.AddRange(GetDefaultDropList(ItemTier.Tier3).Select(x => PickupCatalog.FindPickupIndex(x))
                .ToList());

            self.availableEquipmentDropList.Clear();
            self.availableEquipmentDropList.AddRange(GetDefaultEquipmentDropList().Select(x => PickupCatalog.FindPickupIndex(x))
                .ToList());

            self.availableLunarDropList.Clear();
            self.availableLunarDropList.AddRange(GetDefaultLunarDropList());

            self.smallChestDropTierSelector.Clear();
            self.smallChestDropTierSelector.AddChoice(self.availableTier1DropList, DefaultSmallChestTier1SelectorDropChance);
            self.smallChestDropTierSelector.AddChoice(self.availableTier2DropList, DefaultSmallChestTier2SelectorDropChance);
            self.smallChestDropTierSelector.AddChoice(self.availableTier3DropList, DefaultSmallChestTier3SelectorDropChance);
            self.mediumChestDropTierSelector.Clear();
            self.mediumChestDropTierSelector.AddChoice(self.availableTier2DropList, DefaultMediumChestTier1SelectorDropChance);
            self.mediumChestDropTierSelector.AddChoice(self.availableTier3DropList, DefaultMediumChestTier2SelectorDropChance);
            self.largeChestDropTierSelector.Clear();
        }

        private static void ChestBehaviorOnRollItem(On.RoR2.ChestBehavior.orig_RollItem orig, RoR2.ChestBehavior self) {
            if (!NetworkServer.active) {
                Debug.LogWarning("[Server] function 'System.Void RoR2.ChestBehavior::RollItem()' called on client");
                return;
            }

            if (self.GetFieldValue<PickupIndex>("dropPickup") != PickupIndex.none) {
                return;
            }

            var chestName = self.name.Replace("(Clone)", "").Trim();
            Logger.LogDebug($"Dropping {self.name} - {self.requiredItemTag}");


            if (ChestLookup.ContainsKey(chestName)) {

                self.SetFieldValue("dropPickup", GetSelection(ChestLookup[chestName],
                    Run.instance.treasureRng.nextNormalizedFloat));
            } else if (self.name.ToLower().Contains(Lockbox.ToLower())) {
                Logger.LogDebug("Dropping Lockbox");

                var lockboxes = CharacterMaster.readOnlyInstancesList.Sum(x => x.inventory.GetItemCount(ItemIndex.TreasureCache));
                Selection[ItemDropLocation.Lockbox][1].DropChance = DefaultSmallChestTier2DropChance * lockboxes;
                Selection[ItemDropLocation.Lockbox][2].DropChance = DefaultSmallChestTier3DropChance * Mathf.Pow(lockboxes, 2f);
                self.SetFieldValue("dropPickup", GetSelection(ItemDropLocation.Lockbox,
                    Run.instance.treasureRng.nextNormalizedFloat));
            } else {
                Logger.LogError($"Unidentified chest type: {self.name}. Dropping normal loot instead.");

                self.SetFieldValue("dropPickup", GetSelection(ItemDropLocation.SmallChest,
                    Run.instance.treasureRng.nextNormalizedFloat));
            }
        }

        private static void ShrineChanceBehaviorOnAddShrineStack(ILContext il) {
            var cursor = new ILCursor(il).Goto(0);

            cursor.GotoNext(x => x.MatchCallvirt(typeof(WeightedSelection<PickupIndex>).GetMethodCached("Evaluate")));
            cursor.Next.OpCode = OpCodes.Nop;
            cursor.Next.Operand = null;
            cursor.EmitDelegate<Func<WeightedSelection<PickupIndex>, float, PickupIndex>>((_, x) =>
                GetSelection(ItemDropLocation.Shrine, x));
        }

        private static void DropRewards(ILContext il) {
            var cursor = new ILCursor(il).Goto(0);

            cursor.GotoNext(MoveType.After, x => x.MatchCallvirt(typeof(Run).GetMethod("get_participatingPlayerCount")));
            cursor.Index += 1;

            cursor.EmitDelegate<Func<int>>(() => BossDropParticipatingPlayerCount ?? Run.instance.participatingPlayerCount);

            cursor.Emit(OpCodes.Stloc_0);

            cursor.GotoNext(x => x.MatchCall(typeof(PickupIndex).GetMethodCached("get_itemIndex")));

            var itemIndex = Reflection.ReadLocalIndex(cursor.Next.Next.OpCode, cursor.Next.Next.Operand);

            cursor.GotoNext(x => x.MatchCall(typeof(PickupIndex).GetConstructorCached(new[] { typeof(ItemIndex) })));
            cursor.GotoPrev(x => x.OpCode == OpCodes.Ldloca_S);

            var pickupIndex = (VariableDefinition)cursor.Next.Operand;

            cursor.Goto(0);

            cursor.GotoNext(x => x.MatchStloc(itemIndex));
            cursor.Emit(OpCodes.Stloc_S, itemIndex);

            
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Dup);
            cursor.Emit(OpCodes.Ldfld, typeof(BossGroup).GetFieldCached("rng"));
            cursor.EmitDelegate<Func<BossGroup, Xoroshiro128Plus, PickupIndex>>((self, rng) => {
                var norm = rng.nextNormalizedFloat;

                if (self.forceTier3Reward) {
                    var t3List = ItemDropAPI.GetDefaultDropList(ItemTier.Tier3);
                    var selection = t3List.ToSelection();
                    return GetSelection(new List<PickupSelection> { selection }, norm);
                } else {
                    return GetSelection(ItemDropLocation.Boss, norm);
                }
            });

            cursor.Emit(OpCodes.Stloc_S, pickupIndex);
            cursor.Emit(OpCodes.Ldloca_S, pickupIndex);
            cursor.Emit(OpCodes.Call, typeof(PickupIndex).GetMethodCached("get_itemIndex"));
        }

        public static void AddDrops(ItemDropLocation dropLocation, PickupSelection pickups) {
            if (!Selection.ContainsKey(dropLocation)) {
                Selection[dropLocation] = new List<PickupSelection>();
            }
            Selection[dropLocation].Add(pickups);
        }

        public static void AddDrops(ItemDropLocation dropLocation, params PickupSelection[] pickups) {
            if (!Selection.ContainsKey(dropLocation)) {
                Selection[dropLocation] = new List<PickupSelection>();
            }
            Selection[dropLocation].AddRange(pickups);
        }

        public static void AddToDefaultByTier(ItemTier itemTier, params ItemIndex[] items) {
            if (itemTier == ItemTier.NoTier) {
                return;
            }

            AdditionalTierItems[itemTier].AddRange(items);
        }

        public static void AddToDefaultEquipment(params EquipmentIndex[] equipment) {
            AdditionalEquipment.AddRange(equipment);
        }

        public static void ReplaceDrops(ItemDropLocation dropLocation,
            params PickupSelection[] pickupSelections) {
            Logger.LogInfo(
                $"Adding drop information for {dropLocation.ToString()}: {pickupSelections.Sum(x => x.Pickups.Count)} items");

            Selection[dropLocation] = pickupSelections.ToList();
        }

        public static void ReplaceDrops(ItemDropLocation dropLocation, List<PickupSelection> pickupSelections) {
            Logger.LogInfo(
                $"Adding drop information for {dropLocation.ToString()}: {pickupSelections.Sum(x => x.Pickups.Count)} items");

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
                if (!Run.instance.availableItems.HasItem(itemIndex))
                    continue;

                if (ItemCatalog.GetItemDef(itemIndex).tier == itemTier) {
                    list.Add(itemIndex);
                }
            }

            list.AddRange(AdditionalTierItems[itemTier]);
            return list;
        }


        public static List<ItemIndex> GetDefaultDropList(ItemTier itemTier, ItemTag requiredTag) {
            var list = new List<ItemIndex>();

            for (var itemIndex = ItemIndex.Syringe; itemIndex < ItemIndex.Count; itemIndex++) {
                if (!Run.instance.availableItems.HasItem(itemIndex))
                    continue;

                var itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (itemDef.tier == itemTier && itemDef.ContainsTag(requiredTag)) {
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
                if (!Run.instance.availableEquipment.HasEquipment(equipmentIndex))
                    continue;

                var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                if (equipmentDef.canDrop && equipmentDef.isLunar) {
                    list.Add(PickupCatalog.FindPickupIndex(equipmentIndex));
                }
            }

            for (var itemIndex = ItemIndex.Syringe;
                itemIndex < ItemIndex.Count;
                itemIndex++) {
                if (!Run.instance.availableItems.HasItem(itemIndex))
                    continue;

                if (ItemCatalog.GetItemDef(itemIndex).tier == ItemTier.Lunar) {
                    list.Add(PickupCatalog.FindPickupIndex(itemIndex));
                }
            }

            list.AddRange(AdditionalTierItems[ItemTier.Lunar].Select(x => PickupCatalog.FindPickupIndex(x)));
            return list;
        }

        public static List<EquipmentIndex> GetDefaultEquipmentDropList() {
            var list = new List<EquipmentIndex>();

            for (var equipmentIndex = EquipmentIndex.CommandMissile;
                equipmentIndex < EquipmentIndex.Count;
                equipmentIndex++) {
                if (!Run.instance.availableEquipment.HasEquipment(equipmentIndex))
                    continue;

                var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                if (equipmentDef.canDrop && !equipmentDef.isLunar) {
                    list.Add(equipmentIndex);
                }
            }

            list.AddRange(AdditionalEquipment);
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
