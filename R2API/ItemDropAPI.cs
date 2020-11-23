using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BepInEx.Logging;

using Mono.Cecil.Cil;

using MonoMod.Cil;

using R2API.ItemDrop;
using R2API.ItemDropAPITools;
using R2API.MiscHelpers;
using R2API.Utils;
using RoR2;
using UnityEngine;

using BF = System.Reflection.BindingFlags;
using EmittedModifer = System.Func<System.Collections.Generic.List<RoR2.PickupIndex>, System.Collections.Generic.List<RoR2.PickupIndex>>;
using CustomModifier = System.Func<System.Collections.Generic.IEnumerable<RoR2.PickupIndex>, System.Collections.Generic.IEnumerable<RoR2.PickupIndex>>;

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

        public static class ChestItems {
            internal static List<PickupIndex> EmitTier1Modifier(List<PickupIndex> current) => current.EditDrops(tier1);
            public static event CustomModifier tier1;
            internal static List<PickupIndex> EmitTier2Modifier(List<PickupIndex> current) => current.EditDrops(tier2);
            public static event CustomModifier tier2;
            internal static List<PickupIndex> EmitTier3Modifier(List<PickupIndex> current) => current.EditDrops(tier3);
            public static event CustomModifier tier3;
            internal static List<PickupIndex> EmitBossModifier(List<PickupIndex> current) => current.EditDrops(boss);
            public static event CustomModifier boss;
            internal static List<PickupIndex> EmitLunarModifier(List<PickupIndex> current) => current.EditDrops(lunar);
            public static event CustomModifier lunar;
            internal static List<PickupIndex> EmitEquipmentModifier(List<PickupIndex> current) => current.EditDrops(equipment);
            public static event CustomModifier equipment;
            internal static List<PickupIndex> EmitNormalEquipmentModifier(List<PickupIndex> current) => current.EditDrops(normalEquipment);
            public static event CustomModifier normalEquipment;
            internal static List<PickupIndex> EmitLunarEquipmentModifier(List<PickupIndex> current) => current.EditDrops(lunarEquipment);
            public static event CustomModifier lunarEquipment;
        }

        public static class EvolutionMonsterItems {
            internal static List<PickupIndex> EmitTier1Modifier(List<PickupIndex> current) => current.EditDrops(tier1);
            public static event CustomModifier tier1;
            internal static List<PickupIndex> EmitTier2Modifier(List<PickupIndex> current) => current.EditDrops(tier2);
            public static event CustomModifier tier2;
            internal static List<PickupIndex> EmitTier3Modifier(List<PickupIndex> current) => current.EditDrops(tier3);
            public static event CustomModifier tier3;
        }

        public static class BossTeleporterRewards {
            internal static List<PickupIndex> EmitTier2Modifier(List<PickupIndex> current) => current.EditDrops(tier2);
            public static event CustomModifier tier2;
            internal static List<PickupIndex> EmitTier3Modifier(List<PickupIndex> current) => current.EditDrops(tier3);
            public static event CustomModifier tier3;
            internal static List<PickupIndex> EmitBossModifier(List<PickupIndex> current) => current.EditDrops(boss, false);
            public static event CustomModifier boss;
        }

        private static List<PickupIndex> EditDrops(this List<PickupIndex> input, CustomModifier? modifiers, Boolean addIfEmpty = true) => (modifiers?.InvokeSequential(input)?.ToList() ?? input).AddEmptyIfNeeded(addIfEmpty);
        private static List<PickupIndex> AddEmptyIfNeeded(this List<PickupIndex> input, Boolean run) {
            if(input.Count == 0 && run) input.Add(PickupIndex.none);
            return input;
        }
        // TODO: Does PickupIndex.none actually work for this?

        

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            IL.RoR2.Run.BuildDropTable += Run_BuildDropTable;
            IL.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GenerateAvailableItemsSet += MonsterTeamGainsItemsArtifactManager_GenerateAvailableItemsSet;
            IL.RoR2.BossGroup.DropRewards += BossGroup_DropRewards;
        }

        

        private static readonly FieldInfo run_smallChestDropTierSelector = typeof(Run).GetField(nameof(Run.smallChestDropTierSelector), BF.Public | BF.NonPublic | BF.Instance);
        private static readonly FieldInfo run_availableTier1DropList = typeof(Run).GetField(nameof(Run.availableTier1DropList), BF.Public | BF.NonPublic | BF.Instance);
        private static readonly FieldInfo run_availableTier2DropList = typeof(Run).GetField(nameof(Run.availableTier2DropList), BF.Public | BF.NonPublic | BF.Instance);
        private static readonly FieldInfo run_availableTier3DropList = typeof(Run).GetField(nameof(Run.availableTier3DropList), BF.Public | BF.NonPublic | BF.Instance);
        private static readonly FieldInfo run_availableLunarDropList = typeof(Run).GetField(nameof(Run.availableLunarDropList), BF.Public | BF.NonPublic | BF.Instance);
        private static readonly FieldInfo run_availableBossDropList = typeof(Run).GetField(nameof(Run.availableBossDropList), BF.Public | BF.NonPublic | BF.Instance);
        private static readonly FieldInfo run_availableEquipmentDropList = typeof(Run).GetField(nameof(Run.availableEquipmentDropList), BF.Public | BF.NonPublic | BF.Instance);
        private static readonly FieldInfo run_availableLunarEquipmentDropList = typeof(Run).GetField(nameof(Run.availableLunarEquipmentDropList), BF.Public | BF.NonPublic | BF.Instance);
        private static readonly FieldInfo run_availableNormalEquipmentDropList = typeof(Run).GetField(nameof(Run.availableNormalEquipmentDropList), BF.Public | BF.NonPublic | BF.Instance);
        private static readonly FieldInfo bossgroup_bossDrops = typeof(BossGroup).GetField(nameof(BossGroup.bossDrops), BF.Public | BF.NonPublic | BF.Instance);

        private static ILCursor EmitModifier(this ILCursor cursor, FieldInfo targetField, EmittedModifer modifier, Boolean isLast = false) => (isLast ? cursor : cursor.Emit(OpCodes.Dup))
            .Emit(OpCodes.Dup)
            .Emit(OpCodes.Ldfld, targetField)
            .EmitDel(modifier)
            .Emit(OpCodes.Stfld, targetField);

        private static void Run_BuildDropTable(ILContext il) => new ILCursor(il)
            .GotoNext(MoveType.AfterLabel,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(run_smallChestDropTierSelector))
            .Emit(OpCodes.Ldarg_0)
            .EmitModifier(run_availableTier1DropList, ChestItems.EmitTier1Modifier)
            .EmitModifier(run_availableTier2DropList, ChestItems.EmitTier2Modifier)
            .EmitModifier(run_availableTier3DropList, ChestItems.EmitTier3Modifier)
            .EmitModifier(run_availableBossDropList, ChestItems.EmitBossModifier)
            .EmitModifier(run_availableLunarDropList, ChestItems.EmitLunarModifier)
            .EmitModifier(run_availableEquipmentDropList, ChestItems.EmitEquipmentModifier)
            .EmitModifier(run_availableNormalEquipmentDropList, ChestItems.EmitNormalEquipmentModifier)
            .EmitModifier(run_availableLunarEquipmentDropList, ChestItems.EmitLunarEquipmentModifier, true);
        //It is fairly easy to also allow edits to the chest rarity distribution here, so potentially expand to include that as well?


        private static void MonsterTeamGainsItemsArtifactManager_GenerateAvailableItemsSet(ILContext il) => new ILCursor(il)
            .GotoNext(MoveType.After,
                x => x.MatchLdfld(run_availableTier1DropList))
            .EmitDel<EmittedModifer>(EvolutionMonsterItems.EmitTier1Modifier)
            .GotoNext(MoveType.After,
                x => x.MatchLdfld(run_availableTier2DropList))
            .EmitDel<EmittedModifer>(EvolutionMonsterItems.EmitTier2Modifier)
            .GotoNext(MoveType.After,
                x => x.MatchLdfld(run_availableTier3DropList))
            .EmitDel<EmittedModifer>(EvolutionMonsterItems.EmitTier3Modifier);


        private static void BossGroup_DropRewards(ILContext il) => new ILCursor(il)
            .Emit(OpCodes.Ldarg_0)
            .EmitModifier(bossgroup_bossDrops, BossTeleporterRewards.EmitBossModifier, true)
            .GotoNext(MoveType.After,
                x => x.MatchLdfld(run_availableTier2DropList))
            .EmitDel<EmittedModifer>(BossTeleporterRewards.EmitTier2Modifier)
            .GotoNext(MoveType.After,
                x => x.MatchLdfld(run_availableTier3DropList))
            .EmitDel<EmittedModifer>(BossTeleporterRewards.EmitTier3Modifier);

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            IL.RoR2.Run.BuildDropTable -= Run_BuildDropTable;
            IL.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GenerateAvailableItemsSet -= MonsterTeamGainsItemsArtifactManager_GenerateAvailableItemsSet;
            IL.RoR2.BossGroup.DropRewards -= BossGroup_DropRewards;
        }


        /// <summary>
        /// Add the given items to the given drop table.
        /// </summary>
        /// <param name="itemTier">The drop table to add the items to.</param>
        /// <param name="items">The item indices to add to the given drop table.</param>
        public static void AddItemByTier(ItemTier itemTier, params ItemIndex[] items) {
            switch(itemTier) {
                case ItemTier.Tier1:
                    ChestItems.tier1 += x => x.Concat(items.Select(PickupCatalog.FindPickupIndex));
                    break;
                case ItemTier.Tier2:
                    ChestItems.tier2 += x => x.Concat(items.Select(PickupCatalog.FindPickupIndex));
                    break;
                case ItemTier.Tier3:
                    ChestItems.tier3 += x => x.Concat(items.Select(PickupCatalog.FindPickupIndex));
                    break;
                case ItemTier.Boss:
                    ChestItems.boss += x => x.Concat(items.Select(PickupCatalog.FindPickupIndex));
                    break;
                case ItemTier.Lunar:
                    ChestItems.lunar += x => x.Concat(items.Select(PickupCatalog.FindPickupIndex));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Remove the given items to the given drop table.
        /// </summary>
        /// <param name="itemTier">The drop table to remove the items from.</param>
        /// <param name="items">The item indices to remove from the given drop table.</param>
        public static void RemoveItemByTier(ItemTier itemTier, params ItemIndex[] items) {
            switch(itemTier) {
                case ItemTier.Tier1:
                    ChestItems.tier1 += x => x.Where(x => !items.Contains(x.pickupDef.itemIndex));
                    break;
                case ItemTier.Tier2:
                    ChestItems.tier2 += x => x.Where(x => !items.Contains(x.pickupDef.itemIndex));
                    break;
                case ItemTier.Tier3:
                    ChestItems.tier3 += x => x.Where(x => !items.Contains(x.pickupDef.itemIndex));
                    break;
                case ItemTier.Boss:
                    ChestItems.boss += x => x.Where(x => !items.Contains(x.pickupDef.itemIndex));
                    break;
                case ItemTier.Lunar:
                    ChestItems.lunar += x => x.Where(x => !items.Contains(x.pickupDef.itemIndex));
                    break;
                default:
                    break;
            }
        }


        [Obsolete]
        /// <summary>
        /// Add the given equipments to the given drop table.
        /// </summary>
        /// <param name="equipmentDropType">The drop table to add the items to.</param>
        /// <param name="equipments">The equipments indices to add to the given drop table.</param>
        public static void AddEquipmentByDropType(EquipmentDropType equipmentDropType, params EquipmentIndex[] equipments) {
            switch(equipmentDropType) {
                case EquipmentDropType.Lunar:
                    ChestItems.lunarEquipment += x => x.Concat(equipments.Select(PickupCatalog.FindPickupIndex));
                    break;
                case EquipmentDropType.Normal:
                    ChestItems.normalEquipment += x => x.Concat(equipments.Select(PickupCatalog.FindPickupIndex));
                    break;
                default:
                    break;
            }
        }

        [Obsolete]
        /// <summary>
        /// Remove the given equipments from the given drop table.
        /// </summary>
        /// <param name="equipmentDropType">The drop table to remove the items from.</param>
        /// <param name="equipments">The equipments indices to remove from the given drop table.</param>
        public static void RemoveEquipmentByDropType(EquipmentDropType equipmentDropType, params EquipmentIndex[] equipments) {
            switch(equipmentDropType) {
                case EquipmentDropType.Lunar:
                    ChestItems.lunarEquipment += x => x.Where(x => !equipments.Contains(x.pickupDef.equipmentIndex));
                    break;
                case EquipmentDropType.Normal:
                    ChestItems.normalEquipment += x => x.Where(x => !equipments.Contains(x.pickupDef.equipmentIndex));
                    break;
                default:
                    break;
            }
        }

        [Obsolete]
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

        [Obsolete]
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

        [Obsolete]
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

        [Obsolete]
        /// <summary>
        /// Remove the given equipments from the drop tables.
        /// </summary>
        /// <param name="equipments">Equipment Indices to remove.</param>
        public static void RemoveEquipment(params EquipmentIndex[] equipments) {
            Boolean Predicate(PickupIndex index) => !equipments.Contains(index.equipmentIndex);
            ChestItems.equipment += x => x.Where(Predicate);
            ChestItems.normalEquipment += x => x.Where(Predicate);
            ChestItems.lunarEquipment += x => x.Where(Predicate);
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
    }
}
