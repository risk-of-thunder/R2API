using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using UnityEngine;
using Console = System.Console;
using Object = UnityEngine.Object;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable 618 // PickupIndex being obsolete (but still being used in the game code)

namespace R2API {
    [R2APISubmodule]
    // ReSharper disable once InconsistentNaming
    public static class ItemAPI {
        public static ObservableCollection<CustomItem> ItemDefinitions = new ObservableCollection<CustomItem>();
        public static ObservableCollection<CustomEquipment> EquipmentDefinitions = new ObservableCollection<CustomEquipment>();

        private static bool _itemCatalogInitialized;
        private static bool _equipmentCatalogInitialized;

        public static int OriginalItemCount, OriginalEquipmentCount;
        public static int CustomItemCount, CustomEquipmentCount;

        public static bool Loaded {
            get => _loaded;
            set => _loaded = value;
        }

        private static bool _loaded;

        #region ModHelper Events and Hooks
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            IL.RoR2.ItemCatalog.DefineItems += GetOriginalItemCountHook;
            IL.RoR2.EquipmentCatalog.Init += GetOriginalEquipmentCountHook;

            ItemCatalog.modHelper.getAdditionalEntries += AddItemAction;
            EquipmentCatalog.modHelper.getAdditionalEntries += AddEquipmentAction;

            IL.RoR2.CharacterModel.UpdateMaterials += MaterialFixForItemDisplayOnCharacter;
            R2API.R2APIStart += AddingItemDisplayRulesToCharacterModels;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            IL.RoR2.ItemCatalog.DefineItems -= GetOriginalItemCountHook;
            IL.RoR2.EquipmentCatalog.Init -= GetOriginalEquipmentCountHook;

            ItemCatalog.modHelper.getAdditionalEntries -= AddItemAction;
            EquipmentCatalog.modHelper.getAdditionalEntries -= AddEquipmentAction;

            IL.RoR2.CharacterModel.UpdateMaterials -= MaterialFixForItemDisplayOnCharacter;
            R2API.R2APIStart -= AddingItemDisplayRulesToCharacterModels;
        }

        private static void GetOriginalItemCountHook(ILContext il) {
            var cursor = new ILCursor(il);

            cursor.GotoNext(
                i => i.MatchLdcI4(out OriginalItemCount),
                i => i.MatchNewarr<ItemDef>()
            );
        }

        private static void GetOriginalEquipmentCountHook(ILContext il) {
            var cursor = new ILCursor(il);

            cursor.GotoNext(
                i => i.MatchLdcI4(out OriginalEquipmentCount),
                i => i.MatchCall<Array>("Resize")
            );
        }

        private static void AddItemAction(List<ItemDef> itemDefinitions) {
            foreach (var customItem in ItemDefinitions) {
                itemDefinitions.Add(customItem.ItemDef);

                R2API.Logger.LogInfo($"Custom Item: {customItem.ItemDef.nameToken} added");
            }

            var t1Items = ItemDefinitions.Where(x => x.ItemDef.tier == ItemTier.Tier1).Select(x => x.ItemDef.itemIndex).ToArray();
            var t2Items = ItemDefinitions.Where(x => x.ItemDef.tier == ItemTier.Tier2).Select(x => x.ItemDef.itemIndex).ToArray();
            var t3Items = ItemDefinitions.Where(x => x.ItemDef.tier == ItemTier.Tier3).Select(x => x.ItemDef.itemIndex).ToArray();
            var lunarItems = ItemDefinitions.Where(x => x.ItemDef.tier == ItemTier.Lunar).Select(x => x.ItemDef.itemIndex).ToArray();
            var bossItems = ItemDefinitions.Where(x => x.ItemDef.tier == ItemTier.Boss).Select(x => x.ItemDef.itemIndex).ToArray();

            ItemDropAPI.AddToDefaultByTier(ItemTier.Tier1, t1Items);
            ItemDropAPI.AddToDefaultByTier(ItemTier.Tier2, t2Items);
            ItemDropAPI.AddToDefaultByTier(ItemTier.Tier3, t3Items);
            ItemDropAPI.AddToDefaultByTier(ItemTier.Lunar, lunarItems);
            ItemDropAPI.AddToDefaultByTier(ItemTier.Boss, bossItems);

            _itemCatalogInitialized = true;
        }

        private static void AddEquipmentAction(List<EquipmentDef> equipmentDefinitions) {
            foreach (var customEquipment in EquipmentDefinitions) {
                equipmentDefinitions.Add(customEquipment.EquipmentDef);

                R2API.Logger.LogInfo($"Custom Equipment: {customEquipment.EquipmentDef.nameToken} added");
            }

            var equipments = EquipmentDefinitions.Where(c => c.EquipmentDef.canDrop).Select(x => x.EquipmentDef.equipmentIndex).ToArray();

            ItemDropAPI.AddToDefaultEquipment(equipments);

            _equipmentCatalogInitialized = true;
        }
        #endregion

        #region Add Methods
        /// <summary>
        /// Add a custom item to the list of available items.
        /// Value for ItemDef.ItemIndex can be ignored.
        /// If this is called after the ItemCatalog inits then this will return false and ignore the custom item.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>the ItemIndex of your item if added. -1 otherwise</returns>
        public static ItemIndex Add(CustomItem item) {
            if (!Loaded) {
                R2API.Logger.LogError("ItemAPI is not loaded. Please use [R2APISubmoduleDependency(nameof(ItemAPI)]");
                return ItemIndex.None;
            }

            if (_itemCatalogInitialized) {
                R2API.Logger.LogError($"Too late ! Tried to add item: {item.ItemDef.nameToken} after the item list was created");
                return ItemIndex.None;
            }

            item.ItemDef.itemIndex = (ItemIndex)OriginalItemCount + CustomItemCount++;
            ItemDefinitions.Add(item);
            return item.ItemDef.itemIndex;
        }

        /// <summary>
        /// Add a custom equipment item to the list of available items.
        /// Value for EquipmentDef.ItemIndex can be ignored.
        /// If this is called after the EquipmentCatalog inits then this will return false and ignore the custom equipment item.
        /// </summary>
        /// <param name="item">The equipment item to add.</param>
        /// <returns>the EquipmentIndex of your item if added. -1 otherwise</returns>
        public static EquipmentIndex Add(CustomEquipment item) {
            if (!Loaded) {
                R2API.Logger.LogError("ItemAPI is not loaded. Please use [R2APISubmoduleDependency(nameof(ItemAPI)]");
                return EquipmentIndex.None;
            }

            if (_equipmentCatalogInitialized) {
                R2API.Logger.LogError($"Too late ! Tried to add equipment item: {item.EquipmentDef.nameToken} after the equipment list was created");
                return EquipmentIndex.None;
            }

            item.EquipmentDef.equipmentIndex = (EquipmentIndex)OriginalEquipmentCount + CustomEquipmentCount++;
            EquipmentDefinitions.Add(item);
            return item.EquipmentDef.equipmentIndex;
        }

        [Obsolete("Use the Add() method from BuffAPI instead.")]
        public static BuffIndex Add(CustomBuff buff) {
            if (!BuffAPI.Loaded) {
                try {
                    BuffAPI.SetHooks();
                    BuffAPI.Loaded = true;
                }
                catch (Exception e) {
                    R2API.Logger.LogError($"BuffAPI hooks failed to initialize. Disabling the submodule. {e}");
                    BuffAPI.UnsetHooks();
                }
            }
                
            return BuffAPI.Add(buff);
        }

        [Obsolete("Use the Add() method from EliteAPI instead.")]
        public static EliteIndex Add(CustomElite elite) {
            if (!EliteAPI.Loaded) {
                try {
                    EliteAPI.SetHooks();
                    EliteAPI.Loaded = true;
                }
                catch (Exception e) {
                    R2API.Logger.LogError($"EliteAPI hooks failed to initialize. Disabling the submodule. {e}");
                    EliteAPI.UnsetHooks();
                }
            }
                
            return EliteAPI.Add(elite);
        }

        [Obsolete("Use the Add() method that will directly returns the correct enum type (ItemIndex) instead.")]
        public static int AddCustomItem(CustomItem item) {
            return (int)Add(item);
        }

        [Obsolete("Use the Add() method that will directly returns the correct enum type (EquipmentIndex) instead.")]
        public static int AddCustomEquipment(CustomEquipment item) {
            return (int)Add(item);
        }

        [Obsolete("Use the Add() method that will directly returns the correct enum type (BuffIndex) instead.")]
        public static int AddCustomBuff(CustomBuff buff) {
            return (int)Add(buff);
        }

        [Obsolete("Use the Add() method that will directly returns the correct enum type (EliteIndex) instead.")]
        public static int AddCustomElite(CustomElite elite) {
            return (int)Add(elite);
        }
        #endregion

        #region ItemDisplay Hooks
        // With how unfriendly it is to makes your 3D Prefab work with shaders from the game,
        // makes it so that if the custom prefab doesnt have rendering support for when the player is cloaked, or burning, still display the item on the player.
        private static void MaterialFixForItemDisplayOnCharacter(ILContext il) {
            var cursor = new ILCursor(il);
            var forCounterLoc = 0;
            var itemDisplayLoc = 0;

            cursor.GotoNext(
                i => i.MatchLdarg(0),
                i => i.MatchLdfld("RoR2.CharacterModel", "parentedPrefabDisplays"),
                i => i.MatchLdloc(out forCounterLoc)
            );

            cursor.GotoNext(
                i => i.MatchCall("RoR2.CharacterModel/ParentedPrefabDisplay", "get_itemDisplay"),
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

        private static void AddingItemDisplayRulesToCharacterModels(object _, EventArgs __) {
            foreach(GameObject o in BodyCatalog.allBodyPrefabs) {
                CharacterModel cm = o.GetComponentInChildren<CharacterModel>();
                if (cm!=null && cm.itemDisplayRuleSet!=null) {
                    string name = cm.name;
                    foreach(var customItem in ItemDefinitions) {
                        var customRules = customItem.ItemDisplayRules;
                        if (customRules != null) {
                            //if a specific rule for this model exists, or the model has no rules for this item
                            if(customRules.TryGetRules(name, out ItemDisplayRule[] rules) || cm.itemDisplayRuleSet.GetItemDisplayRuleGroup(customItem.ItemDef.itemIndex).rules == null) {
                                cm.itemDisplayRuleSet.SetItemDisplayRuleGroup(customItem.ItemDef.name, new DisplayRuleGroup { rules = rules });
                            }
                        }
                    }

                    foreach (var customEquipment in EquipmentDefinitions) {
                        var customRules = customEquipment.ItemDisplayRules;
                        if (customRules != null) {
                            //if a specific rule for this model exists, or the model has no rules for this equipment
                            if (customRules.TryGetRules(name, out ItemDisplayRule[] rules) || cm.itemDisplayRuleSet.GetEquipmentDisplayRuleGroup(customEquipment.EquipmentDef.equipmentIndex).rules == null) {
                                cm.itemDisplayRuleSet.SetEquipmentDisplayRuleGroup(customEquipment.EquipmentDef.name, new DisplayRuleGroup { rules = rules });
                            }
                        }
                    }
                    cm.itemDisplayRuleSet.InvokeMethod("GenerateRuntimeValues");
                }
            }
        }


        #endregion
        
        public static bool IsCustomItemOrEquipment(PickupIndex pickupIndex) {
            var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            return (int)pickupDef.itemIndex >= OriginalItemCount || (int)pickupDef.equipmentIndex >= OriginalEquipmentCount;
        }
    }


    public class CustomItem {
        public ItemDef ItemDef;
        public ItemDisplayRuleDict ItemDisplayRules;

        public CustomItem(ItemDef itemDef, ItemDisplayRule[] itemDisplayRules) {
            ItemDef = itemDef;
            ItemDisplayRules = new ItemDisplayRuleDict(itemDisplayRules);
        }

        public CustomItem(ItemDef itemDef, ItemDisplayRuleDict itemDisplayRules) {
            ItemDef = itemDef;
            ItemDisplayRules = itemDisplayRules;
        }
    }

    public class CustomEquipment {
        public EquipmentDef EquipmentDef;
        public ItemDisplayRuleDict ItemDisplayRules;

        public CustomEquipment(EquipmentDef equipmentDef, ItemDisplayRule[] itemDisplayRules) {
            EquipmentDef = equipmentDef;
            ItemDisplayRules = new ItemDisplayRuleDict(itemDisplayRules);
        }

        public CustomEquipment(EquipmentDef equipmentDef, ItemDisplayRuleDict itemDisplayRules) {
            EquipmentDef = equipmentDef;
            ItemDisplayRules = itemDisplayRules;
        }
    }

    public class ItemDisplayRuleDict {
        public ItemDisplayRule[] this[string CharacterModelName] {
            get {
                if (string.IsNullOrEmpty(CharacterModelName) || !Dictionary.ContainsKey(CharacterModelName))
                    return DefaultRules;
                else
                    return Dictionary[CharacterModelName];
            }
            set {
                if (string.IsNullOrEmpty(CharacterModelName))
                {
                    R2API.Logger.LogWarning("DefaultRules overwritten with Indexer! Please set them with the constructor instead!");
                    DefaultRules = value;
                    return;
                }
                if (Dictionary.ContainsKey(CharacterModelName)) {
                    Dictionary[CharacterModelName] = value;
                } else {
                    Dictionary.Add(CharacterModelName, value);
                }
            }
        }

        /// <summary>
        /// Equivalent to using the set property of the indexer, but added bonus is the ability to ignore the array wrapper normally needed.
        /// </summary>
        /// <param name="CharacterModelName"></param>
        /// <param name="itemDisplayRules"></param>
        public void Add(string CharacterModelName, params ItemDisplayRule[] itemDisplayRules) {
            this[CharacterModelName] = itemDisplayRules;
        }

        /// <summary>
        /// Safe way of getting a characters rules, with the promise that the out is always filled.
        /// </summary>
        /// <param name="CharacterModelName"></param>
        /// <param name="itemDisplayRules">The specific rules for this model, or if false is returned, the default rules.</param>
        /// <returns></returns>
        public bool TryGetRules(string CharacterModelName, out ItemDisplayRule[] itemDisplayRules) {
            itemDisplayRules = this[CharacterModelName];
            return itemDisplayRules == DefaultRules;
        }

        public ItemDisplayRule[] DefaultRules {get; private set;}

        private Dictionary<string, ItemDisplayRule[]> Dictionary;
        public ItemDisplayRuleDict(params ItemDisplayRule[] defaultRules) {
            DefaultRules = defaultRules;
            Dictionary = new Dictionary<string, ItemDisplayRule[]>();
        }
    }
}
