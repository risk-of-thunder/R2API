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
            static public Dictionary<ItemTier, ItemIndex> scrapItems = new Dictionary<ItemTier, ItemIndex>();
            static public List<EquipmentIndex> eliteEquipment = new List<EquipmentIndex>();
            
            static public List<ItemIndex> pearls = new List<ItemIndex>() {
                ItemIndex.Pearl,
                ItemIndex.ShinyPearl,
            };

            static public void PopulateItemCatalogues() {                
                if (!loaded) {
                    foreach (ItemIndex itemIndex in RoR2.ItemCatalog.allItems) {
                        if (itemIndex.ToString().ToLower().Contains("scrap")) {
                            scrapItems.Add(RoR2.ItemCatalog.GetItemDef(itemIndex).tier, itemIndex);
                        }
                    }
                    foreach (EquipmentIndex equipmentIndex in RoR2.EquipmentCatalog.allEquipment) {
                        if (!RoR2.EquipmentCatalog.equipmentList.Contains(equipmentIndex)) {
                            eliteEquipment.Add(equipmentIndex);
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
