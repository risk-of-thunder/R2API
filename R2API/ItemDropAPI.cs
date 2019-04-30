using System.Collections.Generic;
using System.Linq;
using RoR2;
using Mono.Cecil.Cil;
using System.Reflection;
using MonoMod.Cil;
using System;
using BepInEx.Logging;

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

            var shrineSelections = new List<PickupSelection> {
                new List<ItemIndex> {ItemIndex.None}.ToSelection(ItemDropAPI.DefaultShrineFailureWeight),
                t1.ToSelection(ItemDropAPI.DefaultShrineTier1Weight),
                t2.ToSelection(ItemDropAPI.DefaultShrineTier2Weight),
                t3.ToSelection(ItemDropAPI.DefaultShrineTier3Weight),
                eq.ToSelection(ItemDropAPI.DefaultShrineEquipmentWeight)
            };

            ItemDropAPI.AddDropInformation(ItemDropLocation.Shrine, shrineSelections);
        }

        public static void AddChestDefaultDrops() {
            var t1 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier1);
            var t2 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier2);
            var t3 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier3);

            var chestSelections = new List<PickupSelection> {
                t1.ToSelection(ItemDropAPI.DefaultChestTier1DropChance),
                t2.ToSelection(ItemDropAPI.DefaultChestTier2DropChance),
                t3.ToSelection(ItemDropAPI.DefaultChestTier3DropChance)
            };

            ItemDropAPI.AddDropInformation(ItemDropLocation.SmallChest, chestSelections);
            ItemDropAPI.AddDropInformation(ItemDropLocation.MediumChest, t2.ToSelection(0.8f), t3.ToSelection(0.2f));
            ItemDropAPI.AddDropInformation(ItemDropLocation.LargeChest, t3.ToSelection());
        }

        public static void AddEquipmentChestDefaultDrops() {
            var eq = ItemDropAPI.GetDefaultEquipmentDropList();

            ItemDropAPI.AddDropInformation(ItemDropLocation.EquipmentChest, eq.ToSelection());
        }

        public static void AddLunarChestDefaultDrops() {
            var lun = ItemDropAPI.GetDefaultLunarDropList();
            ItemDropAPI.AddDropInformation(ItemDropLocation.LunarChest, lun.ToSelection());
        }

        public static void AddBossDefaultDrops() {
            ItemDropAPI.IncludeSpecialBossDrops = true;

            var t2 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier2);

            ItemDropAPI.AddDropInformation(ItemDropLocation.Boss, t2.ToSelection());
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
        SmallChestSelector,
        MediumChestSelector,
        LargeChestSelector
    }

    public static class ItemDropAPI {
        public static ManualLogSource Logger = R2API.Logger;

        public static void InitHooks() {
            var itemDropApi_GetSelection = typeof(ItemDropAPI).GetMethod("GetSelection");
            var xoroshiro_GetNextNormalizedFloat = typeof(Xoroshiro128Plus).GetMethod("get_nextNormalizedFloat");

            IL.RoR2.BossGroup.OnCharacterDeathCallback += il => {
                var cursor = new ILCursor(il).Goto(0);

                cursor.GotoNext(x => x.MatchCall(typeof(PickupIndex).GetMethod("get_itemIndex")));

                var itemIndex = (VariableDefinition) cursor.Next.Next.Operand;

                cursor.Goto(0);
                cursor.GotoNext(x => x.MatchCallvirt(typeof(List<PickupIndex>).GetMethod("get_Item")));

                var pickupIndex = (VariableDefinition) cursor.Next.Next.Operand;

                cursor.GotoNext(MoveType.Before, x => x.MatchStloc(itemIndex.Index));
                cursor.Emit(OpCodes.Stloc_S, itemIndex);

                cursor.Emit(OpCodes.Ldc_I4_0);

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld,
                    typeof(BossGroup).GetField("rng", BindingFlags.NonPublic | BindingFlags.Instance));
                cursor.Emit(OpCodes.Callvirt, xoroshiro_GetNextNormalizedFloat);
                cursor.Emit(OpCodes.Call, itemDropApi_GetSelection);
                cursor.Emit(OpCodes.Stloc_S, pickupIndex);
                cursor.Emit(OpCodes.Ldloca_S, pickupIndex);

                cursor.Emit(OpCodes.Call, typeof(PickupIndex).GetMethod("get_itemIndex"));
            };

            var dropPickup =
                typeof(ChestBehavior).GetField("dropPickup", BindingFlags.NonPublic | BindingFlags.Instance);
            var lunarChance =
                typeof(ChestBehavior).GetField("lunarChance", BindingFlags.Public | BindingFlags.Instance);
            var tier1Chance =
                typeof(ChestBehavior).GetField("tier1Chance", BindingFlags.Public | BindingFlags.Instance);
            var tier2Chance =
                typeof(ChestBehavior).GetField("tier2Chance", BindingFlags.Public | BindingFlags.Instance);
            var tier3Chance =
                typeof(ChestBehavior).GetField("tier3Chance", BindingFlags.Public | BindingFlags.Instance);

            IL.RoR2.ChestBehavior.RollItem += il => {
                var cursor = new ILCursor(il).Goto(0);
                cursor.GotoNext(x => x.MatchLdcI4(8));

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<ChestBehavior>>(@this => {
                    if ((PickupIndex) dropPickup.GetValue(@this) != PickupIndex.none) {
                        return;
                    }

                    if ((float) lunarChance.GetValue(@this) >= 1f) {
                        @this.dropPickup = GetSelection(ItemDropLocation.LunarChest,
                            Run.instance.treasureRng.nextNormalizedFloat);
                    }
                    else if ((float) tier3Chance.GetValue(@this) >= 0.2f) {
                        @this.dropPickup = GetSelection(ItemDropLocation.LargeChest,
                            Run.instance.treasureRng.nextNormalizedFloat);
                    }
                    else if ((float) tier2Chance.GetValue(@this) >= 0.8f) {
                        @this.dropPickup = GetSelection(ItemDropLocation.MediumChest,
                            Run.instance.treasureRng.nextNormalizedFloat);
                    }
                    else if ((float) tier1Chance.GetValue(@this) <= 0.8f) {
                        @this.dropPickup = GetSelection(ItemDropLocation.SmallChest,
                            Run.instance.treasureRng.nextNormalizedFloat);
                    }
                });

                cursor.Emit(OpCodes.Ret);
            };

            var weightedSelection_Evaluate = typeof(WeightedSelection<PickupIndex>).GetMethod("Evaluate");

            IL.RoR2.ShrineChanceBehavior.AddShrineStack += il => {
                var cursor = new ILCursor(il).Goto(0);

                cursor.GotoNext(x => x.MatchCallvirt(weightedSelection_Evaluate));
                cursor.Next.OpCode = OpCodes.Nop;
                cursor.Next.Operand = null;
                cursor.EmitDelegate<Func<WeightedSelection<PickupIndex>, float, PickupIndex>>((_, x) =>
                    GetSelection(ItemDropLocation.Shrine, x));
            };

            On.RoR2.Run.BuildDropTable += (orig, self) => {
                if (DefaultDrops) {
                    // Setup default item lists
                    DefaultItemDrops.AddDefaults();
                }

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
                self.availableLunarDropList.AddRange(GetDefaultLunarDropList().Select(x => new PickupIndex(x))
                    .ToList());

                self.smallChestDropTierSelector.Clear();
                self.smallChestDropTierSelector.AddChoice(self.availableTier1DropList, 0.8f);
                self.smallChestDropTierSelector.AddChoice(self.availableTier2DropList, 0.2f);
                self.smallChestDropTierSelector.AddChoice(self.availableTier3DropList, 0.01f);
                self.mediumChestDropTierSelector.Clear();
                self.mediumChestDropTierSelector.AddChoice(self.availableTier2DropList, 0.8f);
                self.mediumChestDropTierSelector.AddChoice(self.availableTier3DropList, 0.2f);
                self.largeChestDropTierSelector.Clear();
            };
        }

        public static float ChestSpawnRate = 1.0f;
        public static bool IncludeSpecialBossDrops = true;

        public static float DefaultChestTier1DropChance = 0.8f;
        public static float DefaultChestTier2DropChance = 0.2f;
        public static float DefaultChestTier3DropChance = 0.01f;

        public static float DefaultShrineEquipmentWeight = 2f;
        public static float DefaultShrineFailureWeight = 10.1f;
        public static float DefaultShrineTier1Weight = 8f;
        public static float DefaultShrineTier2Weight = 2f;
        public static float DefaultShrineTier3Weight = 0.2f;

        public static float DefaultTier1SelectorDropChance = 0.8f;
        public static float DefaultTier2SelectorDropChance = 0.2f;
        public static float DefaultTier3SelectorDropChance = 0.01f;

        public static bool DefaultDrops { get; set; } = true;

        public static List<ItemIndex> None { get; set; } = new List<ItemIndex> {ItemIndex.None};

        public static Dictionary<ItemDropLocation, List<PickupSelection>> Selection { get; set; } =
            new Dictionary<ItemDropLocation, List<PickupSelection>>();

        public static void AddDropInformation(ItemDropLocation dropLocation,
            params PickupSelection[] pickupSelections) {
            Logger.LogInfo(
                $"Adding drop information for {dropLocation.ToString()}: {pickupSelections.Sum(x => x.Pickups.Count)} items");

            Selection[dropLocation] = pickupSelections.ToList();
        }

        public static void AddDropInformation(ItemDropLocation dropLocation, List<PickupSelection> pickupSelections) {
            Logger.LogInfo(
                $"Adding drop information for {dropLocation.ToString()}: {pickupSelections.Sum(x => x.Pickups.Count)} items");

            Selection[dropLocation] = pickupSelections;
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

        public static List<EquipmentIndex> GetDefaultLunarDropList() {
            var list = new List<EquipmentIndex>();

            for (var equipmentIndex = EquipmentIndex.CommandMissile;
                equipmentIndex < EquipmentIndex.Count;
                equipmentIndex++) {
                if (!Run.instance.availableEquipment.HasEquipment(equipmentIndex))
                    continue;

                var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                if (equipmentDef.canDrop && equipmentDef.isLunar) {
                    list.Add(equipmentIndex);
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
            if (indices == null) {
                return null;
            }

            return new PickupSelection {
                DropChance = dropChance,
                Pickups = indices.Select(x => new PickupIndex(x)).ToList()
            };
        }

        public static PickupSelection ToSelection(this List<EquipmentIndex> indices, float dropChance = 1.0f) {
            if (indices == null) {
                return null;
            }

            return new PickupSelection {
                DropChance = dropChance,
                Pickups = indices.Select(x => new PickupIndex(x)).ToList()
            };
        }
    }
}
