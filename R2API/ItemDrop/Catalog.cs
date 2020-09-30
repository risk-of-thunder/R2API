using System.Collections.Generic;
using RoR2;

namespace R2API {
    namespace ItemDropAPITools {
        public static class Catalog {
            public static bool Loaded;
            public static readonly Dictionary<ItemTier, ItemIndex> ScrapItems = new Dictionary<ItemTier, ItemIndex>();
            public static readonly List<EquipmentIndex> EliteEquipment = new List<EquipmentIndex>();
            
            public static readonly List<ItemIndex> Pearls = new List<ItemIndex> {
                ItemIndex.Pearl,
                ItemIndex.ShinyPearl
            };

            public static void PopulateItemCatalog() {
                if (!Loaded) {
                    foreach (var itemIndex in ItemCatalog.allItems) {
                        if (itemIndex.ToString().ToLower().Contains("scrap")) {
                            ScrapItems.Add(ItemCatalog.GetItemDef(itemIndex).tier, itemIndex);
                        }
                    }
                    foreach (var equipmentIndex in EquipmentCatalog.allEquipment) {
                        if (!EquipmentCatalog.equipmentList.Contains(equipmentIndex)) {
                            EliteEquipment.Add(equipmentIndex);
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
