using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using EntityStates.Huntress;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using RoR2.Stats;
using RoR2.UI;
using RoR2.UI.LogBook;
using Object = UnityEngine.Object;
#pragma warning disable 618 // PickupIndex being obsolete (but still being used in the game code)

namespace R2API {
    [R2APISubmodule]
    // ReSharper disable once InconsistentNaming
    public static class ItemAPI {
        public static ObservableCollection<CustomItem> ItemDefinitions = new ObservableCollection<CustomItem>();
        public static ObservableCollection<CustomEquipment> EquipmentDefinitions = new ObservableCollection<CustomEquipment>();
        public static ObservableCollection<CustomBuff> BuffDefinitions = new ObservableCollection<CustomBuff>();
        public static ObservableCollection<CustomElite> EliteDefinitions = new ObservableCollection<CustomElite>();

        private static bool _itemCatalogInitialized;
        private static bool _equipmentCatalogInitialized;
        private static bool _buffCatalogInitialized;
        private static bool _eliteCatalogInitialized;

        public static int OriginalItemCount, OriginalEquipmentCount, OriginalBuffCount, OriginalEliteCount;
        public static int CustomItemCount, CustomEquipmentCount, CustomBuffCount, CustomEliteCount;

        #region ModHelper Events and Hooks
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            // Temporary fix as the getter from the buffCount propriety doesnt retrieve from BuffCatalog.buffDefs.Length directly
            On.RoR2.BuffCatalog.Init += orig => {
                orig();
                typeof(BuffCatalog).SetPropertyValue("buffCount", typeof(BuffCatalog).GetFieldValue<BuffDef[]>("buffDefs").Length);
            };

            IL.RoR2.ItemCatalog.DefineItems += il => {
                var cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(out OriginalItemCount),
                    i => i.MatchNewarr<ItemDef>()
                );
            };

            IL.RoR2.EquipmentCatalog.Init += il => {
                var cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(out OriginalEquipmentCount),
                    i => i.MatchCallOrCallvirt<Array>("Resize")
                );
            };

            IL.RoR2.BuffCatalog.Init += il => {
                var cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(out OriginalBuffCount),
                    i => i.MatchNewarr<BuffDef>()
                );
            };

            IL.RoR2.EliteCatalog.cctor += il => {
                var cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(out OriginalEliteCount),
                    i => i.MatchNewarr<EliteDef>()
                );
            };

            ItemCatalog.modHelper.getAdditionalEntries += AddItemAction;
            EquipmentCatalog.modHelper.getAdditionalEntries += AddEquipmentAction;
            BuffCatalog.modHelper.getAdditionalEntries += AddBuffAction;
            EliteCatalog.modHelper.getAdditionalEntries += AddEliteAction;

            IL.RoR2.CharacterModel.UpdateMaterials += MaterialFixForItemDisplayOnCharacter;
            On.RoR2.CharacterModel.Start += AddingItemDisplayRulesToCharacterModels;

            On.RoR2.UserProfile.SaveFieldAttribute.SetupPickupsSet += SaveFieldAttributeOnSetupPickupsSet;
            On.RoR2.UserProfile.DiscoverPickup += UserProfileOnDiscoverPickup;
            On.RoR2.Stats.PerItemStatDef.RegisterStatDefs += PerItemStatDefOnRegisterStatDefs;
            On.RoR2.Stats.PerEquipmentStatDef.RegisterStatDefs += PerEquipmentStatDefOnRegisterStatDefs;
            IL.RoR2.Stats.StatManager.ProcessCharacterUpdateEvents += StatManagerOnProcessCharacterUpdateEvents;
            On.RoR2.Stats.StatManager.OnServerItemGiven += StatManagerOnOnServerItemGiven;
            On.RoR2.Stats.StatManager.OnEquipmentActivated += StatManagerOnOnEquipmentActivated;

            On.RoR2.UI.LogBook.LogBookController.GetPickupStatus += LogBookControllerOnGetPickupStatus;
            On.RoR2.UI.LogBook.PageBuilder.AddSimplePickup += PageBuilderOnAddSimplePickup;
            On.RoR2.UI.LogBook.LogBookController.GetPickupTooltipContent += LogBookControllerOnGetPickupTooltipContent;
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

            var equipments = EquipmentDefinitions.Where(c => c.EquipmentDef.canDrop && !c.EquipmentDef.isLunar).Select(x => x.EquipmentDef.equipmentIndex).ToArray();
            var lunarEquipments = EquipmentDefinitions.Where(c => c.EquipmentDef.canDrop && c.EquipmentDef.isLunar).Select(x => x.EquipmentDef.equipmentIndex).ToList();

            ItemDropAPI.AddToDefaultEquipment(equipments);
            ItemDropAPI.AddDrops(ItemDropLocation.LunarChest, lunarEquipments.ToSelection());

            _equipmentCatalogInitialized = true;
        }

        private static void AddBuffAction(List<BuffDef> buffDefinitions) {
            foreach (var customBuff in BuffDefinitions) {
                buffDefinitions.Add(customBuff.BuffDef);

                R2API.Logger.LogInfo($"Custom Buff: {customBuff.BuffDef.name} added");
            }

            _buffCatalogInitialized = true;
        }

        private static void AddEliteAction(List<EliteDef> eliteDefinitions) {
            foreach (var customElite in EliteDefinitions) {
                eliteDefinitions.Add(customElite.EliteDef);

                R2API.Logger.LogInfo($"Custom Elite: {customElite.EliteDef.modifierToken} added");
            }

            _eliteCatalogInitialized = true;
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
        public static int AddCustomItem(CustomItem item) {
            if (_itemCatalogInitialized) {
                R2API.Logger.LogError($"Too late ! Tried to add item: {item.ItemDef.nameToken} after the item list was created");
                return -1;
            }

            item.ItemDef.itemIndex = (ItemIndex) OriginalItemCount + CustomItemCount++;
            ItemDefinitions.Add(item);
            return (int) item.ItemDef.itemIndex;
        }

        /// <summary>
        /// Add a custom equipment item to the list of available items.
        /// Value for EquipmentDef.ItemIndex can be ignored.
        /// If this is called after the EquipmentCatalog inits then this will return false and ignore the custom equipment item.
        /// </summary>
        /// <param name="item">The equipment item to add.</param>
        /// <returns>the EquipmentIndex of your item if added. -1 otherwise</returns>
        public static int AddCustomEquipment(CustomEquipment item) {
            if (_equipmentCatalogInitialized) {
                R2API.Logger.LogError($"Too late ! Tried to add equipment item: {item.EquipmentDef.nameToken} after the equipment list was created");
                return -1;
            }

            item.EquipmentDef.equipmentIndex = (EquipmentIndex) OriginalEquipmentCount + CustomEquipmentCount++;
            EquipmentDefinitions.Add(item);
            return (int) item.EquipmentDef.equipmentIndex;
        }

        /// <summary>
        /// Add a custom buff to the list of available buffs.
        /// Value for BuffDef.buffIndex can be ignored.
        /// If this is called after the BuffCatalog inits then this will return false and ignore the custom buff.
        /// </summary>
        /// <param name="buff">The buff to add.</param>
        /// <returns>the BuffIndex of your item if added. -1 otherwise</returns>
        public static int AddCustomBuff(CustomBuff buff) {
            if (_buffCatalogInitialized) {
                R2API.Logger.LogError($"Too late ! Tried to add buff: {buff.BuffDef.name} after the buff list was created");
                return -1;
            }

            buff.BuffDef.buffIndex = (BuffIndex) OriginalBuffCount + CustomBuffCount++;
            BuffDefinitions.Add(buff);
            return (int) buff.BuffDef.buffIndex;
        }

        /// <summary>
        /// Add a custom item to the list of available elites.
        /// Value for EliteDef.eliteIndex can be ignored.
        /// If this is called after the ItemCatalog inits then this will return false and ignore the custom elite.
        /// </summary>
        /// <param name="elite">The elite to add.</param>
        /// <returns>the EliteIndex of your item if added. -1 otherwise</returns>
        public static int AddCustomElite(CustomElite elite) {
            if (_eliteCatalogInitialized) {
                R2API.Logger.LogError($"Too late ! Tried to add iteliteem: {elite.EliteDef.modifierToken} after the elite list was created");
                return -1;
            }

            elite.EliteDef.eliteIndex = (EliteIndex) OriginalEliteCount + CustomEliteCount++;
            EliteDefinitions.Add(elite);
            return (int) elite.EliteDef.eliteIndex;
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
        
        private static void AddingItemDisplayRulesToCharacterModels(On.RoR2.CharacterModel.orig_Start orig, CharacterModel self) {
            orig(self);

            foreach (var customItem in ItemDefinitions) {
                if (customItem.ItemDisplayRules != null) {
                    var displayRuleGroup = new DisplayRuleGroup { rules = customItem.ItemDisplayRules };
                    self.itemDisplayRuleSet.SetItemDisplayRuleGroup(customItem.ItemDef.name, displayRuleGroup);
                }
            }

            foreach (var customEquipment in EquipmentDefinitions) {
                if (customEquipment.ItemDisplayRules != null) {
                    var displayRuleGroup = new DisplayRuleGroup {rules = customEquipment.ItemDisplayRules};
                    self.itemDisplayRuleSet.SetItemDisplayRuleGroup(customEquipment.EquipmentDef.name,  displayRuleGroup);
                }
            }

            self.itemDisplayRuleSet.InvokeMethod("GenerateRuntimeValues");
        }
        #endregion

        #region Secure UserProfile
        // Making sure nothing custom items related get saved to the UserProfile so if the user decide to remove the custom items he doesn't get a corrupted profile
        // TODO: Somehow find a way to make all of this UserProfile securing cleaner. By having a custom serialized file only for custom items etc

        // Disable DiscoveredPickups field in UserProfile for custom items
        private static void SaveFieldAttributeOnSetupPickupsSet(On.RoR2.UserProfile.SaveFieldAttribute.orig_SetupPickupsSet orig, UserProfile.SaveFieldAttribute self, FieldInfo fieldInfo) {
            self.getter = delegate (UserProfile userProfile)
            {
                bool[] pickupsSet = (bool[])fieldInfo.GetValue(userProfile);
                var result = new StringBuilder("");
                for (int i = 0; i < OriginalItemCount + OriginalEquipmentCount + 1; i++) {
                    var pickupIndex = PickupIndex.allPickups.ToList()[i];

                    if (pickupsSet[pickupIndex.value] && !IsCustomItemOrEquipment(pickupIndex)) {
                        result.Append(pickupIndex.ToString());
                        result.Append(" ");
                    }
                }

                return result.ToString();
            };
            self.setter = delegate (UserProfile userProfile, string valueString)
            {
                bool[] array = (bool[])fieldInfo.GetValue(userProfile);
                Array.Clear(array, 0, 0);
                string[] array2 = valueString.Split(' ');
                foreach (var name in array2) {
                    PickupIndex pickupIndex = PickupIndex.Find(name);

                    if (pickupIndex.isValid && !IsCustomItemOrEquipment(pickupIndex)) {
                        array[pickupIndex.value] = true;
                    }
                }
            };
            self.copier = delegate (UserProfile srcProfile, UserProfile destProfile)
            {
                Array sourceArray = (bool[])fieldInfo.GetValue(srcProfile);
                bool[] array = (bool[])fieldInfo.GetValue(destProfile);
                Array.Copy(sourceArray, array, array.Length);
            };
        }

        private static void UserProfileOnDiscoverPickup(On.RoR2.UserProfile.orig_DiscoverPickup orig, UserProfile self, PickupIndex pickupIndex) {
            if (IsCustomItemOrEquipment(pickupIndex))
                return;

            orig(self, pickupIndex);
        }

        // Disable field making for totalCollected and highestCollected for custom items
        private static void PerItemStatDefOnRegisterStatDefs(On.RoR2.Stats.PerItemStatDef.orig_RegisterStatDefs orig) {
            var instancesList = typeof(PerItemStatDef).GetFieldValue<List<PerItemStatDef>>("instancesList");
            foreach (PerItemStatDef perItemStatDef in instancesList) {
                var prefix = perItemStatDef.GetFieldValue<string>("prefix");
                var recordType = perItemStatDef.GetFieldValue<StatRecordType>("recordType");
                var dataType = perItemStatDef.GetFieldValue<StatDataType>("dataType");
                var keyToStatDef = ItemCatalog.GetPerItemBuffer<StatDef>();
                perItemStatDef.SetFieldValue("keyToStatDef", keyToStatDef);

                foreach (ItemIndex itemIndex in ItemCatalog.allItems) {
                    if ((int) itemIndex >= OriginalItemCount)
                        continue;
                    ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                    StatDef statDef = StatDef.Register(prefix + "." + itemDef.name, recordType, dataType, 0.0);
                    keyToStatDef[(int)itemIndex] = statDef;
                }
            }
        }

        private static void PerEquipmentStatDefOnRegisterStatDefs(On.RoR2.Stats.PerEquipmentStatDef.orig_RegisterStatDefs orig) {
            var instancesList = typeof(PerEquipmentStatDef).GetFieldValue<List<PerEquipmentStatDef>>("instancesList");
            foreach (PerEquipmentStatDef perEquipmentStatDef in instancesList) {
                var prefix = perEquipmentStatDef.GetFieldValue<string>("prefix");
                var recordType = perEquipmentStatDef.GetFieldValue<StatRecordType>("recordType");
                var dataType = perEquipmentStatDef.GetFieldValue<StatDataType>("dataType");
                var keyToStatDef = EquipmentCatalog.GetPerEquipmentBuffer<StatDef>();
                perEquipmentStatDef.SetFieldValue("keyToStatDef", keyToStatDef);
                
                foreach (EquipmentIndex equipmentIndex in EquipmentCatalog.allEquipment) {
                    if ((int) equipmentIndex >= OriginalEquipmentCount)
                        continue;
                    StatDef statDef = StatDef.Register(prefix + "." + equipmentIndex, recordType, dataType, 0.0);
                    keyToStatDef[(int)equipmentIndex] = statDef;
                }
            }
        }

        // Jump to the end of the loop if EquipmentIndex >= OriginalEquipmentCount
        private static void StatManagerOnProcessCharacterUpdateEvents(ILContext il) {
            var cursor = new ILCursor(il);
            var currentEquipmentLoc = 0;

            cursor.GotoNext(
                i => i.MatchCallvirt<Inventory>("get_currentEquipmentIndex"),
                i => i.MatchStloc(out currentEquipmentLoc)
            );
            cursor.GotoNext(
                i => i.MatchLdloc(currentEquipmentLoc),
                i => i.MatchLdcI4(-1),
                i => i.MatchBeq(out _)
            );

            cursor.Index += 2;
            var label = cursor.Next.Operand;
            cursor.Index++;

            cursor.Emit(OpCodes.Ldloc, currentEquipmentLoc);
            cursor.Emit(OpCodes.Ldc_I4, OriginalEquipmentCount);
            cursor.Emit(OpCodes.Bge_S, label);
        }

        // Normally push values to the StatSheet about the item (totalCollected etc). It saves to UserProfile
        private static void StatManagerOnOnServerItemGiven(On.RoR2.Stats.StatManager.orig_OnServerItemGiven orig, Inventory inventory, ItemIndex itemIndex, int quantity) {
            if ((int) itemIndex < OriginalItemCount)
                orig(inventory, itemIndex, quantity);
        }
        
        private static void StatManagerOnOnEquipmentActivated(On.RoR2.Stats.StatManager.orig_OnEquipmentActivated orig, EquipmentSlot activator, EquipmentIndex equipmentIndex) {
            if ((int) equipmentIndex < OriginalEquipmentCount)
                orig(activator, equipmentIndex);
        }
        #endregion

        #region LogBook
        // LogBook. So for now we disable the progress part in the logbook for custom items, since logbook progression is linked to the data from the UserProfile,
        // the best solution would be to have a reserved data file for custom items somewhere so we never interact directly with the so fragile UserProfile of the users.
        // That way we could have a logbook working for custom items too
        // For now lets assume the user unlocked / discovered the item so we can see 3d models on it

        private static EntryStatus LogBookControllerOnGetPickupStatus(On.RoR2.UI.LogBook.LogBookController.orig_GetPickupStatus orig, UserProfile userProfile, Entry entry) {
            if (IsCustomItemOrEquipment((PickupIndex)entry.extraData)) {
                return EntryStatus.Available;
            }
            return orig(userProfile, entry);
        }

        private static void PageBuilderOnAddSimplePickup(On.RoR2.UI.LogBook.PageBuilder.orig_AddSimplePickup orig, PageBuilder self, PickupIndex pickupIndex) {
            if (IsCustomItemOrEquipment(pickupIndex)) {
                ItemIndex itemIndex = pickupIndex.itemIndex;
                EquipmentIndex equipmentIndex = pickupIndex.equipmentIndex;
                string token = null;
                if (itemIndex != ItemIndex.None) {
                    ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                    self.AddDescriptionPanel(Language.GetString(itemDef.descriptionToken));
                    token = itemDef.loreToken;
                    //ulong statValueULong = get from custom file;
                    //ulong statValueULong2 = get from custom file;
                    string stringFormatted = Language.GetStringFormatted("GENERIC_PREFIX_FOUND", "Unknown"); // param arg being the value from custom file
                    string stringFormatted2 = Language.GetStringFormatted("ITEM_PREFIX_STACKCOUNT", "Unknown");
                    self.AddSimpleTextPanel(stringFormatted, stringFormatted2);
                }
                else if (equipmentIndex != EquipmentIndex.None) {
                    EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                    self.AddDescriptionPanel(Language.GetString(equipmentDef.descriptionToken));
                    token = equipmentDef.loreToken;
                    // this.statSheet.GetStatDisplayValue(PerEquipmentStatDef.totalTimeHeld.FindStatDef(equipmentIndex)) get from custom file instead
                    // this.statSheet.GetStatDisplayValue(PerEquipmentStatDef.totalTimesFired.FindStatDef(equipmentIndex))
                    string stringFormatted3 = Language.GetStringFormatted("EQUIPMENT_PREFIX_TOTALTIMEHELD", "Unknown");
                    string stringFormatted4 = Language.GetStringFormatted("EQUIPMENT_PREFIX_USECOUNT", "Unknown");
                    self.AddSimpleTextPanel(stringFormatted3, stringFormatted4);
                }
                // ReSharper disable once AssignNullToNotNullAttribute
                self.AddNotesPanel(Language.IsTokenInvalid(token) ? Language.GetString("EARLY_ACCESS_LORE") : Language.GetString(token));
            }
            else {
                orig(self, pickupIndex);
            }
        }

        private static TooltipContent LogBookControllerOnGetPickupTooltipContent(On.RoR2.UI.LogBook.LogBookController.orig_GetPickupTooltipContent orig, UserProfile userProfile, Entry entry, EntryStatus status) {
            if (IsCustomItemOrEquipment((PickupIndex)entry.extraData)) {
                UnlockableDef unlockableDef = UnlockableCatalog.GetUnlockableDef(((PickupIndex)entry.extraData).GetUnlockableName());
                TooltipContent result = default;
                result.titleToken = entry.nameToken;
                result.titleColor = entry.color;
                if (unlockableDef != null) {
                    result.overrideBodyText = unlockableDef.getUnlockedString();
                }
                result.bodyToken = "LOGBOOK_CATEGORY_ITEM";
                result.bodyColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Unlockable);
                return result;
            }
            return orig(userProfile, entry, status);
        }
        #endregion

        public static bool IsCustomItemOrEquipment(PickupIndex pickupIndex) {
            var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            return (int) pickupDef.itemIndex >= OriginalItemCount || (int) pickupDef.equipmentIndex >= OriginalEquipmentCount;
        }
    }


    public class CustomItem {
        public ItemDef ItemDef;
        public ItemDisplayRule[] ItemDisplayRules;

        public CustomItem(ItemDef itemDef, ItemDisplayRule[] itemDisplayRules) {
            ItemDef = itemDef;
            ItemDisplayRules = itemDisplayRules;
        }
    }

    public class CustomEquipment {
        public EquipmentDef EquipmentDef;
        public ItemDisplayRule[] ItemDisplayRules;

        public CustomEquipment(EquipmentDef equipmentDef, ItemDisplayRule[] itemDisplayRules) {
            EquipmentDef = equipmentDef;
            ItemDisplayRules = itemDisplayRules;
        }
    }

    /// <summary>
    /// Class that defines a custom buff type for use in the game;
    /// you may omit the index in the BuffDef, as that will
    /// be assigned by ItemAPI.
    /// </summary>
    public class CustomBuff {
        /// <summary>
        /// Name of the Buff for the purposes of looking up its index
        /// </summary>
        public string Name;

        /// <summary>
        /// Definition of the Buff
        /// </summary>
        public BuffDef BuffDef;

        public CustomBuff(string name, BuffDef buffDef) {
            Name = name;
            BuffDef = buffDef;
        }
    }

    /// <summary>
    /// Class that defines a custom Elite type for use in the game.
    /// All Elites consistent of an Elite definition, a <see cref="CustomEquipment"/>
    /// and a <see cref="CustomBuff"/>.  The equipment is automatically provided to
    /// the Elite when it spawns and is configured to passively apply the buff.
    /// Note that if Elite Spawning Overhaul is enabled, you'll also want to create a EliteAffixCard/>
    /// to allow the combat director to spawn your elite type.
    /// </summary>
    public class CustomElite {
        /// <summary>
        /// Name of the Elite, for purposes of looking up its index
        /// </summary>
        public string Name;

        /// <summary>
        /// Elite definition (you can omit the index references, as those will be filled in automatically by ItemLib)
        /// </summary>
        public EliteDef EliteDef;

        /// <summary>
        /// Custom equipment that the Elite will carry; do note that this is something that may (rarely) drop from the Elite when killed,
        /// so players can also end up with this equipment
        /// </summary>
        public CustomEquipment Equipment;

        /// <summary>
        /// Custom buff that is applied passively by the equipment; note that this can be active on the player
        /// if they're using Wake of Vultures or pick up the equipment, so you'll need to decide what impact
        /// the elite buff should have on players.
        /// </summary>
        public CustomBuff Buff;

        /// <summary>
        /// Tier for the elite, where 1 is standard elites (Fire, Ice, Lightning) and 2 is currently just Poison (Malachite).
        /// If Elite Spawning Overhaul is disabled, it will use this tier to set cost/hp/dmg scaling.  Even if your mod is
        /// only intended to work with ESO enabled, this should still be set to a valid number 1-2 for compatibility with
        /// the underlying game code.
        /// </summary>
        public int Tier;

        public CustomElite(string name, EliteDef eliteDef, CustomEquipment equipment, CustomBuff buff, int tier = 1) {
            Name = name;
            EliteDef = eliteDef;
            Equipment = equipment;
            Buff = buff;
            Tier = tier;
        }
    }
}
