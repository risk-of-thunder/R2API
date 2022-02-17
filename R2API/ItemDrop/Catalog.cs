using RoR2;
using System.Collections.Generic;

namespace R2API {

    namespace ItemDropAPITools {

        public static class Catalog {
            /*
                This class creates and stores lists for specific types of items that are not saved in their own lists in the vanilla game.
                These are purely for reference so they can be iterated through elsewhere.
            */

            public static readonly List<ItemIndex> SpecialItems = new List<ItemIndex>();
            public static readonly Dictionary<ItemTier, ItemIndex> ScrapItems = new Dictionary<ItemTier, ItemIndex>();
            public static readonly List<EquipmentIndex> EliteEquipment = new List<EquipmentIndex>();
            public static readonly List<ItemIndex> Pearls = new List<ItemIndex>();
            public const string NullIconTextureName = "texNullIcon";

            public static UnityEngine.Events.UnityEvent CatalogGenerated = new UnityEngine.Events.UnityEvent();


            public static void PopulateCatalog() {
                SpecialItems.Clear();
                ScrapItems.Clear();
                EliteEquipment.Clear();
                Pearls.Clear();

                Pearls.Add(ItemCatalog.FindItemIndex("Pearl"));
                Pearls.Add(ItemCatalog.FindItemIndex("ShinyPearl"));
                foreach (var itemIndex in ItemCatalog.allItems) {
                    var itemDef = ItemCatalog.GetItemDef(itemIndex);
                    if (itemDef.tier != ItemTier.NoTier && itemDef.pickupIconSprite != null && itemDef.pickupIconSprite.name != NullIconTextureName) {
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

                foreach (var eliteDef in EliteCatalog.eliteDefs) {
                    if (eliteDef.eliteEquipmentDef != null) {
                        var equipmentIndex = eliteDef.eliteEquipmentDef.equipmentIndex;
                        if (equipmentIndex != EquipmentIndex.None && !EliteEquipment.Contains(equipmentIndex)) {
                            EliteEquipment.Add(equipmentIndex);
                        }
                    }
                }

                /*
                //  This was something I was experimenting with but have abandoned for now.

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
                */
                CatalogGenerated.Invoke();
            }

            // Will retrieve the item index of a scrap type item of the matching tier.
            public static ItemIndex GetScrapIndex(ItemTier itemTier) {
                if (ScrapItems.ContainsKey(itemTier)) {
                    return ScrapItems[itemTier];
                }
                return ItemIndex.None;
            }
        }
    }
}
