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
                t1.ToSelection(ItemDropAPI.DefaultChestTier1DropChance),
                t2.ToSelection(ItemDropAPI.DefaultChestTier2DropChance),
                t3.ToSelection(ItemDropAPI.DefaultChestTier3DropChance)
            };

            ItemDropAPI.AddDrops(ItemDropLocation.SmallChest, chestSelections);
            ItemDropAPI.AddDrops(ItemDropLocation.MediumChest, t2.ToSelection(0.8f), t3.ToSelection(0.2f));
            ItemDropAPI.AddDrops(ItemDropLocation.LargeChest, t3.ToSelection());
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
        Shrine,
        //SmallChestSelector,
        //MediumChestSelector,
        //LargeChestSelector
    }

    // ReSharper disable once InconsistentNaming
    public static class ItemDropAPI {
        public static readonly ManualLogSource Logger = R2API.Logger;

        internal static void InitHooks() {
            var itemDropApi_GetSelection = typeof(ItemDropAPI).GetMethodCached("GetSelection");
            var xoroshiro_GetNextNormalizedFloat = typeof(Xoroshiro128Plus).GetMethodCached("get_nextNormalizedFloat");

            Logger.LogDebug($"{nameof(ItemDropAPI)} - Hook 1");
            IL.RoR2.BossGroup.OnCharacterDeathCallback += il => {
                var cursor = new ILCursor(il).Goto(0);
                cursor.GotoNext(x => x.MatchCall(typeof(PickupIndex).GetMethodCached("get_itemIndex")));

                var itemIndex = (VariableDefinition) cursor.Next.Next.Operand;

                cursor.Goto(0);

                cursor.GotoNext(x => x.MatchCall(typeof(PickupIndex).GetConstructorCached(new[] { typeof(ItemIndex) })));
                cursor.GotoPrev(x => x.OpCode == OpCodes.Ldloca_S);

                var pickupIndex = (VariableDefinition) cursor.Next.Operand;

                cursor.Goto(0);

                cursor.GotoNext(x => x.MatchStloc(itemIndex.Index));
                cursor.Emit(OpCodes.Stloc_S, itemIndex);

                cursor.Emit(OpCodes.Ldc_I4_0);

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, typeof(BossGroup).GetFieldCached("rng"));
                cursor.Emit(OpCodes.Callvirt, xoroshiro_GetNextNormalizedFloat);
                cursor.Emit(OpCodes.Call, itemDropApi_GetSelection);
                cursor.Emit(OpCodes.Stloc_S, pickupIndex);
                cursor.Emit(OpCodes.Ldloca_S, pickupIndex);

                cursor.Emit(OpCodes.Call, typeof(PickupIndex).GetMethodCached("get_itemIndex"));
            };

            On.RoR2.ChestBehavior.RollItem += (orig, self) => {
                if (!NetworkServer.active) {
                    Debug.LogWarning("[Server] function 'System.Void RoR2.ChestBehavior::RollItem()' called on client");
                    return;
                }

                if (self.GetFieldValue<PickupIndex>("dropPickup") != PickupIndex.none) {
                    return;
                }

                if (self.GetFieldValue<float>("lunarChance") >= 1f) {
                    self.SetFieldValue("dropPickup", GetSelection(ItemDropLocation.LunarChest,
                        Run.instance.treasureRng.nextNormalizedFloat));
                } else if (self.GetFieldValue<float>("tier3Chance") >= 0.2f) {
                    self.SetFieldValue("dropPickup", GetSelection(ItemDropLocation.LargeChest,
                        Run.instance.treasureRng.nextNormalizedFloat));
                } else if (self.GetFieldValue<float>("tier2Chance") >= 0.8f) {
                    self.SetFieldValue("dropPickup", GetSelection(ItemDropLocation.MediumChest,
                        Run.instance.treasureRng.nextNormalizedFloat));
                } else if (self.GetFieldValue<float>("tier1Chance") <= 0.8f) {
                    self.SetFieldValue("dropPickup", GetSelection(ItemDropLocation.SmallChest,
                        Run.instance.treasureRng.nextNormalizedFloat));
                }
            };

            var weightedSelection_Evaluate = typeof(WeightedSelection<PickupIndex>).GetMethodCached("Evaluate");

            Logger.LogDebug($"{nameof(ItemDropAPI)} - Hook 3");
            IL.RoR2.ShrineChanceBehavior.AddShrineStack += il => {
                var cursor = new ILCursor(il).Goto(0);

                cursor.GotoNext(x => x.MatchCallvirt(weightedSelection_Evaluate));
                cursor.Next.OpCode = OpCodes.Nop;
                cursor.Next.Operand = null;
                cursor.EmitDelegate<Func<WeightedSelection<PickupIndex>, float, PickupIndex>>((_, x) =>
                    GetSelection(ItemDropLocation.Shrine, x));
            };

            Logger.LogDebug($"{nameof(ItemDropAPI)} - Hook 4");

            On.RoR2.Run.BuildDropTable += (orig, self) => {
                if (DefaultDrops) {
                    // Setup default item lists
                    DefaultItemDrops.AddDefaults();
                }
                // These lists should be replaced soon.
                self.availableTier1DropList.Clear();
                self.availableTier1DropList.AddRange(GetDefaultDropList(ItemTier.Tier1).Select(x => new PickupIndex(x))
                    .ToList());

                self.availableTier2DropList.Clear();
                self.availableTier2DropList.AddRange(GetDefaultDropList(ItemTier.Tier2).Select(x => new PickupIndex(x))
                    .ToList());

                self.availableTier3DropList.Clear();
                self.availableTier3DropList.AddRange(GetDefaultDropList(ItemTier.Tier3).Select(x => new PickupIndex(x))
                    .ToList());

                self.availableEquipmentDropList.Clear();
                self.availableEquipmentDropList.AddRange(GetDefaultEquipmentDropList().Select(x => new PickupIndex(x))
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
            };
        }

        public static bool IncludeSpecialBossDrops = true;

        public static float DefaultChestTier1DropChance = 0.8f;
        public static float DefaultChestTier2DropChance = 0.2f;
        public static float DefaultChestTier3DropChance = 0.01f;

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

        public static PickupIndex GetSelection(ItemDropLocation dropLocation, float normalizedIndex) {
            if (!Selection.ContainsKey(dropLocation))
                return new PickupIndex(ItemIndex.None);

            var selections = Selection[dropLocation];

            var weightedSelection = new WeightedSelection<PickupIndex>();
            foreach (var selection in selections.Where(x => x != null))
            foreach (var pickup in selection.Pickups)
                weightedSelection.AddChoice(pickup, selection.DropChance / selection.Pickups.Count);

            return weightedSelection.Evaluate(normalizedIndex);
        }

        public static List<ItemIndex> GetDefaultDropList(ItemTier itemTier) {
            var list = new List<ItemIndex>();

            for (var itemIndex = ItemIndex.Syringe; itemIndex < ItemIndex.Count; itemIndex++) {
                if (!Run.instance.availableItems.HasItem(itemIndex))
                    continue;

                if (ItemCatalog.GetItemDef(itemIndex).tier == itemTier) {
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
                    list.Add(new PickupIndex(equipmentIndex));
                }
            }

            for (var itemIndex = ItemIndex.Syringe;
                itemIndex < ItemIndex.Count;
                itemIndex++) {
                if (!Run.instance.availableItems.HasItem(itemIndex))
                    continue;

                if (ItemCatalog.GetItemDef(itemIndex).tier == ItemTier.Lunar) {
                    list.Add(new PickupIndex(itemIndex));
                }
            }

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

            return list;
        }

        public static PickupSelection ToSelection(this List<ItemIndex> indices, float dropChance = 1.0f) {
            return indices == null ? null : new PickupSelection {
                DropChance = dropChance,
                Pickups = indices.Select(x => new PickupIndex(x)).ToList()
            };
        }

        public static PickupSelection ToSelection(this List<EquipmentIndex> indices, float dropChance = 1.0f) {
            return indices == null ? null : new PickupSelection {
                DropChance = dropChance,
                Pickups = indices.Select(x => new PickupIndex(x)).ToList()
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
