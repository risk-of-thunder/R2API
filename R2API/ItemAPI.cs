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
using Object = UnityEngine.Object;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace R2API {

    [R2APISubmodule]
    // ReSharper disable once InconsistentNaming
    public static class ItemAPI {
        public static ObservableCollection<CustomItem?>? ItemDefinitions = new ObservableCollection<CustomItem?>();
        public static ObservableCollection<CustomEquipment?> EquipmentDefinitions = new ObservableCollection<CustomEquipment?>();

        private static ICollection<string> noDefaultIDRSCharacterList = new List<string>();

        public static int CustomItemCount, CustomEquipmentCount;

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        #region ModHelper Events and Hooks

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            IL.RoR2.CharacterModel.UpdateMaterials += MaterialFixForItemDisplayOnCharacter;
            On.RoR2.ItemDisplayRuleSet.Init += AddingItemDisplayRulesToCharacterModels;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
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
            return AddInternal(item, Assembly.GetCallingAssembly());
        }

        internal static bool AddInternal(CustomItem item, Assembly addingAssembly) {

            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(ItemAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ItemAPI)})]");
            }

            if (!CatalogBlockers.GetAvailability<ItemDef>()) {
                R2API.Logger.LogError($"Too late ! Tried to add item: {item.ItemDef.nameToken} after the ItemCatalog has Initialized!");
            }

            if (!item.ItemDef) {
                R2API.Logger.LogError("ItemDef is null ! Can't add the custom item.");
            }

            if (string.IsNullOrEmpty(item.ItemDef.name)) {
                R2API.Logger.LogError("ItemDef.name is null or empty ! Can't add the custom item.");
            }

            if (!item.ItemDef.pickupModelPrefab) {
                R2API.Logger.LogWarning($"No ItemDef.pickupModelPrefab ({item.ItemDef.name}), the game will show nothing when the item is on the ground.");
            }
            else if (item.ItemDisplayRules != null &&
                     item.ItemDisplayRules.Dictionary.Values.Any(rules => rules.Any(rule => rule.ruleType == ItemDisplayRuleType.ParentedPrefab)) &&
                     !item.ItemDef.pickupModelPrefab.GetComponent<ItemDisplay>()) {
                R2API.Logger.LogWarning($"ItemDef.pickupModelPrefab ({item.ItemDef.name}) does not have an ItemDisplay component attached to it " +
                    "(there are ItemDisplayRuleType.ParentedPrefab rules), " +
                    "the pickup model should have one and have atleast a rendererInfo in it for having correct visibility levels.");
            }

            bool xmlSafe = false;
            try {
                XElement element = new(item.ItemDef.name);
                xmlSafe = true;
            }
            catch {
                R2API.Logger.LogError($"Custom item '{item.ItemDef.name}' is not XMLsafe. Item not added.");
            }
            if (xmlSafe) {
                R2APIContentManager.HandleContentAddition(addingAssembly, item.ItemDef);
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
            return AddInternal(item, Assembly.GetCallingAssembly());
        }

        internal static bool AddInternal(CustomEquipment item, Assembly addingAssembly) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(ItemAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ItemAPI)})]");
            }

            if (!CatalogBlockers.GetAvailability<EquipmentDef>()) {
                R2API.Logger.LogError($"Too late ! Tried to add equipment item: {item.EquipmentDef.nameToken} after the EquipmentCatalog has initialized!");
            }

            if (item.EquipmentDef == null) {
                R2API.Logger.LogError("EquipmentDef is null ! Can't add the custom Equipment.");
            }

            if (string.IsNullOrEmpty(item.EquipmentDef.name)) {
                R2API.Logger.LogError("EquipmentDef.name is null or empty ! Can't add the custom Equipment.");
            }

            if (!item.EquipmentDef.pickupModelPrefab) {
                R2API.Logger.LogWarning($"No EquipmentDef.pickupModelPrefab ({item.EquipmentDef.name}), the game will show nothing when the item is on the ground.");
            }
            else if (item.ItemDisplayRules != null &&
                     item.ItemDisplayRules.Dictionary.Values.Any(rules => rules.Any(rule => rule.ruleType == ItemDisplayRuleType.ParentedPrefab)) &&
                     !item.EquipmentDef.pickupModelPrefab.GetComponent<ItemDisplay>()) {
                R2API.Logger.LogWarning($"EquipmentDef.pickupModelPrefab ({item.EquipmentDef.name}) does not have an ItemDisplay component attached to it " +
                    "(there are ItemDisplayRuleType.ParentedPrefab rules), " +
                    "the pickup model should have one and have atleast a rendererInfo in it for having correct visibility levels.");
            }

            bool xmlSafe = false;
            try {
                XElement element = new(item.EquipmentDef.name);
                xmlSafe = true;
            }
            catch {
                R2API.Logger.LogError($"Custom equipment '{item.EquipmentDef.name}' is not XMLsafe. Item not added.");
            }
            if (xmlSafe) {
                R2APIContentManager.HandleContentAddition(addingAssembly, item.EquipmentDef);
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
            ItemDef.tier = tier;
            ItemDef.unlockableDef = unlockableDef;

            ItemDisplayRules = new ItemDisplayRuleDict(itemDisplayRules);
        }

        public CustomItem(string name, string nameToken,
            string descriptionToken, string loreToken,
            string pickupToken,
            Sprite pickupIconSprite, GameObject pickupModelPrefab,
            ItemTier tier, ItemTag[] tags,
            bool canRemove,
            bool hidden,
            UnlockableDef unlockableDef = null,
            ItemDisplayRuleDict? itemDisplayRules = null) {

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
            ItemDef.tier = tier;
            ItemDef.unlockableDef = unlockableDef;

            ItemDisplayRules = itemDisplayRules;
        }
    }

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

    public class ItemDisplayRuleDict {

        /// <summary>
        /// Get the applicable rule for this charactermodel. Returns the default rules if no specific rule is found.
        /// </summary>
        /// <param name="bodyPrefabName">The model to look for. Null and empty strings are also accepted.</param>
        /// <returns>The item display rules for this model, or the default rules if no specifics are found.</returns>
        public ItemDisplayRule[]? this[string? bodyPrefabName] {
            get {
                if (string.IsNullOrEmpty(bodyPrefabName) || !Dictionary.ContainsKey(bodyPrefabName))
                    return DefaultRules;
                else
                    return Dictionary[bodyPrefabName];
            }
            set {
                if (string.IsNullOrEmpty(bodyPrefabName)) {
                    R2API.Logger.LogWarning("DefaultRules overwritten with Indexer! Please set them with the constructor instead!");
                    DefaultRules = value;
                    return;
                }

                if (Dictionary.ContainsKey(bodyPrefabName)) {
                    Dictionary[bodyPrefabName] = value;
                }
                else {
                    Dictionary.Add(bodyPrefabName, value);
                }
            }
        }


        /// <summary>
        /// Equivalent to using the set property of the indexer, but added bonus is the ability to ignore the array wrapper normally needed.
        /// </summary>
        /// <param name="bodyPrefabName"></param>
        /// <param name="itemDisplayRules"></param>
        public void Add(string? bodyPrefabName, params ItemDisplayRule[]? itemDisplayRules) {
            this[bodyPrefabName] = itemDisplayRules;
        }

        /// <summary>
        /// Safe way of getting a characters rules, with the promise that the out is always filled.
        /// </summary>
        /// <param name="bodyPrefabName"></param>
        /// <param name="itemDisplayRules">The specific rules for this model, or if false is returned, the default rules.</param>
        /// <returns>True if there's a specific rule for this model. False otherwise.</returns>
        public bool TryGetRules(string? bodyPrefabName, out ItemDisplayRule[] itemDisplayRules) {
            itemDisplayRules = this[bodyPrefabName];
            return bodyPrefabName != null && Dictionary.ContainsKey(bodyPrefabName);
        }

        /// <summary>
        /// The default rules to apply when no matching model is found.
        /// </summary>
        public ItemDisplayRule[]? DefaultRules { get; private set; }

        internal Dictionary<string, ItemDisplayRule[]?> Dictionary { get; private set; }

        public ItemDisplayRuleDict(params ItemDisplayRule[]? defaultRules) {
            DefaultRules = defaultRules;
            Dictionary = new Dictionary<string, ItemDisplayRule[]?>();
        }
    }
}
