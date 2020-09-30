using System.Collections.Generic;
using System.Linq;
using R2API.ItemDropAPITools;
using RoR2;

namespace R2API.ItemDrop {
    public class InteractableCalculator {
        public const int PrefixLength = 3;

        public readonly List<string> InvalidInteractables = new List<string>();

        public static readonly List<string> AllTiersMustBePresent = new List<string> {
            "ShrineCleanse"
        };
        public static readonly Dictionary<string, ItemTier> TierConversion = new Dictionary<string, ItemTier> {
            { "tier1", ItemTier.Tier1 },
            { "tier2", ItemTier.Tier2 },
            { "tier3", ItemTier.Tier3 },
            { "boss", ItemTier.Boss }
        };
        private readonly List<string> _subsetChests = new List<string> {
            "CategoryChestDamage",
            "CategoryChestHealing",
            "CategoryChestUtility"
        };
        public readonly Dictionary<string, Dictionary<string, bool>> SubsetTiersPresent = new Dictionary<string, Dictionary<string, bool>>();
        public readonly Dictionary<string, bool> TiersPresent = new Dictionary<string, bool> {
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
            { "drone", false }
        };
        public readonly Dictionary<string, Dictionary<string, bool>> InteractablesTiers = new Dictionary<string, Dictionary<string, bool>> {
            { "Chest1", new Dictionary<string, bool> {
                { "tier1", false }
                //{ "tier2", false },
                //{ "tier3", false },
            }},
            { "Chest2", new Dictionary<string, bool> {
                { "tier2", false }
                //{ "tier3", false },
            }},
            { "EquipmentBarrel", new Dictionary<string, bool> {
                { "equipment", false }
            }},
            { "TripleShop", new Dictionary<string, bool> {
                { "tier1", false }
            }},
            { "LunarChest", new Dictionary<string, bool> {
                { "lunar", false }
            }},
            { "TripleShopLarge", new Dictionary<string, bool> {
                { "tier2", false }
            }},
            { "CategoryChestDamage", new Dictionary<string, bool> {
                { "damage", false }
            }},
            { "CategoryChestHealing", new Dictionary<string, bool> {
                { "healing", false }
            }},
            { "CategoryChestUtility", new Dictionary<string, bool> {
                { "utility", false }
            }},
            { "ShrineChance", new Dictionary<string, bool> {
                { "tier1", false },
                { "tier2", false },
                { "tier3", false },
                { "equipment", false}
            }},
            { "ShrineCleanse", new Dictionary<string, bool> {
                { "lunar", false },
                { "pearl", false}
            }},
            { "ShrineRestack", new Dictionary<string, bool> {
                { "tier1", false },
                { "tier2", false },
                { "tier3", false },
                { "boss", false },
                { "lunar", false }
            }},
            { "TripleShopEquipment", new Dictionary<string, bool> {
                { "equipment", false }
            }},
            { "BrokenEquipmentDrone", new Dictionary<string, bool> {
                { "equipment", false }
            }},
            { "Chest1Stealthed", new Dictionary<string, bool> {
                { "tier1", false },
                { "tier2", false },
                { "tier3", false }
            }},
            { "GoldChest", new Dictionary<string, bool> {
                { "tier3", false }
            }},
            { "Scrapper", new Dictionary<string, bool> {
                { "tier1", false },
                { "tier2", false },
                { "tier3", false },
                { "boss", false }
            }},
            { "Duplicator", new Dictionary<string, bool> {
                { "tier1", false }
            }},
            { "DuplicatorLarge", new Dictionary<string, bool> {
                { "tier2", false }
            }},
            { "DuplicatorMilitary", new Dictionary<string, bool> {
                { "tier3", false }
            }},
            { "DuplicatorWild", new Dictionary<string, bool> {
                { "boss", false }
            }},
            { "ScavBackpack", new Dictionary<string, bool> {
                { "tier1", false },
                { "tier2", false },
                { "tier3", false }
            }},
            { "CasinoChest", new Dictionary<string, bool> {
                { "tier1", false },
                { "tier2", false },
                { "tier3", false },
                { "equipment", false }
            }}
        };

        public static string GetSpawnCardName(SpawnCard givenSpawnCard) {
            return givenSpawnCard.name.Substring(PrefixLength, givenSpawnCard.name.Length - PrefixLength);
        }

        public static void AddItemsToList(List<PickupIndex> dst, IEnumerable<PickupIndex> src) {
            foreach (var pickupIndex in src) {
                if (PickupCatalog.GetPickupDef(pickupIndex)?.itemIndex != ItemIndex.None) {
                    if (!dst.Contains(pickupIndex)) {
                        dst.Add(pickupIndex);
                    }
                }
            }
        }

        public void CalculateInvalidInteractables(DropList dropList) {
            var tiersPresentKeys = TiersPresent.Keys.ToList();
            foreach (var tier in tiersPresentKeys) {
                TiersPresent[tier] = false;
            }

            SubsetTiersPresent.Clear();
            foreach (var subsetChest in _subsetChests) {
                SubsetTiersPresent.Add(subsetChest, new Dictionary<string, bool>());
                foreach (var tier in tiersPresentKeys) {
                    SubsetTiersPresent[subsetChest].Add(tier, false);
                }
            }

            foreach (var pickupIndex in dropList.AvailableTier1DropList) {
                var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef != null) {
                    if (pickupDef.itemIndex != ItemIndex.None &&
                        !Catalog.ScrapItems.ContainsValue(pickupDef.itemIndex)) {
                        TiersPresent["tier1"] = true;
                        break;
                    }
                }
            }

            foreach (var pickupIndex in dropList.AvailableTier2DropList) {
                var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef != null) {
                    if (pickupDef.itemIndex != ItemIndex.None &&
                        !Catalog.ScrapItems.ContainsValue(pickupDef.itemIndex)) {
                        TiersPresent["tier2"] = true;
                        break;
                    }
                }
            }

            foreach (var pickupIndex in dropList.AvailableTier3DropList) {
                var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef != null) {
                    if (pickupDef.itemIndex != ItemIndex.None &&
                        !Catalog.ScrapItems.ContainsValue(pickupDef.itemIndex)) {
                        TiersPresent["tier3"] = true;
                        break;
                    }
                }
            }

            foreach (var pickupIndex in dropList.AvailableBossDropList) {
                var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef != null) {
                    if (pickupDef.itemIndex != ItemIndex.None &&
                        !Catalog.ScrapItems.ContainsValue(pickupDef.itemIndex) &&
                        Catalog.Pearls.Contains(pickupDef.itemIndex)) {
                        TiersPresent["pearl"] = true;
                        break;
                    }
                }
            }

            foreach (var pickupIndex in dropList.AvailableBossDropList) {
                var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef != null) {
                    if (pickupDef.itemIndex != ItemIndex.None &&
                        !Catalog.ScrapItems.ContainsValue(pickupDef.itemIndex) &&
                        !Catalog.Pearls.Contains(pickupDef.itemIndex)) {
                        TiersPresent["boss"] = true;
                        break;
                    }
                }
            }

            foreach (var pickupIndex in dropList.AvailableLunarDropList) {
                var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef != null) {
                    if (pickupDef.itemIndex != ItemIndex.None &&
                        !Catalog.ScrapItems.ContainsValue(pickupDef.itemIndex) &&
                        Catalog.Pearls.Contains(pickupDef.itemIndex)) {
                        TiersPresent["lunar"] = true;
                        break;
                    }
                }
            }

            var everyItem = new List<PickupIndex>();
            AddItemsToList(everyItem, dropList.AvailableTier1DropList);
            AddItemsToList(everyItem, dropList.AvailableTier2DropList);
            AddItemsToList(everyItem, dropList.AvailableTier3DropList);
            foreach (var pickupIndex in everyItem) {
                var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef != null) {
                    var itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);
                    if (itemDef != null) {
                        foreach (var itemTag in itemDef.tags) {
                            var interactableName = "";
                            if (itemTag == ItemTag.Damage) {
                                TiersPresent["damage"] = true;
                                interactableName = "CategoryChestDamage";
                            } else if (itemTag == ItemTag.Healing) {
                                TiersPresent["healing"] = true;
                                interactableName = "CategoryChestHealing";
                            } else if (itemTag == ItemTag.Utility) {
                                TiersPresent["utility"] = true;
                                interactableName = "CategoryChestUtility";
                            }
                            if (_subsetChests.Contains(interactableName)) {
                                if (ItemCatalog.tier1ItemList.Contains(pickupDef.itemIndex)) {
                                    SubsetTiersPresent[interactableName]["tier1"] = true;
                                } else if (ItemCatalog.tier2ItemList.Contains(pickupDef.itemIndex)) {
                                    SubsetTiersPresent[interactableName]["tier2"] = true;
                                } else if (ItemCatalog.tier3ItemList.Contains(pickupDef.itemIndex)) {
                                    SubsetTiersPresent[interactableName]["tier3"] = true;
                                }
                            }
                        }
                    }
                }
            }
            if (dropList.AvailableNormalEquipmentDropList.Count > 0) {
                TiersPresent["equipment"] = true;
            }
            if (dropList.AvailableLunarEquipmentDropList.Count > 0) {
                TiersPresent["lunar"] = true;
            }
            var interactableTypeKeys = InteractablesTiers.Keys.ToList();
            foreach (var interactableType in interactableTypeKeys) {
                var interactableTypeTierKeys = InteractablesTiers[interactableType].Keys.ToList();
                foreach (var tier in interactableTypeTierKeys) {
                    InteractablesTiers[interactableType][tier] = false;
                }
            }
            foreach (var tier in TiersPresent.Keys) {
                if (TiersPresent[tier]) {
                    foreach (var interactableType in interactableTypeKeys) {
                        if (InteractablesTiers[interactableType].ContainsKey(tier)) {
                            InteractablesTiers[interactableType][tier] = true;
                        }
                    }
                }
            }
            var scrapTierKeys = InteractablesTiers["Scrapper"].Keys.ToList();
            foreach (var tier in scrapTierKeys) {
                if (InteractablesTiers["Scrapper"][tier]) {
                    if (!dropList.AvailableSpecialItems.Contains(PickupCatalog.FindPickupIndex(Catalog.ScrapItems[TierConversion[tier]]))) {
                        InteractablesTiers["Scrapper"][tier] = false;
                    }
                }
            }

            InvalidInteractables.Clear();
            foreach (var interactableType in InteractablesTiers.Keys) {
                var interactableValid = false;
                var allTrue = true;
                foreach (var tier in InteractablesTiers[interactableType].Keys) {
                    if (InteractablesTiers[interactableType][tier]) {
                        interactableValid = true;
                    } else {
                        allTrue = false;
                    }
                }
                if (!interactableValid || AllTiersMustBePresent.Contains(interactableType) && !allTrue) {
                    InvalidInteractables.Add(interactableType);
                }
            }
        }
    }
}
