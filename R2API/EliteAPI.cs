using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public static class EliteAPI {
        public static ObservableCollection<CustomElite> EliteDefinitions = new ObservableCollection<CustomElite>();
        
        private static bool _eliteCatalogInitialized;

        public static int OriginalEliteCount;
        public static int CustomEliteCount;

        public static bool Loaded {
            get => _loaded;
            set => _loaded = value;
        }

        private static bool _loaded;

        #region ModHelper Events and Hooks
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            IL.RoR2.EliteCatalog.Init += GetOriginalEliteCountHook;

            EliteCatalog.modHelper.getAdditionalEntries += AddEliteAction;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            IL.RoR2.EliteCatalog.Init -= GetOriginalEliteCountHook;

            EliteCatalog.modHelper.getAdditionalEntries -= AddEliteAction;
        }

        private static void GetOriginalEliteCountHook(ILContext il) {
            var cursor = new ILCursor(il);

            cursor.GotoNext(
                i => i.MatchLdcI4(out OriginalEliteCount),
                i => i.MatchNewarr<EliteDef>()
           );
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
        /// Add a custom item to the list of available elites.
        /// Value for EliteDef.eliteIndex can be ignored.
        /// If this is called after the ItemCatalog inits then this will return false and ignore the custom elite.
        /// </summary>
        /// <param name="elite">The elite to add.</param>
        /// <returns>the EliteIndex of your item if added. -1 otherwise</returns>
        public static EliteIndex Add(CustomElite elite) {
            if (!Loaded) {
                R2API.Logger.LogError("EliteAPI is not loaded. Please use [R2APISubmoduleDependency(nameof(EliteAPI)]");
                return EliteIndex.None;
            }

            if (_eliteCatalogInitialized) {
                R2API.Logger.LogError($"Too late ! Tried to add elite: {elite.EliteDef.modifierToken} after the elite list was created");
                return EliteIndex.None;
            }

            elite.EliteDef.eliteIndex = (EliteIndex)OriginalEliteCount + CustomEliteCount++;
            EliteDefinitions.Add(elite);
            return elite.EliteDef.eliteIndex;
        }
        #endregion
    }

    /// <summary>
    /// Class that defines a custom Elite type for use in the game
    /// All Elites consistent of an Elite definition, a <see cref="CustomEquipment"/>
    /// and a <see cref="CustomBuff"/>. The equipment is automatically provided to
    /// the Elite when it spawns and is configured to passively apply the buff
    /// Note that if Elite Spawning Overhaul is enabled, you'll also want to create a EliteAffixCard/
    /// to allow the combat director to spawn your elite type
    /// </summary>

    public class CustomElite {
        /// <summary>
        /// Elite definition
        /// </summary>
        public EliteDef EliteDef;

        /// <summary>
        /// You can omit the index references for the EliteDef, as those will be filled in automatically by the API.
        /// If you are doing an equipment for a custom elite, don't forget to register your CustomEquipment before too to fill the equipmentIndex field !
        /// </summary>
        public CustomElite(string name, EquipmentIndex equipmentIndex, Color32 color, string modifierToken) {
            EliteDef = new EliteDef
            {
                name = name,
                eliteEquipmentIndex = equipmentIndex,
                color = color,
                modifierToken = modifierToken
            };
        }

        /// <summary>
        /// You can omit the index references for the EliteDef, as those will be filled in automatically by the API.
        /// </summary>
        public CustomElite(EliteDef eliteDef) {
            EliteDef = eliteDef;
        }

        [Obsolete("Use the constructor that allows you to input the fields of an EliteDef or use the one that take an EliteDef as parameter directly.")]
        public CustomElite(string nameUnusedObsolete, EliteDef eliteDef, CustomEquipment equipmentUnusedObsolete, CustomBuff buffUnusedObsolete, int tierUnusedObsolete = 1) {
            EliteDef = eliteDef;
        }
    }
}
