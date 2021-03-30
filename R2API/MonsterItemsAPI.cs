using System.Collections.Generic;
using System.Linq;
using MonoMod.Cil;
using R2API.ItemDrop;
using R2API.ItemDropAPITools;
using R2API.Utils;
using RoR2;
using RoR2.Artifacts;
using UnityEngine;

namespace R2API {
    [R2APISubmodule]
    public class MonsterItemsAPI {
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }
        private static bool _loaded;

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

        private static System.Reflection.FieldInfo forbiddenTagsInfo = typeof(MonsterTeamGainsItemsArtifactManager).GetField("forbiddenTags", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        private static System.Reflection.FieldInfo patternInfo = typeof(MonsterTeamGainsItemsArtifactManager).GetField("pattern", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

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

        private static void GenerateAvailableItemsSet(On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.orig_GenerateAvailableItemsSet orig) {
            var forbiddenTags = new List<ItemTag>() {
            };
            System.Collections.ICollection forbiddenTagsCollection = (System.Collections.ICollection)forbiddenTagsInfo.GetValue(null);
            foreach (object forbiddenTag in forbiddenTagsCollection) {
                forbiddenTags.Add((ItemTag)forbiddenTag);
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


            var patternAdjustedList = new List<ItemTier>() {
            };
            
            System.Collections.ICollection patternCollection = (System.Collections.ICollection)patternInfo.GetValue(null);
            foreach (object tier in patternCollection) {
                patternAdjustedList.Add((ItemTier)tier);
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

        private static void EnsureMonsterTeamItemCount(On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.orig_EnsureMonsterTeamItemCount orig, int integer) {
            if (_patternAdjusted.Length > 0) {
                orig(integer);
            }
        }

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

        private static void GiveRandomEquipment(On.RoR2.Inventory.orig_GiveRandomEquipment orig, Inventory inventory) {
            if (MonsterDropList.AvailableEquipmentDropList.Count > 0) {
                orig(inventory);
            }
        }

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
