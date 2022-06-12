using EntityStates;
using R2API.ContentManagement;
using RoR2;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace R2API;

public class CustomItem {
        public ItemDef? ItemDef;
        public ItemDisplayRuleDict? ItemDisplayRules;

        public CustomItem(ItemDef? itemDef, ItemDisplayRule[]? itemDisplayRules) {
            ItemDef = itemDef;
            ItemDisplayRules = new ItemDisplayRuleDict(itemDisplayRules);
        }

        public CustomItem(ItemDef? itemDef, ItemDisplayRuleDict? itemDisplayRules) {
            ItemDef = itemDef;
            ItemDisplayRules = itemDisplayRules;
        }

        public CustomItem(string name, string nameToken,
            string descriptionToken, string loreToken,
            string pickupToken,
            Sprite pickupIconSprite, GameObject pickupModelPrefab,
            ItemTag[] tags, ItemTier tier,
            bool hidden,
            bool canRemove,
            UnlockableDef unlockableDef,
            ItemDisplayRule[]? itemDisplayRules) {
            SetupItem(name, nameToken, descriptionToken, loreToken, pickupToken, pickupIconSprite, pickupModelPrefab, tier, tags, canRemove, hidden, unlockableDef, new ItemDisplayRuleDict(itemDisplayRules));
        }

        public CustomItem(string name, string nameToken,
            string descriptionToken, string loreToken,
            string pickupToken,
            Sprite pickupIconSprite, GameObject pickupModelPrefab,
            ItemTag[] tags, ItemTier tier,
            bool hidden,
            bool canRemove,
            UnlockableDef unlockableDef,
            ItemDisplayRule[]? itemDisplayRules,
            ItemTierDef itemTierDef = null) {
            SetupItem(name, nameToken, descriptionToken, loreToken, pickupToken, pickupIconSprite, pickupModelPrefab, tier, tags, canRemove, hidden, unlockableDef, new ItemDisplayRuleDict(itemDisplayRules), itemTierDef);
        }

        public CustomItem(string name, string nameToken,
            string descriptionToken, string loreToken,
            string pickupToken,
            Sprite pickupIconSprite, GameObject pickupModelPrefab,
            ItemTier tier, ItemTag[] tags,
            bool canRemove,
            bool hidden,
            UnlockableDef unlockableDef = null,
            ItemDisplayRuleDict? itemDisplayRules = null){
            SetupItem(name, nameToken, descriptionToken, loreToken, pickupToken, pickupIconSprite, pickupModelPrefab, tier, tags, canRemove, hidden, unlockableDef, itemDisplayRules);
        }

        public CustomItem(string name, string nameToken,
            string descriptionToken, string loreToken,
            string pickupToken,
            Sprite pickupIconSprite, GameObject pickupModelPrefab,
            ItemTier tier, ItemTag[] tags,
            bool canRemove,
            bool hidden,
            UnlockableDef unlockableDef = null,
            ItemDisplayRuleDict? itemDisplayRules = null,
            ItemTierDef itemTierDef = null) {
            SetupItem(name, nameToken, descriptionToken, loreToken, pickupToken, pickupIconSprite, pickupModelPrefab, tier, tags, canRemove, hidden, unlockableDef, itemDisplayRules, itemTierDef);
        }

        private void SetupItem(string name, string nameToken,
            string descriptionToken, string loreToken,
            string pickupToken,
            Sprite pickupIconSprite, GameObject pickupModelPrefab,
            ItemTier tier, ItemTag[] tags,
            bool canRemove,
            bool hidden,
            UnlockableDef unlockableDef = null,
            ItemDisplayRuleDict? itemDisplayRules = null,
            ItemTierDef itemTierDef = null) {

            ItemDef = ScriptableObject.CreateInstance<ItemDef>();
            ItemDef.canRemove = canRemove;
            ItemDef.descriptionToken = descriptionToken;
            ItemDef.hidden = hidden;
            ItemDef.loreToken = loreToken;
            ItemDef.name = name;
            ItemDef.nameToken = nameToken;
            ItemDef.pickupIconSprite = pickupIconSprite;
            ItemDef.pickupModelPrefab = pickupModelPrefab;
            ItemDef.pickupToken = pickupToken;
            ItemDef.tags = tags;
            ItemDef.unlockableDef = unlockableDef;
            ItemDisplayRules = itemDisplayRules;

            //If the tier isnt assigned at runtime, load tier from addressables, this should make it so mods that add items dont break.
            //We dont want to set the .tier directly, because that'll attempt to load the itemTierDef via the itemTierCatalog, and we cant
            //Guarantee said catalog has been initialized at that point
            if (tier != ItemTier.AssignedAtRuntime) {
                ItemDef._itemTierDef = LoadTierFromAddress(tier);
                return;
            }
            else {
                //If the itemTier is AssignedAtRuntime, but an itemTierDef is not assigned, default to noTier and warn the user
                if (!itemTierDef) {
                    ItemDef._itemTierDef = null;
                    R2API.Logger.LogWarning($"Trying to create an itemDef ({name}), but the \"tier\" argument is set to {nameof(ItemTier.AssignedAtRuntime)}" +
                        $"And the argument \"itemTierDef\" is null! Resorting to setting tier to NoTier");
                }
                else {
                    ItemDef._itemTierDef = itemTierDef;
                }
            }

        }
        private ItemTierDef LoadTierFromAddress(ItemTier itemTierToLoad) {
            return itemTierToLoad switch {
                ItemTier.Tier1 => LoadTier("RoR2/Base/Common/Tier1Def.asset"),
                ItemTier.Tier2 => LoadTier("RoR2/Base/Common/Tier2Def.asset"),
                ItemTier.Tier3 => LoadTier("RoR2/Base/Common/Tier3Def.asset"),
                ItemTier.Lunar => LoadTier("RoR2/Base/Common/LunarTierDef.asset"),
                ItemTier.Boss => LoadTier("RoR2/Base/Common/BossTierDef.asset"),
                //Void
                ItemTier.VoidTier1 => LoadTier("RoR2/DLC1/Common/VoidTier1Def.asset"),
                ItemTier.VoidTier2 => LoadTier("RoR2/DLC1/Common/VoidTier2Def.asset"),
                ItemTier.VoidTier3 => LoadTier("RoR2/DLC1/Common/VoidTier3Def.asset"),
                ItemTier.VoidBoss => LoadTier("RoR2/DLC1/Common/VoidBossDef.asset"),
                ItemTier.NoTier => null,
                ItemTier.AssignedAtRuntime => null,
                _ => null,
            };
        }

        private ItemTierDef LoadTier(string address) => Addressables.LoadAssetAsync<ItemTierDef>(address).WaitForCompletion();
    }
