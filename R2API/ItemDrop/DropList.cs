using R2API.ItemDrop;
using RoR2;
using System.Collections.Generic;
using System.Linq;

namespace R2API {

    namespace ItemDropAPITools {

        public class DropList {
            public static bool OriginalListsSaved;
            public static List<PickupIndex> Tier1DropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> Tier2DropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> Tier3DropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> BossDropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> LunarDropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> EquipmentDropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> NormalEquipmentDropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> LunarEquipmentDropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> SpecialItemsOriginal = new List<PickupIndex>();
            public static List<PickupIndex> SpecialEquipmentOriginal = new List<PickupIndex>();

            private static List<PickupIndex> Tier1DropListBackup = new List<PickupIndex>();
            private static List<PickupIndex> Tier2DropListBackup = new List<PickupIndex>();
            private static List<PickupIndex> Tier3DropListBackup = new List<PickupIndex>();
            private static List<PickupIndex> EquipmentDropListBackup = new List<PickupIndex>();

            public List<PickupIndex> AvailableTier1DropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableTier2DropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableTier3DropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableBossDropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableLunarDropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableEquipmentDropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableNormalEquipmentDropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableLunarEquipmentDropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableSpecialItems = new List<PickupIndex>();
            public List<PickupIndex> AvailableSpecialEquipment = new List<PickupIndex>();

            public const string NullIconTextureName = "texNullIcon";

            public List<PickupIndex> GetDropList(ItemTier itemTier) {
                if (itemTier == ItemTier.Tier1) {
                    return AvailableTier1DropList;
                }
                else if (itemTier == ItemTier.Tier2) {
                    return AvailableTier2DropList;
                }
                else if (itemTier == ItemTier.Tier3) {
                    return AvailableTier3DropList;
                }
                else if (itemTier == ItemTier.Boss) {
                    return AvailableBossDropList;
                }
                else if (itemTier == ItemTier.Lunar) {
                    return AvailableLunarDropList;
                }
                else {
                    return AvailableNormalEquipmentDropList;
                }
            }

            private static List<PickupIndex> BackupDropList(IEnumerable<PickupIndex> list) {
                var pickupIndices = list.ToList();
                return pickupIndices.Any() ? pickupIndices.ToList() : new List<PickupIndex> { PickupIndex.none };
            }

            public static void SetDropLists(IEnumerable<PickupIndex> givenTier1, IEnumerable<PickupIndex> givenTier2,
                IEnumerable<PickupIndex> givenTier3, IEnumerable<PickupIndex> givenEquipment) {
                var availableItems = new List<List<PickupIndex>> {
                    BackupDropList(givenTier1),
                    BackupDropList(givenTier2),
                    BackupDropList(givenTier3),
                    BackupDropList(givenEquipment)
                };

                var run = Run.instance;
                Tier1DropListBackup = BackupDropList(run.availableTier1DropList);
                Tier2DropListBackup = BackupDropList(run.availableTier2DropList);
                Tier3DropListBackup = BackupDropList(run.availableTier3DropList);
                EquipmentDropListBackup = BackupDropList(run.availableEquipmentDropList);

                run.availableTier1DropList = availableItems[0];
                run.availableTier2DropList = availableItems[1];
                run.availableTier3DropList = availableItems[2];
                run.availableEquipmentDropList = availableItems[3];
            }

            public static void RevertDropLists() {
                var oldItems = new List<List<PickupIndex>> {
                    BackupDropList(Tier1DropListBackup),
                    BackupDropList(Tier2DropListBackup),
                    BackupDropList(Tier3DropListBackup),
                    BackupDropList(EquipmentDropListBackup)
                };

                var run = Run.instance;
                run.availableTier1DropList = oldItems[0];
                run.availableTier2DropList = oldItems[1];
                run.availableTier3DropList = oldItems[2];
                run.availableEquipmentDropList = oldItems[3];
            }

            public void ClearAllLists(Run run) {
                run.availableItems.Clear();
                run.availableEquipment.Clear();
                run.availableTier1DropList.Clear();
                run.availableTier2DropList.Clear();
                run.availableTier3DropList.Clear();
                run.availableBossDropList.Clear();
                run.availableLunarDropList.Clear();
                run.availableEquipmentDropList.Clear();
                run.availableNormalEquipmentDropList.Clear();
                run.availableLunarEquipmentDropList.Clear();
            }

            public void DuplicateDropLists(Run run) {
                if (!OriginalListsSaved) {
                    Tier1DropListOriginal = BackupDropList(run.availableTier1DropList);
                    Tier2DropListOriginal = BackupDropList(run.availableTier2DropList);
                    Tier3DropListOriginal = BackupDropList(run.availableTier3DropList);
                    LunarDropListOriginal = BackupDropList(run.availableLunarDropList);
                    EquipmentDropListOriginal = BackupDropList(run.availableEquipmentDropList);
                    NormalEquipmentDropListOriginal = BackupDropList(run.availableNormalEquipmentDropList);
                    LunarEquipmentDropListOriginal = BackupDropList(run.availableLunarEquipmentDropList);

                    BossDropListOriginal = BackupDropList(run.availableBossDropList);

                    /*
                    foreach (var bossItem in Catalog.SpecialItems) {
                        var pickupIndex = PickupCatalog.FindPickupIndex(bossItem);
                        if (!BossDropListOriginal.Contains(pickupIndex)) {
                            BossDropListOriginal.Add(pickupIndex);
                        }
                    }
                    */

                    SpecialItemsOriginal.Clear();
                    foreach (var itemIndex in Catalog.SpecialItems) {
                        if (run.availableItems.Contains(itemIndex)) {
                            SpecialItemsOriginal.Add(PickupCatalog.FindPickupIndex(itemIndex));
                        }
                    }
                    foreach (var itemIndex in Catalog.ScrapItems.Values) {
                        if (run.availableItems.Contains(itemIndex)) {
                            SpecialItemsOriginal.Add(PickupCatalog.FindPickupIndex(itemIndex));
                        }
                    }
                    
                    SpecialEquipmentOriginal.Clear();
                    foreach (var equipmentIndex in Catalog.EliteEquipment) {
                        var sprite = EquipmentCatalog.GetEquipmentDef(equipmentIndex).pickupIconSprite;
                        if (sprite != null && !sprite.name.Contains(NullIconTextureName)) {
                            SpecialEquipmentOriginal.Add(PickupCatalog.FindPickupIndex(equipmentIndex));
                        }
                    }

                    OriginalListsSaved = true;
                }
            }

            internal void GenerateDropLists(
                Dictionary<ItemTier, List<ItemIndex>> itemsToAdd,
                Dictionary<ItemTier, List<ItemIndex>> itemsToRemove,
                Dictionary<EquipmentDropType, List<EquipmentIndex>> equipmentsToAdd,
                Dictionary<EquipmentDropType, List<EquipmentIndex>> equipmentsToRemove) {
                AvailableTier1DropList = BackupDropList(CreateDropList(Tier1DropListOriginal, itemsToAdd[ItemTier.Tier1], itemsToRemove[ItemTier.Tier1]));
                AvailableTier2DropList = BackupDropList(CreateDropList(Tier2DropListOriginal, itemsToAdd[ItemTier.Tier2], itemsToRemove[ItemTier.Tier2]));
                AvailableTier3DropList = BackupDropList(CreateDropList(Tier3DropListOriginal, itemsToAdd[ItemTier.Tier3], itemsToRemove[ItemTier.Tier3]));
                AvailableLunarDropList = BackupDropList(CreateDropList(LunarDropListOriginal, itemsToAdd[ItemTier.Lunar], itemsToRemove[ItemTier.Lunar]));
                AvailableSpecialItems = BackupDropList(CreateDropList(SpecialItemsOriginal, itemsToAdd[ItemTier.NoTier], itemsToRemove[ItemTier.NoTier]));

                AvailableEquipmentDropList = BackupDropList(CreateDropList(NormalEquipmentDropListOriginal,
                    equipmentsToAdd[EquipmentDropType.Normal], equipmentsToRemove[EquipmentDropType.Normal]));
                AvailableNormalEquipmentDropList = AvailableEquipmentDropList;

                AvailableLunarEquipmentDropList = BackupDropList(CreateDropList(LunarEquipmentDropListOriginal,
                    equipmentsToAdd[EquipmentDropType.Lunar], equipmentsToRemove[EquipmentDropType.Lunar]));

                AvailableSpecialEquipment = BackupDropList(CreateDropList(SpecialEquipmentOriginal,
                    equipmentsToAdd[EquipmentDropType.Elite], equipmentsToRemove[EquipmentDropType.Elite]));

                AvailableBossDropList = BackupDropList(CreateDropList(BossDropListOriginal,
                    itemsToAdd[ItemTier.Boss], equipmentsToAdd[EquipmentDropType.Boss],
                    itemsToRemove[ItemTier.Boss], equipmentsToRemove[EquipmentDropType.Boss]));
            }

            private static List<PickupIndex> CreateDropList(IEnumerable<PickupIndex> vanillaDropList,
                IEnumerable<ItemIndex> itemsToAdd,
                IEnumerable<EquipmentIndex> equipmentsToAdd,
                IEnumerable<ItemIndex> itemsToRemove,
                IEnumerable<EquipmentIndex> equipmentsToRemove) {
                var finalDropList = new List<PickupIndex>();
                foreach (var pickupIndex in vanillaDropList) {
                    if (!finalDropList.Contains(pickupIndex)) {
                        finalDropList.Add(pickupIndex);
                    }
                }

                foreach (var itemIndex in itemsToAdd) {
                    var pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
                    if (!finalDropList.Contains(pickupIndex)) {
                        finalDropList.Add(pickupIndex);
                    }
                }

                foreach (var itemIndex in itemsToRemove) {
                    var pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
                    if (finalDropList.Contains(pickupIndex)) {
                        finalDropList.Remove(pickupIndex);
                    }
                }

                foreach (var equipmentIndex in equipmentsToAdd) {
                    var pickupIndex = PickupCatalog.FindPickupIndex(equipmentIndex);
                    if (!finalDropList.Contains(pickupIndex)) {
                        finalDropList.Add(pickupIndex);
                    }
                }

                foreach (var equipmentIndex in equipmentsToRemove) {
                    var pickupIndex = PickupCatalog.FindPickupIndex(equipmentIndex);
                    if (finalDropList.Contains(pickupIndex)) {
                        finalDropList.Remove(pickupIndex);
                    }
                }

                return finalDropList;
            }

            private static List<PickupIndex> CreateDropList(
                IEnumerable<PickupIndex> vanillaDropList,
                IEnumerable<ItemIndex> itemsToAdd,
                IEnumerable<ItemIndex> itemsToRemove) {
                var finalDropList = new List<PickupIndex>();
                foreach (var pickupIndex in vanillaDropList) {
                    if (!finalDropList.Contains(pickupIndex)) {
                        finalDropList.Add(pickupIndex);
                    }
                }

                foreach (var itemIndex in itemsToAdd) {
                    var pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
                    if (!finalDropList.Contains(pickupIndex)) {
                        finalDropList.Add(pickupIndex);
                    }
                }

                foreach (var itemIndex in itemsToRemove) {
                    var pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
                    if (finalDropList.Contains(pickupIndex)) {
                        finalDropList.Remove(pickupIndex);
                    }
                }

                return finalDropList;
            }

            private static List<PickupIndex> CreateDropList(
                IEnumerable<PickupIndex> vanillaDropList,
                IEnumerable<EquipmentIndex> equipmentsToAdd,
                IEnumerable<EquipmentIndex> equipmentsToRemove) {
                var finalDropList = new List<PickupIndex>();
                foreach (var pickupIndex in vanillaDropList) {
                    if (!finalDropList.Contains(pickupIndex)) {
                        finalDropList.Add(pickupIndex);
                    }
                }

                foreach (var equipmentIndex in equipmentsToAdd) {
                    var pickupIndex = PickupCatalog.FindPickupIndex(equipmentIndex);
                    if (!finalDropList.Contains(pickupIndex)) {
                        finalDropList.Add(pickupIndex);
                    }
                }

                foreach (var equipmentIndex in equipmentsToRemove) {
                    var pickupIndex = PickupCatalog.FindPickupIndex(equipmentIndex);
                    if (finalDropList.Contains(pickupIndex)) {
                        finalDropList.Remove(pickupIndex);
                    }
                }

                return finalDropList;
            }

            public void SetItems(Run run) {
                if (IsValidList(AvailableTier1DropList)) {
                    foreach (var pickupIndex in AvailableTier1DropList) {
                        run.availableTier1DropList.Add(pickupIndex);
                        run.availableItems.Add(PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
                    }
                }

                if (IsValidList(AvailableTier2DropList)) {
                    foreach (var pickupIndex in AvailableTier2DropList) {
                        run.availableTier2DropList.Add(pickupIndex);
                        run.availableItems.Add(PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
                    }
                }

                if (IsValidList(AvailableTier3DropList)) {
                    foreach (var pickupIndex in AvailableTier3DropList) {
                        run.availableTier3DropList.Add(pickupIndex);
                        run.availableItems.Add(PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
                    }
                }

                if (IsValidList(AvailableBossDropList)) {
                    foreach (var pickupIndex in AvailableBossDropList) {
                        run.availableBossDropList.Add(pickupIndex);
                        run.availableItems.Add(PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
                    }
                }

                if (IsValidList(AvailableLunarDropList)) {
                    foreach (var pickupIndex in AvailableLunarDropList) {
                        run.availableLunarDropList.Add(pickupIndex);
                        run.availableItems.Add(PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
                    }
                }

                if (IsValidList(AvailableSpecialItems)) {
                    foreach (var pickupIndex in AvailableSpecialItems) {
                        run.availableItems.Add(PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
                    }
                }

                if (IsValidList(AvailableEquipmentDropList)) {
                    foreach (var pickupIndex in AvailableEquipmentDropList) {
                        run.availableEquipmentDropList.Add(pickupIndex);
                        run.availableEquipment.Add(PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex);
                    }
                }
                // high probability of code smell from ror2 code
                run.availableNormalEquipmentDropList = run.availableEquipmentDropList;

                if (IsValidList(AvailableLunarEquipmentDropList)) {
                    foreach (var pickupIndex in AvailableLunarEquipmentDropList) {
                        run.availableLunarEquipmentDropList.Add(pickupIndex);
                        run.availableEquipment.Add(PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex);
                    }
                }

                if (IsValidList(AvailableSpecialEquipment)) {
                    foreach (var pickupIndex in AvailableSpecialEquipment) {
                        run.availableEquipment.Add(PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex);
                    }
                }
            }

            public static List<PickupIndex> ToPickupIndices(IEnumerable<ItemIndex> indices) {
                return indices.Select(PickupCatalog.FindPickupIndex).ToList();
            }

            public static List<PickupIndex> ToPickupIndices(IEnumerable<EquipmentIndex> indices) {
                return indices.Select(PickupCatalog.FindPickupIndex).ToList();
            }

            public static bool IsValidList(IEnumerable<PickupIndex> dropList) {
                if (dropList.Count() == 0 || (dropList.Count() == 1 && dropList.Contains(PickupIndex.none))) {
                    return false;
                }
                return true;
            }
        }
    }
}
