using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace R2API;

// ReSharper disable once InconsistentNaming
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class ItemAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".items";
    public const string PluginName = R2API.PluginName + ".Items";

    public static ObservableCollection<CustomItem?>? ItemDefinitions = new ObservableCollection<CustomItem?>();
    public static ObservableCollection<CustomEquipment?> EquipmentDefinitions = new ObservableCollection<CustomEquipment?>();

    private static ICollection<string> noDefaultIDRSCharacterList = new List<string>();

    private static List<string> customItemTags = new List<string>();

    public static int CustomItemCount, CustomEquipmentCount;

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;

    #region ModHelper Events and Hooks

    private static bool _hooksEnabled = false;

    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        IL.RoR2.CharacterModel.UpdateMaterials += MaterialFixForItemDisplayOnCharacter;
        On.RoR2.ItemDisplayRuleSet.Init += AddingItemDisplayRulesToCharacterModels;
        IL.RoR2.ItemCatalog.SetItemDefs += AddCustomTagsToItemCatalog;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        IL.RoR2.CharacterModel.UpdateMaterials -= MaterialFixForItemDisplayOnCharacter;
        On.RoR2.ItemDisplayRuleSet.Init -= AddingItemDisplayRulesToCharacterModels;
        IL.RoR2.ItemCatalog.SetItemDefs -= AddCustomTagsToItemCatalog;

        _hooksEnabled = false;
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
    public static bool Add(CustomItem? item)
    {
        ItemAPI.SetHooks();
        return AddItemInternal(item, Assembly.GetCallingAssembly());
    }

    internal static bool AddItemInternal(CustomItem item, Assembly addingAssembly)
    {
        if (!CatalogBlockers.GetAvailability<ItemDef>())
        {
            ItemsPlugin.Logger.LogError($"Too late ! Tried to add item: {item.ItemDef.nameToken} after the ItemCatalog has Initialized!");
        }

        if (!item.ItemDef)
        {
            ItemsPlugin.Logger.LogError("ItemDef is null ! Can't add the custom item.");
        }

        if (string.IsNullOrEmpty(item.ItemDef.name))
        {
            ItemsPlugin.Logger.LogError("ItemDef.name is null or empty ! Can't add the custom item.");
        }

        if (!item.ItemDef.pickupModelPrefab)
        {
            ItemsPlugin.Logger.LogWarning($"No ItemDef.pickupModelPrefab ({item.ItemDef.name}), the game will show nothing when the item is on the ground.");
        }

        if (item.ItemDisplayRules != null &&
            item.ItemDisplayRules.Dictionary.Values.Any(rules => rules.Any(rule => rule.ruleType == ItemDisplayRuleType.ParentedPrefab)))
        {
            if (item.ItemDisplayRules.HasInvalidDisplays(out var log))
            {
                ItemsPlugin.Logger.LogWarning($"Some of the ItemDisplayRules in the dictionary for CustomItem ({item.ItemDef}) have an invalid {nameof(ItemDisplayRule.followerPrefab)}. " +
                    $"(There are ItemDisplayRuleType.ParentedPrefab rules)," +
                    $"Logging invalid rules... (For full details, check the Log file)");
                ItemsPlugin.Logger.LogDebug(log.ToString());
            }
        }

        bool xmlSafe = false;
        try
        {
            XElement element = new(item.ItemDef.name);
            xmlSafe = true;
        }
        catch
        {
            ItemsPlugin.Logger.LogError($"Custom item '{item.ItemDef.name}' is not XMLsafe. Item not added.");
        }
        if (xmlSafe)
        {
            R2APIContentManager.HandleContentAddition(addingAssembly, item.ItemDef);
            ItemDefinitions.Add(item);
            return true;
        }

        if (item.ItemDef.tags == null)
        {
            ItemsPlugin.Logger.LogInfo($"Adding empty tags array to custom item '{item.ItemDef.name}'");
            item.ItemDef.tags = [];
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
    public static bool Add(CustomEquipment? item)
    {
        ItemAPI.SetHooks();
        return AddEquippmentInternal(item, Assembly.GetCallingAssembly());
    }

    private static bool AddEquippmentInternal(CustomEquipment equip, Assembly addingAssembly)
    {
        if (!CatalogBlockers.GetAvailability<EquipmentDef>())
        {
            ItemsPlugin.Logger.LogError($"Too late ! Tried to add equipment item: {equip.EquipmentDef.nameToken} after the EquipmentCatalog has initialized!");
        }

        if (equip.EquipmentDef == null)
        {
            ItemsPlugin.Logger.LogError("EquipmentDef is null ! Can't add the custom Equipment.");
        }

        if (string.IsNullOrEmpty(equip.EquipmentDef.name))
        {
            ItemsPlugin.Logger.LogError("EquipmentDef.name is null or empty ! Can't add the custom Equipment.");
        }

        if (!equip.EquipmentDef.pickupModelPrefab)
        {
            ItemsPlugin.Logger.LogWarning($"No EquipmentDef.pickupModelPrefab ({equip.EquipmentDef.name}), the game will show nothing when the equipment is on the ground.");
        }

        if (equip.ItemDisplayRules != null &&
            equip.ItemDisplayRules.Dictionary.Values.Any(rules => rules.Any(rule => rule.ruleType == ItemDisplayRuleType.ParentedPrefab)))
        {
            if (equip.ItemDisplayRules.HasInvalidDisplays(out var log))
            {
                ItemsPlugin.Logger.LogWarning($"Some of the ItemDisplayRules in the dictionary for CustomEquipment ({equip.EquipmentDef}) have an invalid {nameof(ItemDisplayRule.followerPrefab)}. " +
                    $"(There are ItemDisplayRuleType.ParentedPrefab rules)," +
                    $"Logging invalid rules... (For full details, check the Log file)");
                ItemsPlugin.Logger.LogDebug(log.ToString());
            }
        }

        bool xmlSafe = false;
        try
        {
            XElement element = new(equip.EquipmentDef.name);
            xmlSafe = true;
        }
        catch
        {
            ItemsPlugin.Logger.LogError($"Custom equipment '{equip.EquipmentDef.name}' is not XMLsafe. Equipment not added.");
        }
        if (xmlSafe)
        {
            R2APIContentManager.HandleContentAddition(addingAssembly, equip.EquipmentDef);
            EquipmentDefinitions.Add(equip);
            return true;
        }

        return false;
    }
    #endregion Add Methods

    #region ItemTags
    /// <summary>
    /// Add a custom item tag to the list of item tags.
    /// If this is called after the ItemCatalog inits then this will return -1 and ignore the custom item tag.
    /// </summary>
    /// <param name="name">The tag to add.</param>
    /// <returns>ItemTag value if added or already existent, (-1) cast to ItemTag otherwise</returns>
    public static ItemTag AddItemTag(string name)
    {
        ItemAPI.SetHooks();

        if (!CatalogBlockers.GetAvailability<ItemDef>())
        {
            ItemsPlugin.Logger.LogError($"Too late ! Tried to add itemTag: {name} after the ItemCatalog has Initialized!");
            return (ItemTag)(-1);
        }

        int result = (int)FindItemTagByName(name);
        if (result == -1)
        {
            customItemTags.Add(name);
            result = customItemTags.Count + (int)ItemTag.Count;
        }

        return (ItemTag)result;
    }

    /// <summary>
    /// Gets ItemTag value for tag of given name
    /// </summary>
    /// <param name="name">The tag name string to match</param>
    /// <returns>ItemTag value if found,(-1) cast to ItemTag otherwise</returns>
    public static ItemTag FindItemTagByName(string name)
    {
        ItemAPI.SetHooks();
        ItemTag result = (ItemTag)customItemTags.IndexOf(name);
        if ((int)result == -1)
        {
            if (Enum.TryParse(name, out result))
            {
                return result;
            }
            else
            {
                return (ItemTag)(-1);
            }
        }
        return (ItemTag)(result + 1 + (int)ItemTag.Count);
    }

    /// <summary>
    /// Applies given ItemTag to the ItemDef (by Tag Name Overload)
    /// </summary>
    /// <param name="tagName">The name of the tag to apply</param>
    /// <param name="item"> The ItemDef to apply the tag to</param>
    public static void ApplyTagToItem(string tagName, ItemDef item)
    {
        ItemAPI.SetHooks();
        ApplyTagToItem(FindItemTagByName(tagName), item);
    }

    /// <summary>
    /// Applies given ItemTag to the ItemDef
    /// </summary>
    /// <param name="tag">The ItemTag to apply</param>
    /// <param name="item"> The ItemDef to apply the tag to</param>
    public static void ApplyTagToItem(ItemTag tag, ItemDef item)
    {
        ItemAPI.SetHooks();
        HG.ArrayUtils.ArrayAppend(ref item.tags, tag);
        if (!CatalogBlockers.GetAvailability<ItemDef>())
        {
            HG.ArrayUtils.ArrayAppend(ref ItemCatalog.itemIndicesByTag[(int)tag], item.itemIndex);
        }
    }
    #endregion

    #region Other Modded Content Support

    /// <summary>
    /// Prevents bodies and charactermodels matching this name from having nonspecific item display rules applied to them
    /// </summary>
    /// <param name="bodyPrefabOrCharacterModelName">The string to match</param>
    public static void DoNotAutoIDRSFor(string bodyPrefabOrCharacterModelName)
    {
        ItemAPI.SetHooks();
        noDefaultIDRSCharacterList.Add(bodyPrefabOrCharacterModelName);
    }

    /// <summary>
    /// Prevent prefabs with this name having nonspecific item display rules applied to them
    /// </summary>
    /// <param name="bodyPrefab">The body prefab to match</param>
    public static void DoNotAutoIDRSFor(GameObject bodyPrefab)
    {
        ItemAPI.SetHooks();
        var characterModel = bodyPrefab.GetComponentInChildren<CharacterModel>();
        if (characterModel)
        {
            DoNotAutoIDRSFor(bodyPrefab.name);
        }
    }

    #endregion

    #region ItemDisplay Hooks

    // With how unfriendly it is to makes your 3D Prefab work with shaders from the game,
    // makes it so that if the custom prefab doesnt have rendering support for when the player is cloaked, or burning, still display the item on the player.
    // iDeath : This hook was made back when I didn't know that pickupModelPrefab
    // just needed an ItemDisplay component attached to it for the game method to not complain
    private static void MaterialFixForItemDisplayOnCharacter(ILContext il)
    {
        var cursor = new ILCursor(il);
        var forCounterLoc = 0;
        var itemDisplayLoc = 0;

        try
        {
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
        catch (Exception e)
        {
            ItemsPlugin.Logger.LogError($"Exception in {nameof(MaterialFixForItemDisplayOnCharacter)} : Item mods without the {nameof(ItemDisplay)} component may not work correctly.\n{e}");
        }
    }

    // todo : allow override of existing item display rules
    // This method only allow the addition of custom rules.
    //
    private static void AddingItemDisplayRulesToCharacterModels(On.RoR2.ItemDisplayRuleSet.orig_Init orig)
    {
        orig();

        foreach (var bodyPrefab in BodyCatalog.allBodyPrefabs)
        {
            var characterModel = bodyPrefab.GetComponentInChildren<CharacterModel>();
            if (characterModel)
            {
                if (!characterModel.itemDisplayRuleSet)
                {
                    characterModel.itemDisplayRuleSet = ScriptableObject.CreateInstance<ItemDisplayRuleSet>();
                }
                var modelName = characterModel.name;
                var bodyName = bodyPrefab.name;
                bool allowDefault = true;
                if (noDefaultIDRSCharacterList.Contains(modelName) || noDefaultIDRSCharacterList.Contains(bodyName))
                {
                    allowDefault = false;
                }

                foreach (var customItem in ItemDefinitions)
                {
                    var customRules = customItem.ItemDisplayRules;
                    if (customRules != null)
                    {
                        //if a specific rule for this model exists, or the model has no rules for this item
                        if (customRules.TryGetRules(modelName, out ItemDisplayRule[]? rules) ||
                            customRules.TryGetRules(bodyName, out rules) ||
                            (
                                allowDefault &&
                                characterModel.itemDisplayRuleSet.GetItemDisplayRuleGroup(customItem.ItemDef.itemIndex).rules == null
                            ))
                        {
                            characterModel.itemDisplayRuleSet.SetDisplayRuleGroup(customItem.ItemDef, new DisplayRuleGroup { rules = rules });
                        }
                    }
                }

                foreach (var customEquipment in EquipmentDefinitions)
                {
                    var customRules = customEquipment.ItemDisplayRules;
                    if (customRules != null)
                    {
                        //if a specific rule for this model exists, or the model has no rules for this equipment
                        if (customRules.TryGetRules(modelName, out ItemDisplayRule[]? rules) ||
                            customRules.TryGetRules(bodyName, out rules) ||
                            (
                                allowDefault &&
                                characterModel.itemDisplayRuleSet.GetEquipmentDisplayRuleGroup(customEquipment.EquipmentDef.equipmentIndex).rules == null
                            ))
                        {
                            characterModel.itemDisplayRuleSet.SetDisplayRuleGroup(customEquipment.EquipmentDef, new DisplayRuleGroup { rules = rules });
                        }
                    }
                }

                characterModel.itemDisplayRuleSet.GenerateRuntimeValues();
            }
        }
    }

    #endregion ItemDisplay Hooks

    #region ItemTag Hooks
    private static void AddCustomTagsToItemCatalog(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After, x => x.MatchLdcI4((int)ItemTag.Count)))
        {
            c.EmitDelegate<Func<int>>(() => customItemTags.Count + 1);
            c.Emit(OpCodes.Add);
        }
    }
    #endregion
}
