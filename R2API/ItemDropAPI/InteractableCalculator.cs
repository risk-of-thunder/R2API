using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using RoR2;

namespace R2API {
    namespace ItemDropAPITools {
        public class InteractableCalculator : MonoBehaviour {
            static public int prefixLength = 3;

            public List<string> interactablesInvalid = new List<string>();

            static public List<string> allTiersMustBePresent = new List<string>() {
                "ShrineCleanse",
            };
            static public Dictionary<string, ItemTier> tierConversion = new Dictionary<string, ItemTier>() {
                { "tier1", ItemTier.Tier1 },
                { "tier2", ItemTier.Tier2 },
                { "tier3", ItemTier.Tier3 },
                { "boss", ItemTier.Boss },
                { "lunar", ItemTier.Lunar},
            };
            private List<string> subsetChests = new List<string>() {
                "CategoryChestDamage",
                "CategoryChestHealing",
                "CategoryChestUtility",
            };
            public Dictionary<string, Dictionary<string, bool>> subsetTiersPresent = new Dictionary<string, Dictionary<string, bool>>() {

            };
            public Dictionary<string, bool> tiersPresent = new Dictionary<string, bool>() {
                { "tier1", false },
                { "tier2", false },
                { "tier3", false },
                { "boss", false },
                { "lunar", false },
                { "equipment", false },
                { "lunarEquipment", false },
                { "damage", false },
                { "healing", false },
                { "utility", false },
                { "pearl", false },
                { "drone", false },
            };
            public Dictionary<string, Dictionary<string, bool>> interactablesTiers = new Dictionary<string, Dictionary<string, bool>>() {
                { "Chest1", new Dictionary<string, bool>() {
                    { "tier1", false },
                    //{ "tier2", false },
                    //{ "tier3", false },
                }},
                { "Chest2", new Dictionary<string, bool>() {
                    { "tier2", false },
                    //{ "tier3", false },
                }},
                { "EquipmentBarrel", new Dictionary<string, bool>() {
                    { "equipment", false },
                }},
                { "TripleShop", new Dictionary<string, bool>() {
                    { "tier1", false },
                }},
                { "LunarChest", new Dictionary<string, bool>() {
                    { "lunar", false },
                }},
                { "TripleShopLarge", new Dictionary<string, bool>() {
                    { "tier2", false },
                }},
                { "CategoryChestDamage", new Dictionary<string, bool>() {
                    { "damage", false },
                }},
                { "CategoryChestHealing", new Dictionary<string, bool>() {
                    { "healing", false },
                }},
                { "CategoryChestUtility", new Dictionary<string, bool>() {
                    { "utility", false },
                }},
                { "ShrineChance", new Dictionary<string, bool>() {
                    { "tier1", false },
                    { "tier2", false },
                    { "tier3", false },
                    { "equipment", false},
                }},
                { "ShrineCleanse", new Dictionary<string, bool>() {
                    { "lunar", false },
                    { "pearl", false}
                }},
                { "ShrineRestack", new Dictionary<string, bool>() {
                    { "tier1", false },
                    { "tier2", false },
                    { "tier3", false },
                    { "boss", false },
                    { "lunar", false },
                }},
                { "TripleShopEquipment", new Dictionary<string, bool>() {
                    { "equipment", false },
                }},
                { "BrokenEquipmentDrone", new Dictionary<string, bool>() {
                    { "equipment", false },
                }},
                { "Chest1Stealthed", new Dictionary<string, bool>() {
                    { "tier1", false },
                    { "tier2", false },
                    { "tier3", false },
                }},
                { "GoldChest", new Dictionary<string, bool>() {
                    { "tier3", false },
                }},
                { "Scrapper", new Dictionary<string, bool>() {
                    { "tier1", false },
                    { "tier2", false },
                    { "tier3", false },
                    { "boss", false },
                }},
                { "Duplicator", new Dictionary<string, bool>() {
                    { "tier1", false },
                }},
                { "DuplicatorLarge", new Dictionary<string, bool>() {
                    { "tier2", false },
                }},
                { "DuplicatorMilitary", new Dictionary<string, bool>() {
                    { "tier3", false },
                }},
                { "DuplicatorWild", new Dictionary<string, bool>() {
                    { "boss", false },
                }},
                { "ScavBackpack", new Dictionary<string, bool>() {
                    { "tier1", false },
                    { "tier2", false },
                    { "tier3", false },
                }},
                { "CasinoChest", new Dictionary<string, bool>() {
                    { "tier1", false },
                    { "tier2", false },
                    { "tier3", false },
                    { "equipment", false },
                }},
            };

            static public string GetSpawnCardName(RoR2.SpawnCard givenSpawncard) {
                return givenSpawncard.name.Substring(prefixLength, givenSpawncard.name.Length - prefixLength);
            }

            static public void AddItemsToList(List<PickupIndex> listA, List<PickupIndex> listB) {
                foreach (PickupIndex pickupIndex in listB) {
                    if (PickupCatalog.GetPickupDef(pickupIndex).itemIndex != ItemIndex.None) {
                        if (!listA.Contains(pickupIndex)) {
                            listA.Add(pickupIndex);
                        }
                    }
                }
            }

            public void CalculateInvalidInteractables(DropList dropList) {
                List<string> tiersPresentKeys = tiersPresent.Keys.ToList();
                foreach (string tier in tiersPresentKeys) {
                    tiersPresent[tier] = false;
                }
                subsetTiersPresent.Clear();
                foreach (string subsetChest in subsetChests) {
                    subsetTiersPresent.Add(subsetChest, new Dictionary<string, bool>());
                    foreach (string tier in tiersPresentKeys) {
                        subsetTiersPresent[subsetChest].Add(tier, false);
                    }
                }
                foreach (PickupIndex pickupIndex in dropList.availableTier1DropList) {
                    if (PickupCatalog.GetPickupDef(pickupIndex).itemIndex != ItemIndex.None && !Catalogue.scrapItems.ContainsValue(PickupCatalog.GetPickupDef(pickupIndex).itemIndex)) {
                        tiersPresent["tier1"] = true;
                        break;
                    }
                }
                foreach (PickupIndex pickupIndex in dropList.availableTier2DropList) {
                    if (PickupCatalog.GetPickupDef(pickupIndex).itemIndex != ItemIndex.None && !Catalogue.scrapItems.ContainsValue(PickupCatalog.GetPickupDef(pickupIndex).itemIndex)) {
                        tiersPresent["tier2"] = true;
                        break;
                    }
                }
                foreach (PickupIndex pickupIndex in dropList.availableTier3DropList) {
                    if (PickupCatalog.GetPickupDef(pickupIndex).itemIndex != ItemIndex.None && !Catalogue.scrapItems.ContainsValue(PickupCatalog.GetPickupDef(pickupIndex).itemIndex)) {
                        tiersPresent["tier3"] = true;
                        break;
                    }
                }
                foreach (PickupIndex pickupIndex in dropList.availableBossDropList) {
                    if (PickupCatalog.GetPickupDef(pickupIndex).itemIndex != ItemIndex.None && !Catalogue.scrapItems.ContainsValue(PickupCatalog.GetPickupDef(pickupIndex).itemIndex) && Catalogue.pearls.Contains(PickupCatalog.GetPickupDef(pickupIndex).itemIndex)) {
                        tiersPresent["pearl"] = true;
                        break;
                    }
                }
                foreach (PickupIndex pickupIndex in dropList.availableBossDropList) {
                    if (PickupCatalog.GetPickupDef(pickupIndex).itemIndex != ItemIndex.None && !Catalogue.scrapItems.ContainsValue(PickupCatalog.GetPickupDef(pickupIndex).itemIndex) && !Catalogue.pearls.Contains(PickupCatalog.GetPickupDef(pickupIndex).itemIndex)) {
                        tiersPresent["boss"] = true;
                        break;
                    }
                }

                foreach (PickupIndex pickupIndex in dropList.availableLunarDropList) {
                    if (PickupCatalog.GetPickupDef(pickupIndex).itemIndex != ItemIndex.None && !Catalogue.scrapItems.ContainsValue(PickupCatalog.GetPickupDef(pickupIndex).itemIndex)) {
                        tiersPresent["lunar"] = true;
                        break;
                    }
                }
                List<PickupIndex> everyItem = new List<PickupIndex>();
                AddItemsToList(everyItem, dropList.availableTier1DropList);
                AddItemsToList(everyItem, dropList.availableTier2DropList);
                AddItemsToList(everyItem, dropList.availableTier3DropList);
                foreach (PickupIndex pickupIndex in everyItem) {
                    PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                    RoR2.ItemDef itemDef = RoR2.ItemCatalog.GetItemDef(pickupDef.itemIndex);
                    foreach (RoR2.ItemTag itemTag in itemDef.tags) {
                        string interactableName = "";
                        if (itemTag == RoR2.ItemTag.Damage) {
                            tiersPresent["damage"] = true;
                            interactableName = "CategoryChestDamage";
                        } else if (itemTag == RoR2.ItemTag.Healing) {
                            tiersPresent["healing"] = true;
                            interactableName = "CategoryChestHealing";
                        } else if (itemTag == RoR2.ItemTag.Utility) {
                            tiersPresent["utility"] = true;
                            interactableName = "CategoryChestUtility";
                        }
                        if (subsetChests.Contains(interactableName)) {
                            if (RoR2.ItemCatalog.tier1ItemList.Contains(pickupDef.itemIndex)) {
                                subsetTiersPresent[interactableName]["tier1"] = true;
                            } else if (RoR2.ItemCatalog.tier2ItemList.Contains(pickupDef.itemIndex)) {
                                subsetTiersPresent[interactableName]["tier2"] = true;
                            } else if (RoR2.ItemCatalog.tier3ItemList.Contains(pickupDef.itemIndex)) {
                                subsetTiersPresent[interactableName]["tier3"] = true;
                            }
                        }
                    }
                }
                if (dropList.availableNormalEquipmentDropList.Count > 0) {
                    tiersPresent["equipment"] = true;
                }
                if (dropList.availableLunarEquipmentDropList.Count > 0) {
                    tiersPresent["lunar"] = true;
                }
                List<string> interactableTypeKeys = interactablesTiers.Keys.ToList();
                foreach (string interactableType in interactableTypeKeys) {
                    List<string> interactableTypeTierKeys = interactablesTiers[interactableType].Keys.ToList();
                    foreach (string tier in interactableTypeTierKeys) {
                        interactablesTiers[interactableType][tier] = false;
                    }
                }
                foreach (string tier in tiersPresent.Keys) {
                    if (tiersPresent[tier]) {
                        foreach (string interactableType in interactableTypeKeys) {
                            if (interactablesTiers[interactableType].ContainsKey(tier)) {
                                interactablesTiers[interactableType][tier] = true;
                            }
                        }
                    }
                }
                List<string> scrapTierKeys = interactablesTiers["Scrapper"].Keys.ToList();
                foreach (string tier in scrapTierKeys) {
                    if (interactablesTiers["Scrapper"][tier]) {
                        if (Catalogue.scrapItems.ContainsKey(tierConversion[tier])) {
                            if (!dropList.availableSpecialItems.Contains(PickupCatalog.FindPickupIndex(Catalogue.scrapItems[tierConversion[tier]]))) {
                                interactablesTiers["Scrapper"][tier] = false;
                            }
                        }
                    }
                }

                interactablesInvalid.Clear();
                foreach (string interactableType in interactablesTiers.Keys) {
                    bool interactableValid = false;
                    bool allTrue = true;
                    foreach (string tier in interactablesTiers[interactableType].Keys) {
                        if (interactablesTiers[interactableType][tier]) {
                            interactableValid = true;
                        } else {
                            allTrue = false;
                        }
                    }
                    if (!interactableValid || (allTiersMustBePresent.Contains(interactableType) && !allTrue)) {
                        interactablesInvalid.Add(interactableType);
                    }
                }
            }
        }
    }
}
