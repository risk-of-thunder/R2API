﻿using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using UnityEngine;

namespace R2API {

    /// <summary>
    /// API for adding custom buffs to the game. Previously included in ItemAPI.
    /// </summary>
    [R2APISubmodule]
    [Obsolete($"The {nameof(BuffAPI)} is obsolete, please add your BuffDefs via R2API.ContentManagement.ContentAdditionHelpers.AddBuffDef()")]
    public static class BuffAPI {

        /// <summary>
        /// All custom buffs added by the API.
        /// </summary>
        [Obsolete($"This observable collection is obsolete, if you want to look at the buffDefs added by R2API, look at R2API.ContentManagement.R2APIContentManager.ManagedContentPacks and do a SelectMany on the buffDefs.")]
        public static ObservableCollection<CustomBuff?>? BuffDefinitions = new ObservableCollection<CustomBuff>();

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        #region Add Methods

        /// <summary>
        /// Add a custom buff to the list of available buffs.
        /// Value for BuffDef.buffIndex can be ignored.
        /// We can't give you the buffIndex anymore in the method return param. Instead use BuffCatalog.FindBuffIndex (after catalog are init)
        /// If this is called after the BuffCatalog inits then this will return false and ignore the custom buff.
        /// </summary>
        /// <param name="buff">The buff to add.</param>
        /// <returns>true if added, false otherwise</returns>
        [Obsolete($"Add is obsolete, please add your BuffDefs via R2API.ContentManagement.ContentAdditionHelpers.AddBuffDef()")]
        public static bool Add(CustomBuff? buff) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(BuffAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(BuffAPI)})]");
            }

            if (!CatalogBlockers.GetAvailability<BuffDef>()) {
                R2API.Logger.LogError(
                    $"Too late ! Tried to add buff: {buff.BuffDef.name} after the BuffCatalog has initialized!");
                return false;
            }

            R2APIContentManager.HandleContentAddition(Assembly.GetCallingAssembly(), buff.BuffDef);
            return true;
        }

        #endregion Add Methods
    }

    /// <summary>
    /// Class that defines a custom buff type for use in the game;
    /// you may omit the buffIndex in the BuffDef, as that will
    /// be assigned by the API.
    /// If you are doing a buff for a custom elite, don't forget to register your CustomElite before too to fill the eliteIndex field !
    /// </summary>
    public class CustomBuff {

        /// <summary>
        /// Definition of the Buff
        /// </summary>
        public BuffDef? BuffDef;

        /// <summary>
        /// Create a custom buff to add into the game.
        /// If you are doing a buff for a custom elite, don't forget to register your CustomElite before too to fill the eliteIndex field !
        /// </summary>
        public CustomBuff(string? name, Sprite iconSprite, Color buffColor, bool isDebuff = false, bool canStack = false) {
            BuffDef = ScriptableObject.CreateInstance<BuffDef>();
            BuffDef.name = name;
            BuffDef.iconSprite = iconSprite;
            BuffDef.buffColor = buffColor;
            BuffDef.isDebuff = isDebuff;
            BuffDef.canStack = canStack;
        }

        /// <summary>
        /// Create a custom buff to add into the game.
        /// you may omit the buffIndex in the BuffDef, as that will be assigned by the API.
        /// If you are doing a buff for a custom elite, don't forget to register your CustomElite before too to fill the eliteIndex field !
        /// </summary>
        public CustomBuff(BuffDef? buffDef) {
            BuffDef = buffDef;
        }
    }
}
