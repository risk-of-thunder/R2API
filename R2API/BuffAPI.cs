using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using UnityEngine;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable 618 // PickupIndex being obsolete (but still being used in the game code)

namespace R2API {
    [R2APISubmodule]
    // ReSharper disable once InconsistentNaming
    public static class BuffAPI {
        public static ObservableCollection<CustomBuff> BuffDefinitions = new ObservableCollection<CustomBuff>();

        private static bool _buffCatalogInitialized;

        public static int OriginalBuffCount;
        public static int CustomBuffCount;

        public static bool Loaded {
            get => _loaded;
            set => _loaded = value;
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
        #endregion

        #region Add Methods

        /// <summary>
        /// Add a custom buff to the list of available buffs.
        /// Value for BuffDef.buffIndex can be ignored.
        /// If this is called after the BuffCatalog inits then this will return false and ignore the custom buff.
        /// </summary>
        /// <param name="buff">The buff to add.</param>
        /// <returns>the BuffIndex of your item if added. -1 otherwise</returns>
        public static BuffIndex Add(CustomBuff buff) {
            if (!Loaded) {
                R2API.Logger.LogError("BuffAPI is not loaded. Please use [R2APISubmoduleDependency(nameof(BuffAPI)]");
                return BuffIndex.None;
            }

            if (_buffCatalogInitialized) {
                R2API.Logger.LogError(
                    $"Too late ! Tried to add buff: {buff.BuffDef.name} after the buff list was created");
                return BuffIndex.None;
            }

            buff.BuffDef.buffIndex = (BuffIndex) OriginalBuffCount + CustomBuffCount++;
            BuffDefinitions.Add(buff);
            return buff.BuffDef.buffIndex;
        }

        #endregion
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
        public BuffDef BuffDef;

        /// <summary>
        /// you may omit the buffIndex in the BuffDef, as that will
        /// be assigned by the API.
        /// If you are doing a buff for a custom elite, don't forget to register your CustomElite before too to fill the eliteIndex field !
        /// </summary>
        public CustomBuff(string name, string iconPath, Color buffColor, bool isDebuff = false, bool canStack = false) {
            BuffDef = new BuffDef {
                name = name,
                iconPath = iconPath,
                buffColor = buffColor,
                isDebuff = isDebuff,
                canStack = canStack
            };
        }

        /// <summary>
        /// you may omit the buffIndex in the BuffDef, as that will
        /// be assigned by the API.
        /// If you are doing a buff for a custom elite, don't forget to register your CustomElite before too to fill the eliteIndex field !
        /// </summary>
        public CustomBuff(BuffDef buffDef) {
            BuffDef = buffDef;
        }

        [Obsolete("Use the constructor that allows you to input the fields of an BuffDef or use the one that take an BuffDef as parameter directly.")]
        public CustomBuff(string name, BuffDef buffDef) {
            BuffDef = buffDef;
        }
    }
}
