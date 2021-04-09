using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.MiscHelpers;
using R2API.Utils;
using RoR2;

using BF = System.Reflection.BindingFlags;

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
            internal static List<PickupIndex> EmitTier1Modifier(List<PickupIndex> current) => current.EditDrops(Tier1);
            public static event Modifier<IEnumerable<PickupIndex>> Tier1;
            internal static List<PickupIndex> EmitTier2Modifier(List<PickupIndex> current) => current.EditDrops(Tier2);
            public static event Modifier<IEnumerable<PickupIndex>> Tier2;
            internal static List<PickupIndex> EmitTier3Modifier(List<PickupIndex> current) => current.EditDrops(Tier3);
            public static event Modifier<IEnumerable<PickupIndex>> Tier3;
            internal static List<PickupIndex> EmitBossModifier(List<PickupIndex> current) => current.EditDrops(Boss);
            public static event Modifier<IEnumerable<PickupIndex>> Boss;
            internal static List<PickupIndex> EmitLunarModifier(List<PickupIndex> current) => current.EditDrops(Lunar);
            public static event Modifier<IEnumerable<PickupIndex>> Lunar;
            internal static List<PickupIndex> EmitEquipmentModifier(List<PickupIndex> current) => current.EditDrops(Equipment);
            public static event Modifier<IEnumerable<PickupIndex>> Equipment;
            internal static List<PickupIndex> EmitNormalEquipmentModifier(List<PickupIndex> current) => current.EditDrops(NormalEquipment);
            public static event Modifier<IEnumerable<PickupIndex>> NormalEquipment;
            internal static List<PickupIndex> EmitLunarEquipmentModifier(List<PickupIndex> current) => current.EditDrops(LunarEquipment);
            public static event Modifier<IEnumerable<PickupIndex>> LunarEquipment;
        }

        public static class EvolutionMonsterItems {
            internal static List<PickupIndex> EmitTier1Modifier(List<PickupIndex> current) => current.EditDrops(Tier1);
            public static event Modifier<IEnumerable<PickupIndex>> Tier1;
            internal static List<PickupIndex> EmitTier2Modifier(List<PickupIndex> current) => current.EditDrops(Tier2);
            public static event Modifier<IEnumerable<PickupIndex>> Tier2;
            internal static List<PickupIndex> EmitTier3Modifier(List<PickupIndex> current) => current.EditDrops(Tier3);
            public static event Modifier<IEnumerable<PickupIndex>> Tier3;
        }

        public static class BossTeleporterRewards {
            internal static List<PickupIndex> EmitTier2Modifier(List<PickupIndex> current) => current.EditDrops(Tier2);
            public static event Modifier<IEnumerable<PickupIndex>> Tier2;
            internal static List<PickupIndex> EmitTier3Modifier(List<PickupIndex> current) => current.EditDrops(Tier3);
            public static event Modifier<IEnumerable<PickupIndex>> Tier3;
            internal static List<PickupIndex> EmitBossModifier(List<PickupIndex> current) => current.EditDrops(Boss, false);
            public static event Modifier<IEnumerable<PickupIndex>> Boss;
        }

        private static List<PickupIndex> EditDrops(this List<PickupIndex> input, Modifier<IEnumerable<PickupIndex>>? modifiers, bool addIfEmpty = true) =>
            (modifiers?.InvokeSequential(input, true)?.ToList() ?? input).AddEmptyIfNeeded(addIfEmpty);

        private static List<PickupIndex> AddEmptyIfNeeded(this List<PickupIndex> input, bool run) {
            if (input.Count == 0 && run) input.Add(PickupIndex.none);
            return input;
        }

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            IL.RoR2.Run.BuildDropTable += Run_BuildDropTable;
            IL.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GenerateAvailableItemsSet += MonsterTeamGainsItemsArtifactManager_GenerateAvailableItemsSet;
            IL.RoR2.BossGroup.DropRewards += BossGroup_DropRewards;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            IL.RoR2.Run.BuildDropTable -= Run_BuildDropTable;
            IL.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GenerateAvailableItemsSet -= MonsterTeamGainsItemsArtifactManager_GenerateAvailableItemsSet;
            IL.RoR2.BossGroup.DropRewards -= BossGroup_DropRewards;
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

        private static ILCursor EmitModifier(this ILCursor cursor, FieldInfo targetField, Modifier<List<PickupIndex>> modifier, bool isLast = false) => (isLast ? cursor : cursor.Emit(OpCodes.Dup))
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
            .EmitDel<Modifier<List<PickupIndex>>>(EvolutionMonsterItems.EmitTier1Modifier)
            .GotoNext(MoveType.After,
                x => x.MatchLdfld(run_availableTier2DropList))
            .EmitDel<Modifier<List<PickupIndex>>>(EvolutionMonsterItems.EmitTier2Modifier)
            .GotoNext(MoveType.After,
                x => x.MatchLdfld(run_availableTier3DropList))
            .EmitDel<Modifier<List<PickupIndex>>>(EvolutionMonsterItems.EmitTier3Modifier);

        private static void BossGroup_DropRewards(ILContext il) => new ILCursor(il)
            .Emit(OpCodes.Ldarg_0)
            .EmitModifier(bossgroup_bossDrops, BossTeleporterRewards.EmitBossModifier, true)
            .GotoNext(MoveType.After,
                x => x.MatchLdfld(run_availableTier2DropList))
            .EmitDel<Modifier<List<PickupIndex>>>(BossTeleporterRewards.EmitTier2Modifier)
            .GotoNext(MoveType.After,
                x => x.MatchLdfld(run_availableTier3DropList))
            .EmitDel<Modifier<List<PickupIndex>>>(BossTeleporterRewards.EmitTier3Modifier);

        /// <summary>
        /// Add the given items to the given drop table.
        /// </summary>
        /// <param name="itemTier">The drop table to add the items to.</param>
        /// <param name="items">The item indices to add to the given drop table.</param>
        public static void AddItemByTier(ItemTier itemTier, params ItemIndex[] items) {
            switch (itemTier) {
                case ItemTier.Tier1:
                    ChestItems.Tier1 += x => x.Concat(items.Select(PickupCatalog.FindPickupIndex));
                    break;
                case ItemTier.Tier2:
                    ChestItems.Tier2 += x => x.Concat(items.Select(PickupCatalog.FindPickupIndex));
                    break;
                case ItemTier.Tier3:
                    ChestItems.Tier3 += x => x.Concat(items.Select(PickupCatalog.FindPickupIndex));
                    break;
                case ItemTier.Boss:
                    ChestItems.Boss += x => x.Concat(items.Select(PickupCatalog.FindPickupIndex));
                    break;
                case ItemTier.Lunar:
                    ChestItems.Lunar += x => x.Concat(items.Select(PickupCatalog.FindPickupIndex));
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
            switch (itemTier) {
                case ItemTier.Tier1:
                    ChestItems.Tier1 += x => x.Where(x => !items.Contains(x.pickupDef.itemIndex));
                    break;
                case ItemTier.Tier2:
                    ChestItems.Tier2 += x => x.Where(x => !items.Contains(x.pickupDef.itemIndex));
                    break;
                case ItemTier.Tier3:
                    ChestItems.Tier3 += x => x.Where(x => !items.Contains(x.pickupDef.itemIndex));
                    break;
                case ItemTier.Boss:
                    ChestItems.Boss += x => x.Where(x => !items.Contains(x.pickupDef.itemIndex));
                    break;
                case ItemTier.Lunar:
                    ChestItems.Lunar += x => x.Where(x => !items.Contains(x.pickupDef.itemIndex));
                    break;
                default:
                    break;
            }
        }
    }
}
