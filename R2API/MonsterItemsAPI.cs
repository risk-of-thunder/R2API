using MonoMod.Cil;
using R2API.ItemDrop;
using R2API.ItemDropAPITools;
using R2API.Utils;
using RoR2;
using RoR2.Artifacts;
using System.Collections.Generic;
using System.Linq;

namespace R2API {

    [R2APISubmodule]
    public class MonsterItemsAPI {
        /*
            This class has two purposes.
            The first is to contain all the hooks required for the game to utilize these modified drop lists without causing errors.
            The second is the front end that allows mods to register which items should be added and removed from the original drop lists for monsters.

            Monsters are granted items based on the drop lists in Run.
            This module assumes that the drop lists in Run are correct for items that should be dropped for players, but not necessarily right for the items granted to monsters.
            To avoid a lot of illegible IL, because monsters are granted items much more inrefrequently than players, the drop lists in Run are overwritten when monsters are granted an item and then reverted.
        */

        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        public static Dictionary<ItemTier, List<ItemIndex>> AdditionalItemsReadOnly =>
            ItemsToAdd.Except(ItemsToRemove).ToDictionary(p => p.Key, p => p.Value);

        public static Dictionary<EquipmentDropType, List<EquipmentIndex>> AdditionalEquipmentsReadOnly =>
            EquipmentsToAdd.Except(EquipmentsToRemove).ToDictionary(p => p.Key, p => p.Value);

        /*
            These are the modifications that are going to be performed to the original drop lists.
            These lists are cleared after they are used to generate the drop lists so subsequent runs can be varied.
            An explanation for how/why they are used is at the start of DropList.
        */
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

        private static readonly Dictionary<ItemTier, bool> TierValidMonsterTeam = new Dictionary<ItemTier, bool>();
        private static readonly Dictionary<ItemTier, bool> TierValidScav = new Dictionary<ItemTier, bool>();
        private static ItemTier[] _patternAdjusted = new ItemTier[0];
        private static readonly DropList MonsterDropList = new DropList();

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.Run.BuildDropTable += RunOnBuildDropTable;
            On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GenerateAvailableItemsSet += GenerateAvailableItemsSet;
            On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.EnsureMonsterTeamItemCount += EnsureMonsterTeamItemCount;
            IL.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GrantMonsterTeamItem += GrantMonsterTeamItem;
            On.EntityStates.ScavMonster.FindItem.OnEnter += OnEnter;
            On.RoR2.ScavengerItemGranter.Start += ScavengerItemGranterStart;
            On.RoR2.Inventory.GiveRandomEquipment += GiveRandomEquipment;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.Run.BuildDropTable -= RunOnBuildDropTable;
            On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GenerateAvailableItemsSet -= GenerateAvailableItemsSet;
            On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.EnsureMonsterTeamItemCount -= EnsureMonsterTeamItemCount;
            IL.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GrantMonsterTeamItem -= GrantMonsterTeamItem;
            On.EntityStates.ScavMonster.FindItem.OnEnter -= OnEnter;
            On.RoR2.ScavengerItemGranter.Start -= ScavengerItemGranterStart;
            On.RoR2.Inventory.GiveRandomEquipment -= GiveRandomEquipment;
        }

        private static void RunOnBuildDropTable(On.RoR2.Run.orig_BuildDropTable orig, Run run) {
            Catalog.PopulateItemCatalog();
            orig(run);
            MonsterDropList.DuplicateDropLists(run);
            MonsterDropList.GenerateDropLists(ItemsToAdd, ItemsToRemove, EquipmentsToAdd, EquipmentsToRemove);
            ItemDropAPI.ClearItemOperations(ItemsToAdd);
            ItemDropAPI.ClearItemOperations(ItemsToRemove);
            ItemDropAPI.ClearEquipmentOperations(EquipmentsToAdd);
            ItemDropAPI.ClearEquipmentOperations(EquipmentsToRemove);
        }

        /*
            This is called once at the start of a run.
            This will determine whether each subset of each tier of item is valid for the different ways that items can be granted to monsters.
            MonsterTeamGainsItemsArtifactManager generates and stores those subset lists at the start of the run to be referenced throughout the run.
            Empty subset lists will cause errors in the game.
            Modified drop lists that generate empty subsets will instead be substituted with the orignal drop lists to avoid this error.
            Tiers that generate empty subsets are removed from the monster team item tier pattern.

            Side Note:
            Part of this function potentially belongs in InteractableCalculator, as its purpose is almost identicle.

        */
        private static void GenerateAvailableItemsSet(On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.orig_GenerateAvailableItemsSet orig) {
            var forbiddenTags = new List<ItemTag>();
            foreach (var itemTag in MonsterTeamGainsItemsArtifactManager.forbiddenTags) {
                forbiddenTags.Add(itemTag);
            }

            TierValidScav.Clear();
            TierValidMonsterTeam.Clear();
            var tier1Adjusted = MonsterDropList.AvailableTier1DropList;
            TierValidMonsterTeam.Add(ItemTier.Tier1, ListContainsValidItems(forbiddenTags, tier1Adjusted));
            TierValidScav.Add(ItemTier.Tier1, ListContainsValidItems(new List<ItemTag> { ItemTag.AIBlacklist }, tier1Adjusted));
            if (!TierValidMonsterTeam[ItemTier.Tier1]) {
                tier1Adjusted = DropList.Tier1DropListOriginal;
            }
            var tier2Adjusted = MonsterDropList.AvailableTier2DropList;
            TierValidMonsterTeam.Add(ItemTier.Tier2, ListContainsValidItems(forbiddenTags, tier2Adjusted));
            TierValidScav.Add(ItemTier.Tier2, ListContainsValidItems(new List<ItemTag> { ItemTag.AIBlacklist }, tier2Adjusted));
            if (!TierValidMonsterTeam[ItemTier.Tier2]) {
                tier2Adjusted = DropList.Tier2DropListOriginal;
            }
            var tier3Adjusted = MonsterDropList.AvailableTier3DropList;
            TierValidMonsterTeam.Add(ItemTier.Tier3, ListContainsValidItems(forbiddenTags, tier3Adjusted));
            TierValidScav.Add(ItemTier.Tier3, ListContainsValidItems(new List<ItemTag> { ItemTag.AIBlacklist }, tier3Adjusted));
            if (!TierValidMonsterTeam[ItemTier.Tier3]) {
                tier3Adjusted = DropList.Tier3DropListOriginal;
            }
            var equipmentAdjusted = MonsterDropList.AvailableEquipmentDropList;
            if (equipmentAdjusted.Count == 0) {
                equipmentAdjusted = DropList.EquipmentDropListOriginal;
            }

            DropList.SetDropLists(tier1Adjusted, tier2Adjusted, tier3Adjusted, equipmentAdjusted);
            orig();
            DropList.RevertDropLists();

            var patternAdjustedList = new List<ItemTier>();
            foreach (var itemTier in MonsterTeamGainsItemsArtifactManager.pattern) {
                patternAdjustedList.Add(itemTier);
            }
            if (!TierValidMonsterTeam[ItemTier.Tier1]) {
                while (patternAdjustedList.Contains(ItemTier.Tier1)) {
                    patternAdjustedList.Remove(ItemTier.Tier1);
                }
            }
            if (!TierValidMonsterTeam[ItemTier.Tier2]) {
                while (patternAdjustedList.Contains(ItemTier.Tier2)) {
                    patternAdjustedList.Remove(ItemTier.Tier2);
                }
            }
            if (!TierValidMonsterTeam[ItemTier.Tier3]) {
                while (patternAdjustedList.Contains(ItemTier.Tier3)) {
                    patternAdjustedList.Remove(ItemTier.Tier3);
                }
            }
            _patternAdjusted = new ItemTier[patternAdjustedList.Count];
            var patternIndex = 0;
            foreach (var itemTier in patternAdjustedList) {
                _patternAdjusted[patternIndex] = itemTier;
                patternIndex += 1;
            }
        }

        //  This is to prevent an error that occurs when the pattern is empty
        private static void EnsureMonsterTeamItemCount(On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.orig_EnsureMonsterTeamItemCount orig, int integer) {
            if (_patternAdjusted.Length > 0) {
                orig(integer);
            }
        }

        /*
            I don't know the right words to say this.
            The monster team item tier pattern is unmodifiable because of something to do with JIT.
            This IL is a workaround to force it to use the adjusted pattern.
        */
        private static void GrantMonsterTeamItem(ILContext ilContext) {
            var type = typeof(MonsterTeamGainsItemsArtifactManager);
            var patternFieldInfo = type.GetField("pattern", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var currentItemIteratorFieldInfo = type.GetField("currentItemIterator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var cursor = new ILCursor(ilContext);
            cursor.GotoNext(
                x => x.MatchLdsfld(patternFieldInfo),
                x => x.MatchLdsfld(currentItemIteratorFieldInfo),
                x => x.MatchDup(),
                x => x.MatchLdcI4(1),
                x => x.MatchAdd(),
                x => x.MatchStsfld(currentItemIteratorFieldInfo),
                x => x.MatchLdsfld(patternFieldInfo),
                x => x.MatchLdlen(),
                x => x.MatchConvI4(),
                x => x.MatchRem(),
                x => x.MatchLdelemI4(),
                x => x.MatchStloc(0)
            );
            cursor.RemoveRange(11);

            cursor.EmitDelegate<System.Func<ItemTier>>(() => {
                var currentItemIteratorValue = int.Parse(currentItemIteratorFieldInfo.GetValue(null).ToString());
                var itemTier = _patternAdjusted[currentItemIteratorValue % _patternAdjusted.Length];
                currentItemIteratorFieldInfo.SetValue(null, currentItemIteratorValue + 1);
                return itemTier;
            });
        }

        /*
            This ensure the scav will have a populated subset list for each tier of item when it chooses an item to avoid an error.
            It will also update the chance for each tier of item being selected.
            The chance for each tier is reverted after the item selection is processed because they are static fields and could potentially need to return to normal on a subsequent run.
        */
        private static void OnEnter(On.EntityStates.ScavMonster.FindItem.orig_OnEnter orig, EntityStates.ScavMonster.FindItem findItem) {
            var valid = false;
            foreach (var tierValid in TierValidScav.Values) {
                if (tierValid) {
                    valid = true;
                    break;
                }
            }
            if (valid) {
                var tier1Adjusted = MonsterDropList.AvailableTier1DropList;
                if (!TierValidMonsterTeam[ItemTier.Tier1]) {
                    tier1Adjusted = DropList.Tier1DropListOriginal;
                }
                var tier2Adjusted = MonsterDropList.AvailableTier2DropList;
                if (!TierValidMonsterTeam[ItemTier.Tier2]) {
                    tier2Adjusted = DropList.Tier2DropListOriginal;
                }
                var tier3Adjusted = MonsterDropList.AvailableTier3DropList;
                if (!TierValidScav[ItemTier.Tier3]) {
                    tier3Adjusted = DropList.Tier3DropListOriginal;
                }
                var equipmentAdjusted = MonsterDropList.AvailableEquipmentDropList;
                if (equipmentAdjusted.Count == 0) {
                    equipmentAdjusted = DropList.EquipmentDropListOriginal;
                }
                DropList.SetDropLists(tier1Adjusted, tier2Adjusted, tier3Adjusted, equipmentAdjusted);

                var scavTierChanceBackup = new List<float> {
                    EntityStates.ScavMonster.FindItem.tier1Chance,
                    EntityStates.ScavMonster.FindItem.tier2Chance,
                    EntityStates.ScavMonster.FindItem.tier3Chance
                };

                if (!TierValidScav[ItemTier.Tier1]) {
                    EntityStates.ScavMonster.FindItem.tier1Chance = 0;
                }
                if (!TierValidScav[ItemTier.Tier2]) {
                    EntityStates.ScavMonster.FindItem.tier2Chance = 0;
                }
                if (!TierValidScav[ItemTier.Tier3]) {
                    EntityStates.ScavMonster.FindItem.tier3Chance = 0;
                }

                orig(findItem);

                DropList.RevertDropLists();
                EntityStates.ScavMonster.FindItem.tier1Chance = scavTierChanceBackup[0];
                EntityStates.ScavMonster.FindItem.tier2Chance = scavTierChanceBackup[1];
                EntityStates.ScavMonster.FindItem.tier3Chance = scavTierChanceBackup[2];
            }
        }

        /*
            This will update how many of each tier of item will be granted to the scav when it spawns.
            The chance for each tier is reverted after the item selection is processed because they are static fields and could potentially need to return to normal on a subsequent run.
        */
        private static void ScavengerItemGranterStart(On.RoR2.ScavengerItemGranter.orig_Start orig, ScavengerItemGranter scavengerItemGranter) {
            var scavTierTypesBackup = new List<int> {
                scavengerItemGranter.tier1Types, scavengerItemGranter.tier2Types, scavengerItemGranter.tier3Types
            };

            if (!TierValidScav[ItemTier.Tier1]) {
                scavengerItemGranter.tier1Types = 0;
            }
            if (!TierValidScav[ItemTier.Tier2]) {
                scavengerItemGranter.tier2Types = 0;
            }
            if (!TierValidScav[ItemTier.Tier3]) {
                scavengerItemGranter.tier3Types = 0;
            }
            DropList.SetDropLists(MonsterDropList.AvailableTier1DropList, MonsterDropList.AvailableTier2DropList, MonsterDropList.AvailableTier3DropList, MonsterDropList.AvailableEquipmentDropList);
            orig(scavengerItemGranter);
            DropList.RevertDropLists();

            scavengerItemGranter.tier1Types = scavTierTypesBackup[0];
            scavengerItemGranter.tier2Types = scavTierTypesBackup[1];
            scavengerItemGranter.tier3Types = scavTierTypesBackup[2];
        }

        /*
            This function is to prevent an error if the equipment drop list for monsters is empty.
            This function is only called to give a scav a piece of equipment when they are first spawned.
            It occurs during the above function and therefore the drop lists have already been set.
        */
        private static void GiveRandomEquipment(On.RoR2.Inventory.orig_GiveRandomEquipment orig, Inventory inventory) {
            if (MonsterDropList.AvailableEquipmentDropList.Count > 0) {
                orig(inventory);
            }
        }

        // This is will determine whether there are any items in a given list that do not contain any of the given forbidden tags.
        public static bool ListContainsValidItems(List<ItemTag> forbiddenTags, List<PickupIndex> givenList) {
            if (DropList.IsValidList(givenList)) {
                foreach (var pickupIndex in givenList) {
                    var validItem = true;
                    var itemDef = ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
                    foreach (var itemTag in forbiddenTags) {
                        if (itemDef.ContainsTag(itemTag)) {
                            validItem = false;
                            break;
                        }
                    }
                    if (validItem) {
                        return true;
                    }
                }
            }
            return false;
        }

        //  ----------------------------------------------------------------------------------------------------
        //  ----------------------------------------------------------------------------------------------------
        //
        //  This point onwards is my attempt at the api's front end. It is not great and Reins is probably better.


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
        /// Add the given equipments to the monster drop tables (mostly scavenger backpack), the api will look up the equipmentDefs from the indices
        /// and add the equipment depending on the information provided from the EquipmentDef (isLunar, isElite, etc)
        /// </summary>
        /// <param name="equipments"></param>
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
        /// <param name="equipments"></param>
        public static void RemoveEquipment(params EquipmentIndex[] equipments) {
            foreach (var equipmentIndex in equipments) {
                var equipmentDropTypes = EquipmentDropTypeUtil.GetEquipmentTypesFromIndex(equipmentIndex);
                foreach (var equipmentDropType in equipmentDropTypes) {
                    RemoveEquipmentByDropType(equipmentDropType, equipmentIndex);
                }
            }
        }
    }
}
