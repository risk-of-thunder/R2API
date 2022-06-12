using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API {
    public class CustomEquipment {
        public EquipmentDef? EquipmentDef;
        public ItemDisplayRuleDict? ItemDisplayRules;

        public CustomEquipment(EquipmentDef? equipmentDef, ItemDisplayRule[]? itemDisplayRules) {
            EquipmentDef = equipmentDef;
            ItemDisplayRules = new ItemDisplayRuleDict(itemDisplayRules);
        }

        public CustomEquipment(EquipmentDef? equipmentDef, ItemDisplayRuleDict? itemDisplayRules) {
            EquipmentDef = equipmentDef;
            ItemDisplayRules = itemDisplayRules;
        }

        public CustomEquipment(string name, string nameToken,
            string descriptionToken, string loreToken,
            string pickupToken,
            Sprite pickupIconSprite, GameObject pickupModelPrefab,
            float cooldown,
            bool canDrop,
            bool enigmaCompatible,
            bool isBoss, bool isLunar,
            BuffDef passiveBuffDef,
            UnlockableDef unlockableDef,
            ColorCatalog.ColorIndex colorIndex = ColorCatalog.ColorIndex.Equipment,
            bool appearsInMultiPlayer = true, bool appearsInSinglePlayer = true,
            ItemDisplayRule[]? itemDisplayRules = null) {

            EquipmentDef = ScriptableObject.CreateInstance<EquipmentDef>();
            EquipmentDef.appearsInMultiPlayer = appearsInMultiPlayer;
            EquipmentDef.appearsInSinglePlayer = appearsInSinglePlayer;
            EquipmentDef.canDrop = canDrop;
            EquipmentDef.colorIndex = colorIndex;
            EquipmentDef.cooldown = cooldown;
            EquipmentDef.descriptionToken = descriptionToken;
            EquipmentDef.enigmaCompatible = enigmaCompatible;
            EquipmentDef.isBoss = isBoss;
            EquipmentDef.isLunar = isLunar;
            EquipmentDef.loreToken = loreToken;
            EquipmentDef.name = name;
            EquipmentDef.nameToken = nameToken;
            EquipmentDef.passiveBuffDef = passiveBuffDef;
            EquipmentDef.pickupIconSprite = pickupIconSprite;
            EquipmentDef.pickupModelPrefab = pickupModelPrefab;
            EquipmentDef.pickupToken = pickupToken;
            EquipmentDef.unlockableDef = unlockableDef;

            ItemDisplayRules = new ItemDisplayRuleDict(itemDisplayRules);
        }

        public CustomEquipment(string name, string nameToken,
            string descriptionToken, string loreToken,
            string pickupToken,
            Sprite pickupIconSprite, GameObject pickupModelPrefab,
            float cooldown,
            bool canDrop,
            bool enigmaCompatible,
            bool isBoss, bool isLunar,
            BuffDef passiveBuffDef,
            UnlockableDef unlockableDef,
            ColorCatalog.ColorIndex colorIndex = ColorCatalog.ColorIndex.Equipment,
            bool appearsInMultiPlayer = true, bool appearsInSinglePlayer = true,
            ItemDisplayRuleDict? itemDisplayRules = null) {

            EquipmentDef = ScriptableObject.CreateInstance<EquipmentDef>();
            EquipmentDef.appearsInMultiPlayer = appearsInMultiPlayer;
            EquipmentDef.appearsInSinglePlayer = appearsInSinglePlayer;
            EquipmentDef.canDrop = canDrop;
            EquipmentDef.colorIndex = colorIndex;
            EquipmentDef.cooldown = cooldown;
            EquipmentDef.descriptionToken = descriptionToken;
            EquipmentDef.enigmaCompatible = enigmaCompatible;
            EquipmentDef.isBoss = isBoss;
            EquipmentDef.isLunar = isLunar;
            EquipmentDef.loreToken = loreToken;
            EquipmentDef.name = name;
            EquipmentDef.nameToken = nameToken;
            EquipmentDef.passiveBuffDef = passiveBuffDef;
            EquipmentDef.pickupIconSprite = pickupIconSprite;
            EquipmentDef.pickupModelPrefab = pickupModelPrefab;
            EquipmentDef.pickupToken = pickupToken;
            EquipmentDef.unlockableDef = unlockableDef;

            ItemDisplayRules = itemDisplayRules;
        }
    }
}
