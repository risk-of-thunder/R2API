// TODO: Re-enable nullable after ideath makes PR with refactors here.
#pragma warning disable CS8605 // Unboxing a possibly null value.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using RoR2;
using R2API;
using R2API.Utils;
using R2API.ItemDropAPITools;
using MonoMod.Cil;
using UnityEngine.Events;

namespace R2API {
    [R2APISubmodule]
    public class MonsterItemsAPI {
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }
        private static bool _loaded;

        private static readonly Dictionary<ItemTier, HashSet<ItemIndex>> AdditionalTierItems = new Dictionary<ItemTier, HashSet<ItemIndex>> {
            { ItemTier.Tier1, new HashSet<ItemIndex>() },
            { ItemTier.Tier2, new HashSet<ItemIndex>() },
            { ItemTier.Tier3, new HashSet<ItemIndex>() },
            { ItemTier.Boss, new HashSet<ItemIndex>() },
            { ItemTier.Lunar, new HashSet<ItemIndex>() }
        };

        private static readonly HashSet<EquipmentIndex> AdditionalEquipment = new HashSet<EquipmentIndex>();

        private static Dictionary<ItemTier, List<PickupIndex>> _itemsToAdd = new Dictionary<ItemTier, List<PickupIndex>>() {
            { ItemTier.Tier1, new List<PickupIndex>() },
            { ItemTier.Tier2, new List<PickupIndex>() },
            { ItemTier.Tier3, new List<PickupIndex>() },
            { ItemTier.Boss, new List<PickupIndex>() },
            { ItemTier.Lunar, new List<PickupIndex>() },
            { ItemTier.NoTier, new List<PickupIndex>() },
        };

        private static Dictionary<ItemTier, List<PickupIndex>> _itemsToRemove = new Dictionary<ItemTier, List<PickupIndex>>() {
            { ItemTier.Tier1, new List<PickupIndex>() },
            { ItemTier.Tier2, new List<PickupIndex>() },
            { ItemTier.Tier3, new List<PickupIndex>() },
            { ItemTier.Boss, new List<PickupIndex>() },
            { ItemTier.Lunar, new List<PickupIndex>() },
            { ItemTier.NoTier, new List<PickupIndex>() },
        };

        private static Dictionary<ItemTier, List<PickupIndex>> _equipmentToAdd = new Dictionary<ItemTier, List<PickupIndex>>() {
            { ItemTier.Tier1, new List<PickupIndex>() },
            { ItemTier.Lunar, new List<PickupIndex>() },
            { ItemTier.NoTier, new List<PickupIndex>() },
        };

        private static Dictionary<ItemTier, List<PickupIndex>> _equipmentToRemove = new Dictionary<ItemTier, List<PickupIndex>>() {
            { ItemTier.Tier1, new List<PickupIndex>() },
            { ItemTier.Lunar, new List<PickupIndex>() },
            { ItemTier.NoTier, new List<PickupIndex>() },
        };

        static public Dictionary<ItemTier, List<PickupIndex>> itemsToAdd {
            get { return _itemsToAdd; }
            private set { _itemsToAdd = value; }
        }

        static public Dictionary<ItemTier, List<PickupIndex>> itemsToRemove {
            get { return _itemsToRemove; }
            private set { _itemsToRemove = value; }
        }

        static public Dictionary<ItemTier, List<PickupIndex>> equipmentToAdd {
            get { return _equipmentToAdd; }
            private set { _equipmentToAdd = value; }
        }

        static public Dictionary<ItemTier, List<PickupIndex>> equipmentToRemove {
            get { return _equipmentToRemove; }
            private set { _equipmentToRemove = value; }
        }

        static private Dictionary<ItemTier, bool> tierValidMonsterTeam = new Dictionary<ItemTier, bool>();
        static private Dictionary<ItemTier, bool> tierValidScav = new Dictionary<ItemTier, bool>();
        static private ItemTier[] patternAdjusted = new ItemTier[0];
        static public DropList monsterDropList = new DropList();

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
            Catalogue.PopulateItemCatalogues();
            orig(run);
            monsterDropList.DuplicateDropLists(run);
            monsterDropList.GenerateItems(AdditionalTierItems, AdditionalEquipment, itemsToAdd, itemsToRemove, equipmentToAdd, equipmentToRemove);
        }

        static private void GenerateAvailableItemsSet(On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.orig_GenerateAvailableItemsSet orig) {
            List<ItemTag> forbiddenTags = new List<ItemTag>();
            System.Type type = typeof(RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager);
            System.Reflection.FieldInfo info = type.GetField("forbiddenTags", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            ICollection collection = info.GetValue(null) as ICollection;
            foreach (object itemTag in collection) {
                forbiddenTags.Add((ItemTag)itemTag);
            }

            tierValidScav.Clear();
            tierValidMonsterTeam.Clear();
            List<PickupIndex> tier1Adjusted = monsterDropList.availableTier1DropList;
            tierValidMonsterTeam.Add(ItemTier.Tier1, ListContainsValidItems(forbiddenTags, tier1Adjusted));
            tierValidScav.Add(ItemTier.Tier1, ListContainsValidItems(new List<ItemTag>() { ItemTag.AIBlacklist }, tier1Adjusted));
            if (!tierValidMonsterTeam[ItemTier.Tier1]) {
                tier1Adjusted = DropList.tier1DropListOriginal;
            }
            List<PickupIndex> tier2Adjusted = monsterDropList.availableTier2DropList;
            tierValidMonsterTeam.Add(ItemTier.Tier2, ListContainsValidItems(forbiddenTags, tier2Adjusted));
            tierValidScav.Add(ItemTier.Tier2, ListContainsValidItems(new List<ItemTag>() { ItemTag.AIBlacklist }, tier2Adjusted));
            if (!tierValidMonsterTeam[ItemTier.Tier2]) {
                tier2Adjusted = DropList.tier2DropListOriginal;
            }
            List<PickupIndex> tier3Adjusted = monsterDropList.availableTier3DropList;
            tierValidMonsterTeam.Add(ItemTier.Tier3, ListContainsValidItems(forbiddenTags, tier3Adjusted));
            tierValidScav.Add(ItemTier.Tier3, ListContainsValidItems(new List<ItemTag>() { ItemTag.AIBlacklist }, tier3Adjusted));
            if (!tierValidMonsterTeam[ItemTier.Tier3]) {
                tier3Adjusted = DropList.tier3DropListOriginal;
            }

            DropList.SetDropLists(tier1Adjusted, tier2Adjusted, tier3Adjusted, DropList.equipmentDropListOriginal);
            orig();
            DropList.RevertDropLists();


            info = type.GetField("pattern", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            collection = info.GetValue(null) as ICollection;
            List<ItemTier> patternAdjustedList = new List<ItemTier>();
            int patternIndex = 0;
            foreach (object itemTier in collection) {
                patternAdjustedList.Add((ItemTier)itemTier);
                patternIndex += 1;
            }
            if (!tierValidMonsterTeam[ItemTier.Tier1]) {
                while (patternAdjustedList.Contains(ItemTier.Tier1)) {
                    patternAdjustedList.Remove(ItemTier.Tier1);
                }
            }
            if (!tierValidMonsterTeam[ItemTier.Tier2]) {
                while (patternAdjustedList.Contains(ItemTier.Tier2)) {
                    patternAdjustedList.Remove(ItemTier.Tier2);
                }
            }
            if (!tierValidMonsterTeam[ItemTier.Tier3]) {
                while (patternAdjustedList.Contains(ItemTier.Tier3)) {
                    patternAdjustedList.Remove(ItemTier.Tier3);
                }
            }
            patternAdjusted = new ItemTier[patternAdjustedList.Count];
            patternIndex = 0;
            foreach (ItemTier itemTier in patternAdjustedList) {
                patternAdjusted[patternIndex] = itemTier;
                patternIndex += 1;
            }
        }

        static private void EnsureMonsterTeamItemCount(On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.orig_EnsureMonsterTeamItemCount orig, int integer) {
            if (patternAdjusted.Length > 0) {
                orig(integer);
            }
        }

        static private void GrantMonsterTeamItem(ILContext ilContext) {
            //https://github.com/risk-of-thunder/R2Wiki/wiki/Working-with-IL
            System.Type type = typeof(RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager);
            System.Reflection.FieldInfo pattern = type.GetField("pattern", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            System.Reflection.FieldInfo currentItemIterator = type.GetField("currentItemIterator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);


            ILCursor cursor = new ILCursor(ilContext);
            cursor.GotoNext(
                x => x.MatchLdsfld(pattern),
                x => x.MatchLdsfld(currentItemIterator),
                x => x.MatchDup(),
                x => x.MatchLdcI4(1),
                x => x.MatchAdd(),
                x => x.MatchStsfld(currentItemIterator),
                x => x.MatchLdsfld(pattern),
                x => x.MatchLdlen(),
                x => x.MatchConvI4(),
                x => x.MatchRem(),
                x => x.MatchLdelemI4(),
                x => x.MatchStloc(0)
            );
            cursor.RemoveRange(11);

            cursor.EmitDelegate<System.Func<ItemTier>>(() => {
                int currentItemIteratorValue = int.Parse(currentItemIterator.GetValue(null).ToString());
                ItemTier itemTier = patternAdjusted[currentItemIteratorValue % patternAdjusted.Length];
                currentItemIterator.SetValue(null, currentItemIteratorValue + 1);
                return itemTier;
            });
        }

        static private void OnEnter(On.EntityStates.ScavMonster.FindItem.orig_OnEnter orig, EntityStates.ScavMonster.FindItem findItem) {
            bool valid = false;
            foreach (bool tierValid in tierValidScav.Values) {
                if (tierValid) {
                    valid = true;
                    break;
                }
            }
            if (valid) {
                List<PickupIndex> tier1Adjusted = monsterDropList.availableTier1DropList;
                if (!tierValidMonsterTeam[ItemTier.Tier1]) {
                    tier1Adjusted = DropList.tier1DropListOriginal;
                }
                List<PickupIndex> tier2Adjusted = monsterDropList.availableTier2DropList;
                if (!tierValidMonsterTeam[ItemTier.Tier2]) {
                    tier2Adjusted = DropList.tier2DropListOriginal;
                }
                List<PickupIndex> tier3Adjusted = monsterDropList.availableTier3DropList;
                if (!tierValidScav[ItemTier.Tier3]) {
                    tier3Adjusted = DropList.tier3DropListOriginal;
                }
                DropList.SetDropLists(tier1Adjusted, tier2Adjusted, tier3Adjusted, DropList.equipmentDropListOriginal);

                List<float> scavTierChanceBackup = new List<float>();
                scavTierChanceBackup.Add(EntityStates.ScavMonster.FindItem.tier1Chance);
                scavTierChanceBackup.Add(EntityStates.ScavMonster.FindItem.tier2Chance);
                scavTierChanceBackup.Add(EntityStates.ScavMonster.FindItem.tier3Chance);

                if (!tierValidScav[ItemTier.Tier1]) {
                    EntityStates.ScavMonster.FindItem.tier1Chance = 0;
                }
                if (!tierValidScav[ItemTier.Tier2]) {
                    EntityStates.ScavMonster.FindItem.tier2Chance = 0;
                }
                if (!tierValidScav[ItemTier.Tier3]) {
                    EntityStates.ScavMonster.FindItem.tier3Chance = 0;
                }

                orig(findItem);

                DropList.RevertDropLists();
                EntityStates.ScavMonster.FindItem.tier1Chance = scavTierChanceBackup[0];
                EntityStates.ScavMonster.FindItem.tier2Chance = scavTierChanceBackup[1];
                EntityStates.ScavMonster.FindItem.tier3Chance = scavTierChanceBackup[2];
            }
        }

        static private void ScavengerItemGranterStart(On.RoR2.ScavengerItemGranter.orig_Start orig, ScavengerItemGranter scavengerItemGranter) {
            List<int> scavTierTypesBackup = new List<int>();
            scavTierTypesBackup.Add(scavengerItemGranter.tier1Types);
            scavTierTypesBackup.Add(scavengerItemGranter.tier2Types);
            scavTierTypesBackup.Add(scavengerItemGranter.tier3Types);

            if (!tierValidScav[ItemTier.Tier1]) {
                scavengerItemGranter.tier1Types = 0;
            }
            if (!tierValidScav[ItemTier.Tier2]) {
                scavengerItemGranter.tier2Types = 0;
            }
            if (!tierValidScav[ItemTier.Tier3]) {
                scavengerItemGranter.tier3Types = 0;
            }

            orig(scavengerItemGranter);

            scavengerItemGranter.tier1Types = scavTierTypesBackup[0];
            scavengerItemGranter.tier2Types = scavTierTypesBackup[1];
            scavengerItemGranter.tier3Types = scavTierTypesBackup[2];
        }

        static private void GiveRandomEquipment(On.RoR2.Inventory.orig_GiveRandomEquipment orig, Inventory inventory) {
            if (monsterDropList.availableEquipmentDropList.Count > 0) {
                orig(inventory);
            }
        }

        static public bool ListContainsValidItems(List<ItemTag> forbiddenTags, List<PickupIndex> givenList) {
            foreach (PickupIndex pickupIndex in givenList) {
                bool validItem = true;
                ItemDef itemDef = ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
                foreach (ItemTag itemTag in forbiddenTags) {
                    if (itemDef.ContainsTag(itemTag)) {
                        validItem = false;
                        break;
                    }
                }
                if (validItem) {
                    return true;
                }
            }
            return false;
        }

        public static void AddItemByTier(ItemTier itemTier, ItemIndex itemIndex) {
            if (itemsToAdd.ContainsKey(itemTier)) {
                if (!itemsToAdd[itemTier].Contains(PickupCatalog.FindPickupIndex(itemIndex))) {
                    itemsToAdd[itemTier].Add(PickupCatalog.FindPickupIndex(itemIndex));
                }
            }
        }

        public static void UnaddItemByTier(ItemTier itemTier, ItemIndex itemIndex) {
            if (itemsToAdd.ContainsKey(itemTier)) {
                if (itemsToAdd[itemTier].Contains(PickupCatalog.FindPickupIndex(itemIndex))) {
                    itemsToAdd[itemTier].Remove(PickupCatalog.FindPickupIndex(itemIndex));
                }
            }
        }

        public static void RemoveItemByTier(ItemTier itemTier, ItemIndex itemIndex) {
            if (itemsToRemove.ContainsKey(itemTier)) {
                if (!itemsToRemove[itemTier].Contains(PickupCatalog.FindPickupIndex(itemIndex))) {
                    itemsToRemove[itemTier].Add(PickupCatalog.FindPickupIndex(itemIndex));
                }
            }
        }

        public static void UnremoveItemByTier(ItemTier itemTier, ItemIndex itemIndex) {
            if (itemsToRemove.ContainsKey(itemTier)) {
                if (itemsToRemove[itemTier].Contains(PickupCatalog.FindPickupIndex(itemIndex))) {
                    itemsToRemove[itemTier].Remove(PickupCatalog.FindPickupIndex(itemIndex));
                }
            }
        }

        public static void AddEquipmentByTier(ItemTier itemTier, EquipmentIndex equipmentIndex) {
            if (equipmentToAdd.ContainsKey(itemTier)) {
                if (!equipmentToAdd[itemTier].Contains(PickupCatalog.FindPickupIndex(equipmentIndex))) {
                    equipmentToAdd[itemTier].Add(PickupCatalog.FindPickupIndex(equipmentIndex));
                }
            }
        }


        public static void UnaddEquipmentByTier(ItemTier itemTier, EquipmentIndex equipmentIndex) {
            if (equipmentToAdd.ContainsKey(itemTier)) {
                if (equipmentToAdd[itemTier].Contains(PickupCatalog.FindPickupIndex(equipmentIndex))) {
                    equipmentToAdd[itemTier].Remove(PickupCatalog.FindPickupIndex(equipmentIndex));
                }
            }
        }

        public static void RemoveEquipmentByTier(ItemTier itemTier, EquipmentIndex equipmentIndex) {
            if (equipmentToAdd.ContainsKey(itemTier)) {
                if (!equipmentToRemove[itemTier].Contains(PickupCatalog.FindPickupIndex(equipmentIndex))) {
                    equipmentToRemove[itemTier].Add(PickupCatalog.FindPickupIndex(equipmentIndex));
                }
            }
        }

        public static void UnremoveEquipmentByTier(ItemTier itemTier, EquipmentIndex equipmentIndex) {
            if (equipmentToAdd.ContainsKey(itemTier)) {
                if (equipmentToRemove[itemTier].Contains(PickupCatalog.FindPickupIndex(equipmentIndex))) {
                    equipmentToRemove[itemTier].Remove(PickupCatalog.FindPickupIndex(equipmentIndex));
                }
            }
        }

        public static void AddToDefaultByTier(ItemTier itemTier, params ItemIndex[] items) {
            if (itemTier == ItemTier.NoTier) {
                return;
            }

            AdditionalTierItems[itemTier].UnionWith(items);
        }

        public static void RemoveFromDefaultByTier(ItemTier itemTier, params ItemIndex[] items) {
            if (itemTier == ItemTier.NoTier) {
                return;
            }

            AdditionalTierItems[itemTier].ExceptWith(items);
        }

        public static void AddToDefaultByTier(params KeyValuePair<ItemIndex, ItemTier>[] items) {
            foreach (var list in AdditionalTierItems)
                AddToDefaultByTier(list.Key,
                    items.Where(item => list.Key == item.Value)
                    .Select(item => item.Key)
                    .ToArray());
        }

        public static void RemoveFromDefaultByTier(params KeyValuePair<ItemIndex, ItemTier>[] items) {
            foreach (var list in AdditionalTierItems)
                RemoveFromDefaultByTier(list.Key,
                    items.Where(item => list.Key == item.Value)
                    .Select(item => item.Key)
                    .ToArray());
        }

        public static void AddToDefaultEquipment(params EquipmentIndex[] equipment) {
            AdditionalEquipment.UnionWith(equipment);
        }

        public static void RemoveFromDefaultEquipment(params EquipmentIndex[] equipments) {
            AdditionalEquipment.ExceptWith(equipments);
        }
    }
}
#pragma warning restore CS8605 // Unboxing a possibly null value.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
