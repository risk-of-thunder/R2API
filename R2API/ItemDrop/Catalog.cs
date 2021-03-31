using RoR2;
using System.Collections.Generic;

namespace R2API {

    namespace ItemDropAPITools {

        public static class Catalog {
            public static bool Loaded;
            public static readonly List<ItemIndex> SpecialItems = new List<ItemIndex>();
            public static readonly Dictionary<ItemTier, ItemIndex> ScrapItems = new Dictionary<ItemTier, ItemIndex>();
            public static readonly List<EquipmentIndex> EliteEquipment = new List<EquipmentIndex>();

            public static readonly List<ItemIndex> Pearls = new List<ItemIndex> {
                ItemIndex.Pearl,
                ItemIndex.ShinyPearl
            };

            public static void PopulateItemCatalog() {
                if (!Loaded) {
                    foreach (var itemIndex in ItemCatalog.allItems) {
                        var itemDef = ItemCatalog.GetItemDef(itemIndex);
                        if (itemDef.tier != ItemTier.NoTier && itemDef.pickupIconSprite != null && itemDef.pickupIconSprite.name != DropList.NullIconTextureName) {
                            if (itemDef.ContainsTag(ItemTag.Scrap)) {
                                if (!ScrapItems.ContainsKey(itemDef.tier)) {
                                    ScrapItems.Add(itemDef.tier, itemIndex);
                                }
                            }
                            else if (itemDef.ContainsTag(ItemTag.WorldUnique)) {
                                SpecialItems.Add(itemIndex);
                            }
                        }
                    }
                    
                    foreach (var equipmentIndex in EquipmentCatalog.allEquipment) {
                        var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                        if (!EquipmentCatalog.equipmentList.Contains(equipmentIndex)) {
                            if (equipmentDef.pickupIconSprite != null &&
                                equipmentDef.pickupIconSprite.name != DropList.NullIconTextureName) {
                                EliteEquipment.Add(equipmentIndex);
                            }
                        }
                    }

                    foreach (var itemIndex in ItemCatalog.lunarItemList) {
                        var itemDef = ItemCatalog.GetItemDef(itemIndex);
                        var cleansable = false;
                        foreach (var itemTag in itemDef.tags) {
                            if (itemTag == ItemTag.Cleansable) {
                                cleansable = true;
                            }
                        }
                        if (!cleansable) {
                            if (!ScrapItems.ContainsValue(itemIndex) && itemDef.tier != ItemTier.NoTier &&
                                itemDef.pickupIconSprite != null &&
                                itemDef.pickupIconSprite.name != DropList.NullIconTextureName) {
                                //print(itemIndex);
                            }
                        }
                    }

                    Loaded = true;
                }
            }

            public static ItemIndex GetScrapIndex(ItemTier itemTier) {
                if (ScrapItems.ContainsKey(itemTier)) {
                    return ScrapItems[itemTier];
                }
                return ItemIndex.None;
            }
        }
    }
}
