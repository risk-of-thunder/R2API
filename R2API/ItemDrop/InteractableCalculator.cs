using R2API.ItemDropAPITools;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace R2API.ItemDrop {

    public class InteractableCalculator {
        /*
            There are many interactables that can drop items.
            Each of these interactables will have their selection of items sorted into a few different subset lists.
            This class determines which of those subset lists are present in the current run
                and therefore which interactables should be prevented from spawning.
        */

        public readonly List<string> InvalidInteractables = new List<string>();


        /*
            This enum represents all the different subsets of items that interactables currently use.
        */
        public enum DropType {
            tier1,
            tier2,
            tier3,
            boss,
            lunar,
            equipment,
            lunarEquipment,
            damage,
            healing,
            utility,
            pearl,
            drone,
            none
        }

        public static readonly Dictionary<DropType, ItemTier> TierConversion = new Dictionary<DropType, ItemTier> {
            { DropType.tier1, ItemTier.Tier1 },
            { DropType.tier2, ItemTier.Tier2 },
            { DropType.tier3, ItemTier.Tier3 },
            { DropType.boss, ItemTier.Boss },
            { DropType.lunar, ItemTier.Lunar }
        };

        public readonly Dictionary<DropType, bool> TiersPresent = new Dictionary<DropType, bool>();
        public readonly Dictionary<string, Dictionary<DropType, bool>> SubsetTiersPresent = new Dictionary<string, Dictionary<DropType, bool>>();

        private readonly List<string> _subsetChests = new List<string> {
            "CategoryChestDamage",
            "CategoryChestHealing",
            "CategoryChestUtility"
        };


        /*
            This dictionary contains every interactable that drops items
                and the subset lists utilized to select an item to drop.
            If any of the subset lists are populated the interactable is allowed to spawn.
        */
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
                { DropType.lunar, false },
                { DropType.pearl, false}
            }},
            { "ShrineRestack", new Dictionary<DropType, bool> {
                { DropType.tier1, false },
                { DropType.tier2, false },
                { DropType.tier3, false },
                { DropType.boss, false },
                { DropType.lunar, false }
            }},
            { "TripleShopEquipment", new Dictionary<DropType, bool> {
                { DropType.equipment, false }
            }},
            { "BrokenEquipmentDrone", new Dictionary<DropType, bool> {
                { DropType.equipment, false }
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
                { DropType.tier1, false },
                { DropType.tier2, false },
                { DropType.tier3, false },
                { DropType.boss, false }
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

        /*
            This is a list of interactables that require all subset lists to be populated to be allowed to spawn.
        */
        public static readonly List<string> AllTiersMustBePresent = new List<string> {
            "ShrineCleanse"
        };

        private static Regex IscRegex = new Regex("^isc");
        /*
            This function will remove isc from the interactable spawn card name, to save me typing it again everywhere.
            ie. iscShrineCleanse to ShrineCleanse
        */
        public static string GetSpawnCardName(SpawnCard givenSpawnCard) {
            return GetSpawnCardName(givenSpawnCard.name);
        }

        public static string GetSpawnCardName(string spawnCardName) {
            return IscRegex.Replace(spawnCardName, "");
        }


        /*
            Will add every item in list B to list A.
        */
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
            /*
                These sections of code will determine if each of the subset lists are populated.
            */
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

            if (dropList.AvailableTier1DropList.Count > 0) {
                TiersPresent[DropType.tier1] = true;
            }
            if (dropList.AvailableTier2DropList.Count > 0) {
                TiersPresent[DropType.tier2] = true;
            }
            if (dropList.AvailableTier3DropList.Count > 0) {
                TiersPresent[DropType.tier3] = true;
            }
            if (dropList.AvailableBossDropList.Count > 0) {
                TiersPresent[DropType.boss] = true;
            }
            if (dropList.AvailableLunarDropList.Count > 0) {
                TiersPresent[DropType.lunar] = true;
            }
            if (dropList.AvailableEquipmentDropList.Count > 0) {
                TiersPresent[DropType.equipment] = true;
            }
            if (dropList.AvailableLunarEquipmentDropList.Count > 0) {
                TiersPresent[DropType.lunar] = true;
            }
            foreach (var pickupIndex in dropList.AvailableSpecialItems) {
                var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef != null) {
                    if (Catalog.Pearls.Contains(pickupDef.itemIndex)) {
                        TiersPresent[DropType.pearl] = true;
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


            /*
                Updates the interactable types with which subset lists are populated.
            */
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

            /*
                Determines which interactables should be prevented from spawning based on which subset lists are populated.
            */
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
