using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using UnityEngine;
using R2API.MiscHelpers;
using Object = UnityEngine.Object;
using System.Text;
using UnityEngine.AddressableAssets;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace R2API {

    // ReSharper disable once InconsistentNaming
    public static class ItemAPI {
        public const string PluginGUID = R2API.PluginGUID + ".items";
        public const string PluginName = R2API.PluginName + ".Items";
        public const string PluginVersion = "0.0.1";

        public static ObservableCollection<CustomItem?>? ItemDefinitions = new ObservableCollection<CustomItem?>();
        public static ObservableCollection<CustomEquipment?> EquipmentDefinitions = new ObservableCollection<CustomEquipment?>();

        private static ICollection<string> noDefaultIDRSCharacterList = new List<string>();

        public static int CustomItemCount, CustomEquipmentCount;

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded => true;

        #region ModHelper Events and Hooks

        internal static void SetHooks() {
            IL.RoR2.CharacterModel.UpdateMaterials += MaterialFixForItemDisplayOnCharacter;
            On.RoR2.ItemDisplayRuleSet.Init += AddingItemDisplayRulesToCharacterModels;
        }

        internal static void UnsetHooks() {
            IL.RoR2.CharacterModel.UpdateMaterials -= MaterialFixForItemDisplayOnCharacter;
            On.RoR2.ItemDisplayRuleSet.Init -= AddingItemDisplayRulesToCharacterModels;
        }

        #endregion ModHelper Events and Hooks

        #region Add Methods

        /// <summary>
        /// Add a custom item to the list of available items.
        /// Value for ItemDef.ItemIndex can be ignored.
        /// We can't give you the ItemIndex anymore in the method return param. Instead use ItemCatalog.FindItemIndex after catalog are init
        /// If this is called after the ItemCatalog inits then this will return false and ignore the custom item.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>true if added, false otherwise</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Add(CustomItem? item) {
            if (ContentAddition.AddItemInternal(item, Assembly.GetCallingAssembly()))
            {
                ItemDefinitions.Add(item);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add a custom equipment item to the list of available items.
        /// Value for EquipmentDef.equipmentIndex can be ignored.
        /// We can't give you the EquipmentIndex anymore in the method return param. Instead use EquipmentCatalog.FindEquipmentIndex after catalog are init
        /// If this is called after the EquipmentCatalog inits then this will return false and ignore the custom equipment item.
        /// </summary>
        /// <param name="item">The equipment item to add.</param>
        /// <returns>true if added, false otherwise</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Add(CustomEquipment? item) {
            if (ContentAddition.AddEquipmentInternal(item, Assembly.GetCallingAssembly())) {
                EquipmentDefinitions.Add(item);
                return true;
            }
            return false;
        }

        #endregion Add Methods

        #region Other Modded Content Support

        /// <summary>
        /// Prevents bodies and charactermodels matching this name from having nonspecific item display rules applied to them
        /// </summary>
        /// <param name="bodyPrefabOrCharacterModelName">The string to match</param>
        public static void DoNotAutoIDRSFor(string bodyPrefabOrCharacterModelName) {
            noDefaultIDRSCharacterList.Add(bodyPrefabOrCharacterModelName);
        }

        /// <summary>
        /// Prevent prefabs with this name having nonspecific item display rules applied to them
        /// </summary>
        /// <param name="bodyPrefab">The body prefab to match</param>
        public static void DoNotAutoIDRSFor(GameObject bodyPrefab) {
            var characterModel = bodyPrefab.GetComponentInChildren<CharacterModel>();
            if (characterModel) {
                DoNotAutoIDRSFor(bodyPrefab.name);
            }
        }

        #endregion

        #region ItemDisplay Hooks

        // With how unfriendly it is to makes your 3D Prefab work with shaders from the game,
        // makes it so that if the custom prefab doesnt have rendering support for when the player is cloaked, or burning, still display the item on the player.
        // iDeath : This hook was made back when I didn't know that pickupModelPrefab
        // just needed an ItemDisplay component attached to it for the game method to not complain
        private static void MaterialFixForItemDisplayOnCharacter(ILContext il) {
            var cursor = new ILCursor(il);
            var forCounterLoc = 0;
            var itemDisplayLoc = 0;

            try {
                cursor.GotoNext(
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld("RoR2.CharacterModel", "parentedPrefabDisplays"),
                    i => i.MatchLdloc(out forCounterLoc)
                );

                cursor.GotoNext(
                    i => i.MatchCallOrCallvirt("RoR2.CharacterModel/ParentedPrefabDisplay", "get_itemDisplay"),
                    i => i.MatchStloc(out itemDisplayLoc)
                );
                cursor.Index += 2;

                cursor.Emit(OpCodes.Ldloc, itemDisplayLoc);
                cursor.Emit(OpCodes.Call, typeof(Object).GetMethodCached("op_Implicit"));
                cursor.Emit(OpCodes.Brfalse, cursor.MarkLabel());
                var brFalsePos = cursor.Index - 1;

                cursor.GotoNext(
                    i => i.MatchLdloc(forCounterLoc)
                );
                var label = cursor.MarkLabel();

                cursor.Index = brFalsePos;
                cursor.Next.Operand = label;
            }
            catch (Exception e) {
                R2API.Logger.LogError($"Exception in {nameof(MaterialFixForItemDisplayOnCharacter)} : Item mods without the {nameof(ItemDisplay)} component may not work correctly.\n{e}");
            }
        }

        // todo : allow override of existing item display rules
        // This method only allow the addition of custom rules.
        //
        private static void AddingItemDisplayRulesToCharacterModels(On.RoR2.ItemDisplayRuleSet.orig_Init orig) {
            orig();

            foreach (var bodyPrefab in BodyCatalog.allBodyPrefabs) {
                var characterModel = bodyPrefab.GetComponentInChildren<CharacterModel>();
                if (characterModel) {
                    if (!characterModel.itemDisplayRuleSet) {
                        characterModel.itemDisplayRuleSet = ScriptableObject.CreateInstance<ItemDisplayRuleSet>();
                    }
                    var modelName = characterModel.name;
                    var bodyName = bodyPrefab.name;
                    bool allowDefault = true;
                    if (noDefaultIDRSCharacterList.Contains(modelName) || noDefaultIDRSCharacterList.Contains(bodyName)) {
                        allowDefault = false;
                    }

                    foreach (var customItem in ItemDefinitions) {
                        var customRules = customItem.ItemDisplayRules;
                        if (customRules != null) {
                            //if a specific rule for this model exists, or the model has no rules for this item
                            if (customRules.TryGetRules(modelName, out ItemDisplayRule[]? rules) ||
                                customRules.TryGetRules(bodyName, out rules) ||
                                (
                                    allowDefault &&
                                    characterModel.itemDisplayRuleSet.GetItemDisplayRuleGroup(customItem.ItemDef.itemIndex).rules == null
                                )) {
                                characterModel.itemDisplayRuleSet.SetDisplayRuleGroup(customItem.ItemDef, new DisplayRuleGroup { rules = rules });
                            }
                        }
                    }

                    foreach (var customEquipment in EquipmentDefinitions) {
                        var customRules = customEquipment.ItemDisplayRules;
                        if (customRules != null) {
                            //if a specific rule for this model exists, or the model has no rules for this equipment
                            if (customRules.TryGetRules(modelName, out ItemDisplayRule[]? rules) ||
                                customRules.TryGetRules(bodyName, out rules) ||
                                (
                                    allowDefault &&
                                    characterModel.itemDisplayRuleSet.GetEquipmentDisplayRuleGroup(customEquipment.EquipmentDef.equipmentIndex).rules == null
                                )) {
                                characterModel.itemDisplayRuleSet.SetDisplayRuleGroup(customEquipment.EquipmentDef, new DisplayRuleGroup { rules = rules });
                            }
                        }
                    }

                    characterModel.itemDisplayRuleSet.GenerateRuntimeValues();
                }
            }
        }

        #endregion ItemDisplay Hooks

    }
}
