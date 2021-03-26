using MonoMod.Cil;
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
        /// The original buff count of the game.
        /// </summary>
        public static int OriginalBuffCount;

        /// <summary>
        /// Amount of custom Buffs added by R2API
        /// </summary>
        public static int CustomBuffCount;

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
            IL.RoR2.BuffCatalog.Init += GetOriginalBuffCountHook;

            BuffCatalog.modHelper.getAdditionalEntries += AddBuffAction;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            IL.RoR2.BuffCatalog.Init -= GetOriginalBuffCountHook;

            BuffCatalog.modHelper.getAdditionalEntries -= AddBuffAction;
        }

        private static void GetOriginalBuffCountHook(ILContext il) {
            var cursor = new ILCursor(il);

            cursor.GotoNext(
                i => i.MatchLdcI4(out OriginalBuffCount),
                i => i.MatchNewarr<BuffDef>()
            );
        }

        private static void AddBuffAction(List<BuffDef> buffDefinitions) {
            foreach (var customBuff in BuffDefinitions) {
                buffDefinitions.Add(customBuff.BuffDef);

                R2API.Logger.LogInfo($"Custom Buff: {customBuff.BuffDef.name} added");
            }

            _buffCatalogInitialized = true;
        }

        #endregion ModHelper Events and Hooks

        #region Add Methods

        /// <summary>
        /// Add a custom buff to the list of available buffs.
        /// Value for BuffDef.buffIndex can be ignored.
        /// If this is called after the BuffCatalog inits then this will return false and ignore the custom buff.
        /// </summary>
        /// <param name="buff">The buff to add.</param>
        /// <returns>the BuffIndex of your item if added. -1 otherwise</returns>
        public static BuffIndex Add(CustomBuff? buff) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(BuffAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(BuffAPI)})]");
            }

            if (_buffCatalogInitialized) {
                R2API.Logger.LogError(
                    $"Too late ! Tried to add buff: {buff.BuffDef.name} after the buff list was created");
                return BuffIndex.None;
            }

            buff.BuffDef.buffIndex = (BuffIndex)OriginalBuffCount + CustomBuffCount++;
            BuffDefinitions.Add(buff);
            return buff.BuffDef.buffIndex;
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
        public CustomBuff(string? name, string? iconPath, Color buffColor, bool isDebuff = false, bool canStack = false) {
            BuffDef = new BuffDef {
                name = name,
                iconPath = iconPath,
                buffColor = buffColor,
                isDebuff = isDebuff,
                canStack = canStack
            };
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
