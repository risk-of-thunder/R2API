using R2API.ItemDropAPITools;
using RoR2;
using System.Collections.Generic;
using System.Linq;

namespace R2API.ItemDrop {

    public class InteractableCalculator {
        public const int PrefixLength = 3;

        public readonly List<string> InvalidInteractables = new List<string>();

        // tier1 REPRESENTS A VALID ITEM IN THE TIER1 DROP LIST
        // tier1Tier REPRESENTS A VALID TIER1 ITEM THAT CAN DROP FROM ANY DROP LIST

        public enum DropType {
            tier1,
            tier2,
            tier3,
            boss,
            lunar,
            tier1Tier,
            tier2Tier,
            tier3Tier,
            bossTier,
            lunarTier,
            equipment,
            lunarEquipment,
            equipmentTier,
            damage,
            healing,
            utility,
            pearl,
            drone,
            none
        }

        public static readonly Dictionary<DropType, ItemTier> TierConversion = new Dictionary<DropType, ItemTier> {
            { DropType.tier1Tier, ItemTier.Tier1 },
            { DropType.tier2Tier, ItemTier.Tier2 },
            { DropType.tier3Tier, ItemTier.Tier3 },
            { DropType.bossTier, ItemTier.Boss },
            { DropType.lunarTier, ItemTier.Lunar }
        };

        public readonly Dictionary<DropType, bool> TiersPresent = new Dictionary<DropType, bool>();

        public readonly Dictionary<string, Dictionary<DropType, bool>> SubsetTiersPresent = new Dictionary<string, Dictionary<DropType, bool>>();

        private readonly List<string> _subsetChests = new List<string> {
            "CategoryChestDamage",
            "CategoryChestHealing",
            "CategoryChestUtility"
        };

        public readonly Dictionary<string, Dictionary<DropType, bool>> InteractablesTiers = new Dictionary<string, Dictionary<DropType, bool>> {
            { "Chest1", new Dictionary<DropType, bool> {
                { DropType.tier1, false }
                //{ "tier2", false },
                //{ "tier3", false },
            }},
            { "Chest2", new Dictionary<DropType, bool> {
                { DropType.tier2, false }
                //{ "tier3", false },
            }},
            { "EquipmentBarrel", new Dictionary<DropType, bool> {
                { DropType.equipment, false }
            }},
            { "TripleShop", new Dictionary<DropType, bool> {
                { DropType.tier1, false }
            }},
            { "LunarChest", new Dictionary<DropType, bool> {
                { DropType.lunar, false }
            }},
            { "TripleShopLarge", new Dictionary<DropType, bool> {
                { DropType.tier2, false }
            }},
            { "CategoryChestDamage", new Dictionary<DropType, bool> {
                { DropType.damage, false }
            }},
            { "CategoryChestHealing", new Dictionary<DropType, bool> {
                { DropType.healing, false }
            }},
            { "CategoryChestUtility", new Dictionary<DropType, bool> {
                { DropType.utility, false }
            }},
            { "ShrineChance", new Dictionary<DropType, bool> {
                { DropType.tier1, false },
                { DropType.tier2, false },
                { DropType.tier3, false },
                { DropType.equipment, false}
            }},
            { "ShrineCleanse", new Dictionary<DropType, bool> {
                { DropType.lunarTier, false },
                { DropType.pearl, false}
            }},
            { "ShrineRestack", new Dictionary<DropType, bool> {
                { DropType.tier1Tier, false },
                { DropType.tier2Tier, false },
                { DropType.tier3Tier, false },
                { DropType.bossTier, false },
                { DropType.lunarTier, false }
            }},
            { "TripleShopEquipment", new Dictionary<DropType, bool> {
                { DropType.equipment, false }
            }},
            { "BrokenEquipmentDrone", new Dictionary<DropType, bool> {
                { DropType.equipmentTier, false }
            }},
            { "Chest1Stealthed", new Dictionary<DropType, bool> {
                { DropType.tier1, false },
                { DropType.tier2, false },
                { DropType.tier3, false }
            }},
            { "GoldChest", new Dictionary<DropType, bool> {
                { DropType.tier3, false }
            }},
            { "Scrapper", new Dictionary<DropType, bool> {
                { DropType.tier1Tier, false },
                { DropType.tier2Tier, false },
                { DropType.tier3Tier, false },
                { DropType.bossTier, false }
            }},
            { "Duplicator", new Dictionary<DropType, bool> {
                { DropType.tier1, false }
            }},
            { "DuplicatorLarge", new Dictionary<DropType, bool> {
                { DropType.tier2, false }
            }},
            { "DuplicatorMilitary", new Dictionary<DropType, bool> {
                { DropType.tier3, false }
            }},
            { "DuplicatorWild", new Dictionary<DropType, bool> {
                { DropType.boss, false }
            }},
            { "ScavBackpack", new Dictionary<DropType, bool> {
                { DropType.tier1, false },
                { DropType.tier2, false },
                { DropType.tier3, false }
            }},
            { "CasinoChest", new Dictionary<DropType, bool> {
                { DropType.tier1, false },
                { DropType.tier2, false },
                { DropType.tier3, false },
                { DropType.equipment, false }
            }}
        };

        public static readonly List<string> AllTiersMustBePresent = new List<string> {
            "ShrineCleanse"
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
            TiersPresent.Clear();
            foreach (DropType dropType in System.Enum.GetValues(typeof(DropType))) {
                TiersPresent.Add(dropType, false);
            }

            SubsetTiersPresent.Clear();
            foreach (var subsetChest in _subsetChests) {
                SubsetTiersPresent.Add(subsetChest, new Dictionary<DropType, bool>());
                foreach (DropType dropType in System.Enum.GetValues(typeof(DropType))) {
                    SubsetTiersPresent[subsetChest].Add(dropType, false);
                }
            }

            if (DropList.IsValidList(dropList.AvailableTier1DropList)) {
                foreach (var pickupIndex in dropList.AvailableTier1DropList) {
                    var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                    if (pickupDef != null) {
                        TiersPresent[DropType.tier1] = true;
                        break;
                    }
                }
            }

            if (DropList.IsValidList(dropList.AvailableTier2DropList)) {
                foreach (var pickupIndex in dropList.AvailableTier2DropList) {
                    var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                    if (pickupDef != null) {
                        TiersPresent[DropType.tier2] = true;
                        break;
                    }
                }
            }

            if (DropList.IsValidList(dropList.AvailableTier3DropList)) {
                foreach (var pickupIndex in dropList.AvailableTier3DropList) {
                    var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                    if (pickupDef != null) {
                        TiersPresent[DropType.tier3] = true;
                        break;
                    }
                }
            }

            foreach (var pickupIndex in dropList.AvailableSpecialItems) {
                var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef != null) {
                    if (pickupDef.itemIndex != ItemIndex.None &&
                        Catalog.Pearls.Contains(pickupDef.itemIndex)) {
                        TiersPresent[DropType.pearl] = true;
                        break;
                    }
                }
            }

            if (DropList.IsValidList(dropList.AvailableBossDropList)) {
                foreach (var pickupIndex in dropList.AvailableBossDropList) {
                    var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                    if (pickupDef != null) {
                        TiersPresent[DropType.boss] = true;
                        break;
                    }
                }
            }

            if (DropList.IsValidList(dropList.AvailableLunarDropList)) {
                foreach (var pickupIndex in dropList.AvailableLunarDropList) {
                    var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                    if (pickupDef != null) {
                        TiersPresent[DropType.lunar] = true;
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
                                TiersPresent[DropType.damage] = true;
                                interactableName = "CategoryChestDamage";
                            }
                            else if (itemTag == ItemTag.Healing) {
                                TiersPresent[DropType.healing] = true;
                                interactableName = "CategoryChestHealing";
                            }
                            else if (itemTag == ItemTag.Utility) {
                                TiersPresent[DropType.utility] = true;
                                interactableName = "CategoryChestUtility";
                            }
                            if (_subsetChests.Contains(interactableName)) {
                                if (dropList.AvailableTier1DropList.Contains(pickupIndex)) {
                                    SubsetTiersPresent[interactableName][DropType.tier1] = true;
                                }
                                if (dropList.AvailableTier2DropList.Contains(pickupIndex)) {
                                    SubsetTiersPresent[interactableName][DropType.tier2] = true;
                                }
                                if (dropList.AvailableTier3DropList.Contains(pickupIndex)) {
                                    SubsetTiersPresent[interactableName][DropType.tier3] = true;
                                }
                            }
                        }
                    }
                }
            }
            List<List<PickupIndex>> allDropLists = new List<List<PickupIndex>>() {
                dropList.AvailableTier1DropList,
                dropList.AvailableTier2DropList,
                dropList.AvailableTier3DropList,
                dropList.AvailableBossDropList,
                dropList.AvailableLunarDropList,
                dropList.AvailableSpecialItems,
                dropList.AvailableEquipmentDropList,
                dropList.AvailableNormalEquipmentDropList,
                dropList.AvailableLunarEquipmentDropList,
                dropList.AvailableSpecialEquipment,
            };
            foreach (List<PickupIndex> availableDropList in allDropLists) {
                foreach (PickupIndex pickupIndex in availableDropList) {
                    PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                    if (pickupDef != null) {
                        ItemIndex itemIndex = pickupDef.itemIndex;
                        if (itemIndex != ItemIndex.None) {
                            if (!ItemCatalog.GetItemDef(itemIndex).ContainsTag(ItemTag.Scrap)) {
                                ItemTier itemTier = ItemCatalog.GetItemDef(itemIndex).tier;
                                if (itemTier == ItemTier.Tier1) {
                                    TiersPresent[DropType.tier1Tier] = true;
                                }
                                else if (itemTier == ItemTier.Tier2) {
                                    TiersPresent[DropType.tier2Tier] = true;
                                }
                                else if (itemTier == ItemTier.Tier3) {
                                    TiersPresent[DropType.tier3Tier] = true;
                                }
                                else if (itemTier == ItemTier.Boss) {
                                    TiersPresent[DropType.bossTier] = true;
                                }
                                else if (itemTier == ItemTier.Lunar) {
                                    TiersPresent[DropType.lunarTier] = true;
                                }
                            }
                        }
                        EquipmentIndex equipmentIndex = pickupDef.equipmentIndex;
                        if (equipmentIndex != EquipmentIndex.None) {
                            TiersPresent[DropType.equipmentTier] = true;
                        }
                    }
                }
            }

            if (DropList.IsValidList(dropList.AvailableNormalEquipmentDropList)) {
                TiersPresent[DropType.equipment] = true;
            }
            if (DropList.IsValidList(dropList.AvailableLunarEquipmentDropList)) {
                TiersPresent[DropType.lunar] = true;
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
            foreach (DropType tier in scrapTierKeys) {
                if (InteractablesTiers["Scrapper"][tier]) {
                    if (Catalog.ScrapItems.ContainsKey(TierConversion[tier])) {
                        if (!dropList.AvailableSpecialItems.Contains(PickupCatalog.FindPickupIndex(Catalog.ScrapItems[TierConversion[tier]]))) {
                            InteractablesTiers["Scrapper"][tier] = false;
                        }
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
                    }
                    else {
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
