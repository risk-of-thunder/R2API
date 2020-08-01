﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EntityStates.AI.Walker;
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

                var currentEliteTiers = GetCombatDirectorEliteTiers();
                if (customElite.EliteTier == 1) {
                    var index = currentEliteTiers[1].eliteTypes.Length;
                    Array.Resize(ref currentEliteTiers[1].eliteTypes, index + 1);
                    currentEliteTiers[1].eliteTypes[index] = customElite.EliteDef.eliteIndex;
                    currentEliteTiers[2].eliteTypes = currentEliteTiers[1].eliteTypes;
                }
                else {
                    var eliteTierIndex = customElite.EliteTier + 1;
                    var eliteTypeIndex = currentEliteTiers[eliteTierIndex].eliteTypes.Length;
                    if (currentEliteTiers[eliteTierIndex].eliteTypes == null)
                        currentEliteTiers[eliteTierIndex].eliteTypes = new EliteIndex[0];
                    Array.Resize(ref currentEliteTiers[eliteTierIndex].eliteTypes, eliteTypeIndex + 1);
                    currentEliteTiers[1].eliteTypes[eliteTypeIndex] = customElite.EliteDef.eliteIndex;
                }
                OverrideCombatDirectorEliteTiers(currentEliteTiers);

                R2API.Logger.LogInfo($"Custom Elite: {customElite.EliteDef.modifierToken} (elite tier: {customElite.EliteTier}) added");
            }

            _eliteCatalogInitialized = true;
        }
        #endregion

        #region Add Methods
        /// <summary>
        /// Add a custom elite to the list of available elites.
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

            if (elite.EliteTier <= 0) {
                R2API.Logger.LogError("Incorrect Elite Tier, must be greater than 0.");
                return EliteIndex.None;
            }

            elite.EliteDef.eliteIndex = (EliteIndex)OriginalEliteCount + CustomEliteCount++;
            EliteDefinitions.Add(elite);
            return elite.EliteDef.eliteIndex;
        }
        #endregion

        #region Combat Director Modifications
        /// <summary>
        /// Returns the current elite tier definitions used by the Combat Director for doing its elite spawning while doing a run.
        /// </summary>
        public static CombatDirector.EliteTierDef[] GetCombatDirectorEliteTiers() {
            return typeof(CombatDirector).GetFieldValue<CombatDirector.EliteTierDef[]>("eliteTiers");
        }

        /// <summary>
        /// The EliteTierDef array is used by the Combat Director for doing its elite spawning while doing a run.
        /// You can get the current array used by the director with EliteAPI.GetCombatDirectorEliteTiers()
        /// </summary>
        /// <param name="newEliteTiers">The new elite tiers that will be used by the combat director.</param>
        public static void OverrideCombatDirectorEliteTiers(CombatDirector.EliteTierDef[] newEliteTiers) {
            typeof(CombatDirector).SetFieldValue("eliteTiers", newEliteTiers);
        }

        /// <summary>
        /// Allows for easily adding a new elite tier def to the combat director.
        /// When adding a new elite tier,
        /// do not fill the eliteTypes field if your goal is to add your custom elite in it right after.
        /// Because when doing EliteAPI.Add, the API will add your elite to the specified tier automaticly.
        /// Returns the elite tier (normally starts at 3, 2 vanilla tiers atm)
        /// </summary>
        /// <param name="eliteTierDef">The new elite tier to add.</param>
        public static int AddCustomEliteTier(CombatDirector.EliteTierDef eliteTierDef) {
            var currentEliteTiers = GetCombatDirectorEliteTiers();
            var index = currentEliteTiers.Length;
            Array.Resize(ref currentEliteTiers, index + 1);
            currentEliteTiers[index] = eliteTierDef;
            OverrideCombatDirectorEliteTiers(currentEliteTiers);

            return index - 1;
        }
        #endregion
    }

    /// <summary>
    /// Class that defines a custom Elite type for use in the game
    /// All Elites consistent of an Elite definition, a <see cref="CustomEquipment"/>
    /// and a <see cref="CustomBuff"/>. The equipment is automatically provided to
    /// the Elite when it spawns and is configured to passively apply the buff.
    /// </summary>
    public class CustomElite {
        /// <summary>
        /// Elite definition
        /// </summary>
        public EliteDef EliteDef;

        /// <summary>
        /// Elite tier, vanilla game currently have two different tiers :
        /// 1 for fire, lightning, and ice, and tier 2, for poison and haunted.
        /// </summary>
        public int EliteTier;

        /// <summary>
        /// You can omit the index references for the EliteDef, as those will be filled in automatically by the API.
        /// If you are doing an equipment for a custom elite, don't forget to register your CustomEquipment before too to fill the equipmentIndex field !
        /// Also, don't forget to give it a valid eliteTier so that your custom elite correctly get spawned.
        /// You can also make a totally new tier, by using OverrideCombatDirectorEliteTiers for example.
        /// </summary>
        public CustomElite(string name, EquipmentIndex equipmentIndex, Color32 color, string modifierToken, int eliteTier) {
            EliteDef = new EliteDef
            {
                name = name,
                eliteEquipmentIndex = equipmentIndex,
                color = color,
                modifierToken = modifierToken
            };

            EliteTier = eliteTier;
        }

        /// <summary>
        /// You can omit the index references for the EliteDef, as those will be filled in automatically by the API.
        /// But don't forget to give it a valid eliteTier so that your custom elite correctly get spawned.
        /// You can also make a totally new tier, by using OverrideCombatDirectorEliteTiers for example.
        /// </summary>
        public CustomElite(EliteDef eliteDef, int eliteTier) {
            EliteDef = eliteDef;
            EliteTier = eliteTier;
        }

        [Obsolete("Use the constructor that allows you to input the fields of an EliteDef or use the one that take an EliteDef as parameter directly.")]
        public CustomElite(string nameUnusedObsolete, EliteDef eliteDef, CustomEquipment equipmentUnusedObsolete, CustomBuff buffUnusedObsolete, int tierUnusedObsolete = 1) {
            EliteDef = eliteDef;
        }
    }
}
