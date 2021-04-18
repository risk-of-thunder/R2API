using MonoMod.Cil;
using R2API.ItemDrop;
using R2API.ItemDropAPITools;
using R2API.Utils;
using RoR2;
using RoR2.Artifacts;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace R2API {

    [R2APISubmodule]
    public class MonsterItemsAPI {
        /*
            This class has two purposes.
            The first is the front end that allows mods to register which items should be added and removed from the original drop lists for monsters.
            The second is to contain all the hooks required for the game to utilize these modified drop lists without causing errors.

            Monsters are granted items based on the drop lists in Run.
            This module assumes that the drop lists in Run are correct for items that should be dropped for players, but not necessarily right for the items granted to monsters.
            To avoid a lot of illegible IL, because monsters are granted items much more inrefrequently than players, the drop lists in Run are overwritten when monsters are granted an item and then reverted.
        */

        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        //  ----------------------------------------------------------------------------------------------------
        //  ----------------------------------------------------------------------------------------------------
        //  This secion is my attempt at the api's front end.

        /*
            These are the modifications that are going to be performed to the original drop lists.
            These lists are cleared after they are used to generate the drop lists so subsequent runs can be varied.
            Pickups in the PickupsSpecial lists will be added to the drop lists, ignoring if they are blacklisted.
            This will allow mods to easily add blacklisted pickups for the scavenger, for instance.
        */
        private static List<PickupIndex> PickupsToAdd = new List<PickupIndex>();
        private static List<PickupIndex> PickupsToRemove = new List<PickupIndex>();
        private static List<PickupIndex> PickupsSpecialToAdd = new List<PickupIndex>();

        //  This will add a pickup to the appropriate vanilla drop lists
        public static void AddPickup(PickupIndex pickupIndex, bool special = false) {
            if (pickupIndex != PickupIndex.none) {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef.itemIndex != ItemIndex.None || pickupDef.equipmentIndex != EquipmentIndex.None) {
                    List<PickupIndex> addList = PickupsToAdd;
                    List<PickupIndex> removeList = PickupsToRemove;
                    if (special) {
                        addList = PickupsSpecialToAdd;
                        removeList = new List<PickupIndex>();
                    }
                    AlterLists(addList, removeList, pickupIndex);
                }
            }
        }

        //  This will remove a pickup from the appropriate vanilla drop lists
        public static void RemovePickup(PickupIndex pickupIndex) {
            if (pickupIndex != PickupIndex.none) {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef.itemIndex != ItemIndex.None || pickupDef.equipmentIndex != EquipmentIndex.None) {
                    List<PickupIndex> addList = PickupsToRemove;
                    List<PickupIndex> removeList = PickupsToAdd;
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
                alterations = new List<List<PickupIndex>>() { PickupsSpecialToAdd, new List<PickupIndex>() };
            }
            foreach (List<PickupIndex> alterationList in alterations) {
                if (alterationList.Contains(pickupIndex)) {
                    alterationList.Remove(pickupIndex);
                }
            }
        }

        private static List<bool> tierValidScav = new List<bool>();
        private static List<List<PickupIndex>> scavDropList = new List<List<PickupIndex>>();
        private static ItemTier[] _patternAdjusted = new ItemTier[0];
        private static readonly DropList MonsterDropList = new DropList();

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.Run.Start += DropList.OnRunStart;
            RoR2.RoR2Application.onLoad += Catalog.PopulateCatalog;
            On.RoR2.Run.BuildDropTable += RunOnBuildDropTable;
            On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GenerateAvailableItemsSet += GenerateAvailableItemsSet;
            On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.EnsureMonsterTeamItemCount += EnsureMonsterTeamItemCount;
            IL.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GrantMonsterTeamItem += GrantMonsterTeamItem;
            On.EntityStates.ScavMonster.FindItem.OnEnter += OnEnter;
            On.EntityStates.ScavMonster.FindItem.PickupIsNonBlacklistedItem += PickupIsNonBlacklistedItem;
            On.RoR2.ScavengerItemGranter.Start += ScavengerItemGranterStart;
            On.RoR2.Inventory.GiveRandomEquipment += GiveRandomEquipment;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.Run.Start -= DropList.OnRunStart;
            RoR2.RoR2Application.onLoad -= Catalog.PopulateCatalog;
            On.RoR2.Run.BuildDropTable -= RunOnBuildDropTable;
            On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GenerateAvailableItemsSet -= GenerateAvailableItemsSet;
            On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.EnsureMonsterTeamItemCount -= EnsureMonsterTeamItemCount;
            IL.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.GrantMonsterTeamItem -= GrantMonsterTeamItem;
            On.EntityStates.ScavMonster.FindItem.OnEnter -= OnEnter;
            On.EntityStates.ScavMonster.FindItem.PickupIsNonBlacklistedItem -= PickupIsNonBlacklistedItem;
            On.RoR2.ScavengerItemGranter.Start -= ScavengerItemGranterStart;
            On.RoR2.Inventory.GiveRandomEquipment -= GiveRandomEquipment;
        }

        private static void RunOnBuildDropTable(On.RoR2.Run.orig_BuildDropTable orig, Run run) {
            orig(run);
            MonsterDropList.DuplicateDropLists(run);
            MonsterDropList.GenerateDropLists(PickupsToAdd, PickupsToRemove, new List<PickupIndex>(), new List<PickupIndex>());
            ClearPickupOpterations();
        }

        //  Clears add and remove lists
        public static void ClearPickupOpterations() {
            PickupsToAdd.Clear();
            PickupsToRemove.Clear();
            PickupsSpecialToAdd.Clear();
        }

        /*
            This is called once at the start of a run.
            This will generate the final, filtered drop lists that monsters will use.
            Empty subset lists will cause errors in the game.
            Modified drop lists that generate empty subsets will instead be substituted with the orignal drop lists to avoid this error.
            Tiers that generate empty subsets are removed from the monster team item tier pattern.

            Side Note:
            Part of this function potentially belongs in InteractableCalculator, as its purpose is almost identicle.

        */
        private static void GenerateAvailableItemsSet(On.RoR2.Artifacts.MonsterTeamGainsItemsArtifactManager.orig_GenerateAvailableItemsSet orig) {
            orig();

            var forbiddenTags = new List<ItemTag>();
            foreach (var itemTag in MonsterTeamGainsItemsArtifactManager.forbiddenTags) {
                forbiddenTags.Add(itemTag);
            }
            List<List<PickupIndex>> adjustedLists = GetFilteredDropLists(forbiddenTags);
            adjustedLists.RemoveAt(3);
            List<ItemIndex[]> itemIndexLists = new List<ItemIndex[]>();
            foreach (List<PickupIndex> adjustedList in adjustedLists) {
                ItemIndex[] itemIndexList = new ItemIndex[adjustedList.Count];
                for (int listIndex = 0; listIndex < adjustedList.Count; listIndex++) {
                    itemIndexList[listIndex] = PickupCatalog.GetPickupDef(adjustedList[listIndex]).itemIndex;
                }
                itemIndexLists.Add(itemIndexList);
            }
            MonsterTeamGainsItemsArtifactManager.availableTier1Items = itemIndexLists[0];
            MonsterTeamGainsItemsArtifactManager.availableTier2Items = itemIndexLists[1];
            MonsterTeamGainsItemsArtifactManager.availableTier3Items = itemIndexLists[2];
            List<ItemTier> tierList = new List<ItemTier>() { ItemTier.Tier1, ItemTier.Tier2, ItemTier.Tier3 };


            var patternAdjustedList = new List<ItemTier>();
            foreach (var itemTier in MonsterTeamGainsItemsArtifactManager.pattern) {
                patternAdjustedList.Add(itemTier);
            }
            for (int tierIndex = 0; tierIndex < tierList.Count; tierIndex++) {
                if (itemIndexLists[tierIndex].Length == 0) {
                    while (patternAdjustedList.Contains(tierList[tierIndex])) {
                        patternAdjustedList.Remove(tierList[tierIndex]);
                    }
                }
            }
            _patternAdjusted = new ItemTier[patternAdjustedList.Count];
            for (int patternIndex = 0; patternIndex < patternAdjustedList.Count; patternIndex++) {
                _patternAdjusted[patternIndex] = patternAdjustedList[patternIndex];
            }

            tierValidScav.Clear();
            scavDropList = GetFilteredDropLists(new List<ItemTag>() { ItemTag.AIBlacklist });
            List<List<PickupIndex>> originalLists = new List<List<PickupIndex>>() {
                DropList.Tier1DropListOriginal,
                DropList.Tier2DropListOriginal,
                DropList.Tier3DropListOriginal,
                DropList.NormalEquipmentDropListOriginal,
            };
            for (int listIndex = 0; listIndex < adjustedLists.Count; listIndex++) {
                if (scavDropList[listIndex].Count == 0) {
                    scavDropList[listIndex] = originalLists[listIndex];
                    tierValidScav.Add(false);
                } else {
                    tierValidScav.Add(true);
                }
            }
        }

        /*
            This will filter out pickups from the drop lists if they have incompatible tags.
            It will also add in the special pickups which will not be filtered.
        */
        static public List<List<PickupIndex>> GetFilteredDropLists(List<ItemTag> forbiddenTags) {
            List<List<PickupIndex>> originalDropLists = new List<List<PickupIndex>>() {
                MonsterDropList.AvailableTier1DropList,
                MonsterDropList.AvailableTier2DropList,
                MonsterDropList.AvailableTier3DropList,
                MonsterDropList.AvailableEquipmentDropList,
            };
            List<List<PickupIndex>> adjustedDropLists = new List<List<PickupIndex>>() {
            };
            for (int dropListIndex = 0; dropListIndex < 3; dropListIndex++) {
                adjustedDropLists.Add(new List<PickupIndex>());
                if (DropList.IsValidList(originalDropLists[dropListIndex])) {
                    foreach (PickupIndex pickupIndex in originalDropLists[dropListIndex]) {
                        ItemDef itemDef = ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
                        bool valid = true;
                        foreach (ItemTag itemTag in forbiddenTags) {
                            if (itemDef.ContainsTag(itemTag)) {
                                valid = false;
                                break;
                            }
                        }
                        if (valid) {
                            adjustedDropLists[dropListIndex].Add(pickupIndex);
                        }
                    }
                }
            }
            foreach (PickupIndex pickupIndex in PickupsSpecialToAdd) {
                ItemIndex itemIndex = PickupCatalog.GetPickupDef(pickupIndex).itemIndex;
                EquipmentIndex equipmentIndex = PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex;
                if (itemIndex != ItemIndex.None) {
                    ItemTier itemTier = ItemCatalog.GetItemDef(itemIndex).tier;
                    if (itemTier == ItemTier.Tier1) {
                        adjustedDropLists[0].Add(pickupIndex);
                    } else if (itemTier == ItemTier.Tier2) {
                        adjustedDropLists[1].Add(pickupIndex);
                    } else if (itemTier == ItemTier.Tier3) {
                        adjustedDropLists[2].Add(pickupIndex);
                    }
                } else if (equipmentIndex != EquipmentIndex.None) {
                    adjustedDropLists[3].Add(pickupIndex);
                }
            }
            return adjustedDropLists;
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
            if (tierValidScav.Contains(true)) {
                var scavTierChanceBackup = new List<float> {
                    EntityStates.ScavMonster.FindItem.tier1Chance,
                    EntityStates.ScavMonster.FindItem.tier2Chance,
                    EntityStates.ScavMonster.FindItem.tier3Chance
                };

                if (!tierValidScav[0]) {
                    EntityStates.ScavMonster.FindItem.tier1Chance = 0;
                }
                if (!tierValidScav[1]) {
                    EntityStates.ScavMonster.FindItem.tier2Chance = 0;
                }
                if (!tierValidScav[2]) {
                    EntityStates.ScavMonster.FindItem.tier3Chance = 0;
                }

                DropList.SetDropLists(scavDropList[0], scavDropList[1], scavDropList[2], scavDropList[3]);
                orig(findItem);
                DropList.RevertDropLists();

                EntityStates.ScavMonster.FindItem.tier1Chance = scavTierChanceBackup[0];
                EntityStates.ScavMonster.FindItem.tier2Chance = scavTierChanceBackup[1];
                EntityStates.ScavMonster.FindItem.tier3Chance = scavTierChanceBackup[2];
            }
        }

        //  This makes sure no filtering is done by the scav as it has all been done in here already.
        private static bool PickupIsNonBlacklistedItem(On.EntityStates.ScavMonster.FindItem.orig_PickupIsNonBlacklistedItem orig, EntityStates.ScavMonster.FindItem findItem, PickupIndex pickupIndex) {
            return true;
        }

        /*
            This will update how many of each tier of item will be granted to the scav when it spawns.
            The chance for each tier is reverted after the item selection is processed because they are static fields and could potentially need to return to normal on a subsequent run.
        */
        private static void ScavengerItemGranterStart(On.RoR2.ScavengerItemGranter.orig_Start orig, ScavengerItemGranter scavengerItemGranter) {
            var scavTierTypesBackup = new List<int> {
                scavengerItemGranter.tier1Types, scavengerItemGranter.tier2Types, scavengerItemGranter.tier3Types
            };

            if (!tierValidScav[0]) {
                scavengerItemGranter.tier1Types = 0;
            }
            if (!tierValidScav[1]) {
                scavengerItemGranter.tier2Types = 0;
            }
            if (!tierValidScav[2]) {
                scavengerItemGranter.tier3Types = 0;
            }

            DropList.SetDropLists(scavDropList[0], scavDropList[1], scavDropList[2], scavDropList[3]);
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
            if (tierValidScav[3]) {
                orig(inventory);
            }
        }
    }
}
