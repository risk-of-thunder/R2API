﻿using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace R2API {

    /// <summary>
    /// API for adding custom buffs to the game. Previously included in ItemAPI.
    /// </summary>
    [R2APISubmodule]
    public static class BuffAPI {

        /// <summary>
        /// All custom buffs added by the API.
        /// </summary>
        public static ObservableCollection<CustomBuff?>? BuffDefinitions = new ObservableCollection<CustomBuff>();

        private static bool _buffCatalogInitialized;

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
            R2APIContentPackProvider.WhenContentPackReady += AddBuffsToGame;

            IL.RoR2.BuffCatalog.Init += FixWrongListRetrieved;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            R2APIContentPackProvider.WhenContentPackReady -= AddBuffsToGame;

            IL.RoR2.BuffCatalog.Init -= FixWrongListRetrieved;
        }

        private static void AddBuffsToGame(ContentPack r2apiContentPack) {
            var buffDefs = new List<BuffDef>();
            foreach (var customBuff in BuffDefinitions) {
                buffDefs.Add(customBuff.BuffDef);

                R2API.Logger.LogInfo($"Custom Buff: {customBuff.BuffDef.name} added");
            }

            r2apiContentPack.buffDefs = buffDefs.ToArray();
            _buffCatalogInitialized = true;
        }

        private static void FixWrongListRetrieved(ILContext il) {
            var c = new ILCursor(il);

            if (c.TryGotoNext(i => i.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.buffDefs)))) {
                c.Next.Operand = il.Import(typeof(ContentManager).GetField(nameof(ContentManager.buffDefs)));
            }
        }

        #endregion ModHelper Events and Hooks

        #region Add Methods

        /// <summary>
        /// Add a custom buff to the list of available buffs.
        /// Value for BuffDef.buffIndex can be ignored.
        /// We can't give you the buffIndex anymore in the method return param. Instead use BuffCatalog.FindBuffIndex (after catalog are init)
        /// If this is called after the BuffCatalog inits then this will return false and ignore the custom buff.
        /// </summary>
        /// <param name="buff">The buff to add.</param>
        /// <returns>true if added, false otherwise</returns>
        public static bool Add(CustomBuff? buff) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(BuffAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(BuffAPI)})]");
            }

            if (_buffCatalogInitialized) {
                R2API.Logger.LogError(
                    $"Too late ! Tried to add buff: {buff.BuffDef.name} after the buff list was created");
                return false;
            }

            BuffDefinitions.Add(buff);
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
