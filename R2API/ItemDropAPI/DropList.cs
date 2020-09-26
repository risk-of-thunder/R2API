using System;
using System.Collections.Generic;
using UnityEngine;
using RoR2;

namespace R2API {
    namespace ItemDropAPITools {
        public class DropList : MonoBehaviour {
            static public bool originalListsSaved = false;
            static public List<PickupIndex> tier1DropListOriginal = new List<PickupIndex>();
            static public List<PickupIndex> tier2DropListOriginal = new List<PickupIndex>();
            static public List<PickupIndex> tier3DropListOriginal = new List<PickupIndex>();
            static public List<PickupIndex> bossDropListOriginal = new List<PickupIndex>();
            static public List<PickupIndex> lunarDropListOriginal = new List<PickupIndex>();
            static public List<PickupIndex> equipmentDropListOriginal = new List<PickupIndex>();
            static public List<PickupIndex> normalEquipmentDropListOriginal = new List<PickupIndex>();
            static public List<PickupIndex> lunarEquipmentDropListOriginal = new List<PickupIndex>();
            static public List<PickupIndex> specialItemsOriginal = new List<PickupIndex>();
            static public List<PickupIndex> eliteEquipmentOriginal = new List<PickupIndex>();


            static private List<PickupIndex> tier1DropListBackup = new List<PickupIndex>();
            static private List<PickupIndex> tier2DropListBackup = new List<PickupIndex>();
            static private List<PickupIndex> tier3DropListBackup = new List<PickupIndex>();
            static private List<PickupIndex> equipmentDropListBackup = new List<PickupIndex>();

            public List<PickupIndex> availableTier1DropList = new List<PickupIndex>();
            public List<PickupIndex> availableTier2DropList = new List<PickupIndex>();
            public List<PickupIndex> availableTier3DropList = new List<PickupIndex>();
            public List<PickupIndex> availableBossDropList = new List<PickupIndex>();
            public List<PickupIndex> availableLunarDropList = new List<PickupIndex>();
            public List<PickupIndex> availableEquipmentDropList = new List<PickupIndex>();
            public List<PickupIndex> availableNormalEquipmentDropList = new List<PickupIndex>();
            public List<PickupIndex> availableLunarEquipmentDropList = new List<PickupIndex>();
            public List<PickupIndex> availableSpecialItems = new List<PickupIndex>();
            public List<PickupIndex> availableEliteEquipment = new List<PickupIndex>();

            public List<PickupIndex> GetDropList( ItemTier itemTier) {
                if (itemTier == ItemTier.Tier1) {
                    return availableTier1DropList;
                } else if (itemTier == ItemTier.Tier2) {
                    return availableTier2DropList;
                } else if(itemTier == ItemTier.Tier3) {
                    return availableTier3DropList;
                } else if (itemTier == ItemTier.Boss) {
                    return availableBossDropList;
                } else if (itemTier == ItemTier.Lunar) {
                    return availableLunarDropList;
                } else {
                    return availableNormalEquipmentDropList;
                }
            }

            static public void DuplicateDropList(List<PickupIndex> original, List<PickupIndex> backup) {
                backup.Clear();
                foreach (PickupIndex pickupIndex in original) {
                    backup.Add(pickupIndex);
                }
            }

            static public void SetDropLists(List<PickupIndex> givenTier1, List<PickupIndex> givenTier2, List<PickupIndex> givenTier3, List<PickupIndex> givenEquipment) {
                List<PickupIndex> none = new List<PickupIndex>() { PickupIndex.none };
                List<List<PickupIndex>> availableItems = new List<List<PickupIndex>>() { new List<PickupIndex>(), new List<PickupIndex>(), new List<PickupIndex>(), new List<PickupIndex>() };
                DuplicateDropList(givenTier1, availableItems[0]);
                DuplicateDropList(givenTier2, availableItems[1]);
                DuplicateDropList(givenTier3, availableItems[2]);
                DuplicateDropList(givenEquipment, availableItems[3]);
                for (int availableIndex = 0; availableIndex < 4; availableIndex++) {
                    if (availableItems[availableIndex].Count == 0) {
                        availableItems[availableIndex] = none;
                    }
                }

                DuplicateDropList(Run.instance.availableTier1DropList, tier1DropListBackup);
                DuplicateDropList(Run.instance.availableTier2DropList, tier2DropListBackup);
                DuplicateDropList(Run.instance.availableTier3DropList, tier3DropListBackup);
                DuplicateDropList(Run.instance.availableEquipmentDropList, equipmentDropListBackup);
                System.Type type = typeof(RoR2.Run);
                System.Reflection.FieldInfo tier1 = type.GetField("availableTier1DropList", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                System.Reflection.FieldInfo tier2 = type.GetField("availableTier2DropList", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                System.Reflection.FieldInfo tier3 = type.GetField("availableTier3DropList", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                System.Reflection.FieldInfo equipment = type.GetField("availableEquipmentDropList", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                tier1.SetValue(RoR2.Run.instance, availableItems[0]);
                tier2.SetValue(RoR2.Run.instance, availableItems[1]);
                tier3.SetValue(RoR2.Run.instance, availableItems[2]);
                equipment.SetValue(RoR2.Run.instance, availableItems[3]);
            }

            static public void RevertDropLists() {
                System.Type type = typeof(RoR2.Run);
                System.Reflection.FieldInfo tier1 = type.GetField("availableTier1DropList", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                System.Reflection.FieldInfo tier2 = type.GetField("availableTier2DropList", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                System.Reflection.FieldInfo tier3 = type.GetField("availableTier3DropList", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                System.Reflection.FieldInfo equipment = type.GetField("availableEquipmentDropList", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                List<List<PickupIndex>> oldItems = new List<List<PickupIndex>>() { new List<PickupIndex>(), new List<PickupIndex>(), new List<PickupIndex>(), new List<PickupIndex>() };
                DuplicateDropList(tier1DropListBackup, oldItems[0]);
                DuplicateDropList(tier2DropListBackup, oldItems[1]);
                DuplicateDropList(tier3DropListBackup, oldItems[2]);
                DuplicateDropList(equipmentDropListBackup, oldItems[3]);

                tier1.SetValue(RoR2.Run.instance, oldItems[0]);
                tier2.SetValue(RoR2.Run.instance, oldItems[1]);
                tier3.SetValue(RoR2.Run.instance, oldItems[2]);
                equipment.SetValue(RoR2.Run.instance, oldItems[3]);
            }

            public  void ClearAllLists(RoR2.Run run) {
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
                if (!originalListsSaved) {
                    DuplicateDropList(run.availableTier1DropList, tier1DropListOriginal);
                    DuplicateDropList(run.availableTier2DropList, tier2DropListOriginal);
                    DuplicateDropList(run.availableTier3DropList, tier3DropListOriginal);
                    DuplicateDropList(run.availableBossDropList, bossDropListOriginal);
                    DuplicateDropList(run.availableLunarDropList, lunarDropListOriginal);
                    DuplicateDropList(run.availableEquipmentDropList, equipmentDropListOriginal);
                    DuplicateDropList(run.availableNormalEquipmentDropList, normalEquipmentDropListOriginal);
                    DuplicateDropList(run.availableLunarEquipmentDropList, lunarEquipmentDropListOriginal);
                    specialItemsOriginal.Clear();
                    foreach (ItemIndex itemIndex in Catalogue.scrapItems.Values) {
                        Sprite sprite = ItemCatalog.GetItemDef(itemIndex).pickupIconSprite;
                        if (sprite != null && !sprite.name.Contains("texNullIcon")) {
                            specialItemsOriginal.Add(PickupCatalog.FindPickupIndex(itemIndex));
                        }
                    }
                    eliteEquipmentOriginal.Clear();
                    foreach (EquipmentIndex equipmentIndex in Catalogue.eliteEquipment) {
                        Sprite sprite = EquipmentCatalog.GetEquipmentDef(equipmentIndex).pickupIconSprite;
                        if (sprite != null && !sprite.name.Contains("texNullIcon")) {
                            eliteEquipmentOriginal.Add(PickupCatalog.FindPickupIndex(equipmentIndex));
                        }
                    }
                    originalListsSaved = true;
                }
            }

            public void GenerateItems(
                Dictionary<ItemTier, HashSet<ItemIndex>> additionalTierItems,
                HashSet<EquipmentIndex> additionalEquipment,
                Dictionary<ItemTier, List<PickupIndex>> addItems,
                Dictionary<ItemTier, List<PickupIndex>> removeItems,
                Dictionary<ItemTier, List<PickupIndex>> addEquipment,
                Dictionary<ItemTier, List<PickupIndex>> removeEquipment) {

                availableTier1DropList.Clear();
                availableTier2DropList.Clear();
                availableTier3DropList.Clear();
                availableBossDropList.Clear();
                availableLunarDropList.Clear();
                availableLunarEquipmentDropList.Clear();
                availableEquipmentDropList.Clear();
                availableNormalEquipmentDropList.Clear();

                AdjustList(availableTier1DropList, additionalTierItems[ItemTier.Tier1], tier1DropListOriginal, addItems[ItemTier.Tier1], removeItems[ItemTier.Tier1]);
                AdjustList(availableTier2DropList, additionalTierItems[ItemTier.Tier2], tier2DropListOriginal, addItems[ItemTier.Tier2], removeItems[ItemTier.Tier2]);
                AdjustList(availableTier3DropList, additionalTierItems[ItemTier.Tier3], tier3DropListOriginal, addItems[ItemTier.Tier3], removeItems[ItemTier.Tier3]);
                AdjustList(availableBossDropList, additionalTierItems[ItemTier.Boss], bossDropListOriginal, addItems[ItemTier.Boss], removeItems[ItemTier.Boss]);
                AdjustList(availableLunarDropList, additionalTierItems[ItemTier.Lunar], lunarDropListOriginal, addItems[ItemTier.Lunar], removeItems[ItemTier.Lunar]);
                AdjustList(availableSpecialItems, new HashSet<ItemIndex>(), specialItemsOriginal, addItems[ItemTier.NoTier], removeItems[ItemTier.NoTier]);

                AdjustList(availableNormalEquipmentDropList, additionalEquipment, normalEquipmentDropListOriginal, addEquipment[ItemTier.Tier1], removeEquipment[ItemTier.Tier1], ItemTier.Tier1);
                AdjustList(availableLunarEquipmentDropList, additionalEquipment, lunarEquipmentDropListOriginal, addEquipment[ItemTier.Lunar], removeEquipment[ItemTier.Lunar], ItemTier.Lunar);
                AdjustList(availableEliteEquipment, additionalEquipment, eliteEquipmentOriginal, addEquipment[ItemTier.NoTier], removeEquipment[ItemTier.NoTier], ItemTier.NoTier);
            }

            public void AdjustList(
                List<PickupIndex> dropList,
                HashSet<ItemIndex> moddedOriginalList,
                List<PickupIndex> original,
                List<PickupIndex> addItems,
                List<PickupIndex> removeItems) {

                List<PickupIndex> convertedList = new List<PickupIndex>();
                foreach (ItemIndex itemIndex in moddedOriginalList) {
                    PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
                    if (!convertedList.Contains(pickupIndex)) {
                        convertedList.Add(pickupIndex);
                    }
                }
                AdjustList(dropList, convertedList, original, addItems, removeItems);
            }

            public void AdjustList(
                List<PickupIndex> dropList,
                HashSet<EquipmentIndex> moddedOriginalList,
                List<PickupIndex> original,
                List<PickupIndex> addItems,
                List<PickupIndex> removeItems,
                ItemTier itemTier) {

                List<PickupIndex> convertedList = new List<PickupIndex>();
                foreach (EquipmentIndex equipmentIndex in moddedOriginalList) {
                    PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(equipmentIndex);
                    if (!convertedList.Contains(pickupIndex)) {
                        EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                        if ((itemTier == ItemTier.Lunar && equipmentDef.isLunar) || (itemTier == ItemTier.Tier1 && !equipmentDef.isLunar && !Catalogue.eliteEquipment.Contains(equipmentIndex)) || (itemTier == ItemTier.NoTier && !equipmentDef.isLunar && Catalogue.eliteEquipment.Contains(equipmentIndex))) {
                            convertedList.Add(pickupIndex);
                        }
                    }
                }
                AdjustList(dropList, convertedList, original, addItems, removeItems);
            }

            public void AdjustList(
                List<PickupIndex> dropList,
                List<PickupIndex> moddedOriginalList,
                List<PickupIndex> original,
                List<PickupIndex> addItems,
                List<PickupIndex> removeItems) {

                foreach (PickupIndex pickupIndex in original) {
                    if (!dropList.Contains(pickupIndex)) {
                        dropList.Add(pickupIndex);
                    }
                }
                foreach (PickupIndex pickupIndex in addItems) {
                    if (!dropList.Contains(pickupIndex)) {
                        dropList.Add(pickupIndex);
                    }
                }
                foreach (PickupIndex pickupIndex in removeItems) {
                    if (dropList.Contains(pickupIndex)) {
                        dropList.Remove(pickupIndex);
                    }
                }
            }

            public void SetItems(Run run) {
                foreach (PickupIndex pickupIndex in availableTier1DropList) {
                    run.availableTier1DropList.Add(pickupIndex);
                    run.availableItems.Add(PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
                }
                foreach (PickupIndex pickupIndex in availableTier2DropList) {
                    run.availableTier2DropList.Add(pickupIndex);
                    run.availableItems.Add(PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
                }
                foreach (PickupIndex pickupIndex in availableTier3DropList) {
                    run.availableTier3DropList.Add(pickupIndex);
                    run.availableItems.Add(PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
                }
                foreach (PickupIndex pickupIndex in availableBossDropList) {
                    run.availableBossDropList.Add(pickupIndex);
                    run.availableItems.Add(PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
                }
                foreach (PickupIndex pickupIndex in availableLunarDropList) {
                    run.availableLunarDropList.Add(pickupIndex);
                    run.availableItems.Add(PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
                }
                foreach (PickupIndex pickupIndex in availableEquipmentDropList) {
                    run.availableEquipmentDropList.Add(pickupIndex);
                    run.availableEquipment.Add(PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex);
                }
                foreach (PickupIndex pickupIndex in availableNormalEquipmentDropList) {
                    run.availableNormalEquipmentDropList.Add(pickupIndex);
                    run.availableEquipment.Add(PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex);
                }
                foreach (PickupIndex pickupIndex in availableLunarEquipmentDropList) {
                    run.availableLunarEquipmentDropList.Add(pickupIndex);
                    run.availableEquipment.Add(PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex);
                }

                DefaultItemDrops.AddDefaults();
            }
        }
    }
}
