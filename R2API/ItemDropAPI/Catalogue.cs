using System;
using System.IO;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using R2API;

namespace R2API {
    namespace ItemDropAPITools {
        public class Catalogue : MonoBehaviour {
            static public bool loaded = false;
            static public List<ItemIndex> specialBossItems = new List<ItemIndex>();
            static public Dictionary<ItemTier, ItemIndex> scrapItems = new Dictionary<ItemTier, ItemIndex>();
            static public List<EquipmentIndex> eliteEquipment = new List<EquipmentIndex>();
            
            static public List<ItemIndex> pearls = new List<ItemIndex>() {
                ItemIndex.Pearl,
                ItemIndex.ShinyPearl,
            };

            static public void PopulateItemCatalogues() {                
                if (!loaded) {
                    foreach (ItemIndex itemIndex in RoR2.ItemCatalog.allItems) {
                        ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                        foreach (ItemTag itemTag in itemDef.tags) {
                            if (itemTag == ItemTag.Scrap) {
                                if (!scrapItems.ContainsKey(itemDef.tier)) {
                                    scrapItems.Add(itemDef.tier, itemIndex);
                                }
                            }
                        }
                        if (!RoR2.ItemCatalog.tier1ItemList.Contains(itemIndex) &&
                            !RoR2.ItemCatalog.tier2ItemList.Contains(itemIndex) &&
                            !RoR2.ItemCatalog.tier3ItemList.Contains(itemIndex) &&
                            !RoR2.ItemCatalog.lunarItemList.Contains(itemIndex)) {
                            if (!scrapItems.ContainsValue(itemIndex) && itemDef.tier != ItemTier.NoTier && itemDef.pickupIconSprite != null && itemDef.pickupIconSprite.name != "texNullIcon") {
                                foreach (ItemTag itemTag in itemDef.tags) {
                                    if (itemTag == ItemTag.WorldUnique) {
                                        specialBossItems.Add(itemIndex);
                                    }
                                }
                            }
                        }
                    }
                    foreach (EquipmentIndex equipmentIndex in RoR2.EquipmentCatalog.allEquipment) {
                        EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                        if (!RoR2.EquipmentCatalog.equipmentList.Contains(equipmentIndex)) {
                            if (equipmentDef.pickupIconSprite != null && equipmentDef.pickupIconSprite.name != "texNullIcon") {
                                eliteEquipment.Add(equipmentIndex);
                            }
                        }
                    }
                    foreach (ItemIndex itemIndex in RoR2.ItemCatalog.lunarItemList) {
                        ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                        bool cleansable = false;
                        foreach (ItemTag itemTag in itemDef.tags) {
                            if (itemTag == ItemTag.Cleansable) {
                                cleansable = true;
                            }
                        }
                        if (!cleansable) {
                            if (!scrapItems.ContainsValue(itemIndex) && itemDef.tier != ItemTier.NoTier && itemDef.pickupIconSprite != null && itemDef.pickupIconSprite.name != "texNullIcon") {
                                //print(itemIndex);
                            }
                        }
                    }
                    loaded = true;
                }
            }

            static public ItemIndex GetScrapIndex(ItemTier itemTier) {
                if (scrapItems.ContainsKey(itemTier)) {
                    return scrapItems[itemTier];
                }
                return ItemIndex.None;
            }
        }
    }
}
