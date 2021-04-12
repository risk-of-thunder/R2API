using R2API.Utils;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace R2API {

    [R2APISubmodule]
    // ReSharper disable once InconsistentNaming
    public static class EliteAPI {
        public static ObservableCollection<CustomElite?>? EliteDefinitions = new ObservableCollection<CustomElite?>();

        private static bool _eliteCatalogInitialized;

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
            R2APIContentPackProvider.WhenContentPackReady += AddElitesToGame;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            R2APIContentPackProvider.WhenContentPackReady -= AddElitesToGame;
        }

        private static void AddElitesToGame(ContentPack r2apiContentPack) {
            var eliteDefs = new List<EliteDef>();

            foreach (var customElite in EliteDefinitions) {
                eliteDefs.Add(customElite.EliteDef);
                var currentEliteTiers = GetCombatDirectorEliteTiers();
                if (customElite.EliteTier == 1) {
                    var index = currentEliteTiers[1].eliteTypes.Length;
                    Array.Resize(ref currentEliteTiers[1].eliteTypes, index + 1);
                    currentEliteTiers[1].eliteTypes[index] = customElite.EliteDef;
                    currentEliteTiers[2].eliteTypes = currentEliteTiers[1].eliteTypes;
                }
                else {
                    var eliteTierIndex = customElite.EliteTier + 1;
                    var eliteTypeIndex = currentEliteTiers[eliteTierIndex].eliteTypes.Length;
                    Array.Resize(ref currentEliteTiers[eliteTierIndex].eliteTypes, eliteTypeIndex + 1);
                    currentEliteTiers[eliteTierIndex].eliteTypes[eliteTypeIndex] = customElite.EliteDef;
                }
                OverrideCombatDirectorEliteTiers(currentEliteTiers);

                R2API.Logger.LogInfo($"Custom Elite: {customElite.EliteDef.modifierToken} (elite tier: {customElite.EliteTier}) added");
            }

            r2apiContentPack.eliteDefs.Add(eliteDefs.ToArray());
            _eliteCatalogInitialized = true;
        }

        #endregion ModHelper Events and Hooks

        #region Add Methods

        /// <summary>
        /// Add a custom elite to the list of available elites.
        /// Value for EliteDef.eliteIndex can be ignored.
        /// We can't give you the EliteIndex anymore in the method return param.
        /// If this is called after the ItemCatalog inits then this will ignore the custom elite.
        /// </summary>
        /// <param name="elite">The elite to add.</param>
        /// <returns>true if added, false otherwise</returns>
        public static bool Add(CustomElite? elite) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(EliteAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(EliteAPI)})]");
            }

            if (_eliteCatalogInitialized) {
                R2API.Logger.LogError($"Too late ! Tried to add elite: {elite.EliteDef.modifierToken} after the elite list was created");
                return false;
            }

            var numberOfEliteTiersDefined = GetCombatDirectorEliteTiers().Length - 2;
            if (elite.EliteTier <= 0 && elite.EliteTier > numberOfEliteTiersDefined) {
                R2API.Logger.LogError(
                    "Incorrect Elite Tier, must be valid: greater than 0 and "
                    + $"within the current elite tier defs range, current number of elite tiers defined : {numberOfEliteTiersDefined}.");
                return false;
            }

            EliteDefinitions.Add(elite);
            return true;
        }

        #endregion Add Methods

        #region Combat Director Modifications

        /// <summary>
        /// Returns the current elite tier definitions used by the Combat Director for doing its elite spawning while doing a run.
        /// </summary>
        public static CombatDirector.EliteTierDef?[]? GetCombatDirectorEliteTiers() {
            return typeof(CombatDirector).GetFieldValue<CombatDirector.EliteTierDef[]>("eliteTiers");
        }

        /// <summary>
        /// The EliteTierDef array is used by the Combat Director for doing its elite spawning while doing a run.
        /// You can get the current array used by the director with EliteAPI.GetCombatDirectorEliteTiers()
        /// </summary>
        /// <param name="newEliteTiers">The new elite tiers that will be used by the combat director.</param>
        public static void OverrideCombatDirectorEliteTiers(CombatDirector.EliteTierDef?[]? newEliteTiers) {
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
        public static int AddCustomEliteTier(CombatDirector.EliteTierDef? eliteTierDef) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(EliteAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(EliteAPI)})]");
            }
            var currentEliteTiers = GetCombatDirectorEliteTiers();
            var index = currentEliteTiers.Length;
            Array.Resize(ref currentEliteTiers, index + 1);
            currentEliteTiers[index] = eliteTierDef;
            OverrideCombatDirectorEliteTiers(currentEliteTiers);

            return index - 1;
        }

        #endregion Combat Director Modifications
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
        public EliteDef? EliteDef;

        /// <summary>
        /// Elite tier, vanilla game currently have two different tiers :
        /// 1 for fire, lightning, and ice, and tier 2, for poison and haunted.
        /// </summary>
        public int EliteTier;

        /// <summary>
        /// You can omit the index references for the EliteDef, as those will be filled in automatically by the API.
        /// You can retrieve a vanilla EquipmentDef through RoR2Content.Equipments class
        /// Also, don't forget to give it a valid eliteTier so that your custom elite correctly get spawned.
        /// You can also make a totally new tier, by using OverrideCombatDirectorEliteTiers for example.
        /// </summary>
        public CustomElite(string? name, EquipmentDef equipmentDef, Color32 color, string? modifierToken, int eliteTier) {
            EliteDef = ScriptableObject.CreateInstance<EliteDef>();
            EliteDef.name = name;
            EliteDef.eliteEquipmentDef = equipmentDef;
            EliteDef.color = color;
            EliteDef.modifierToken = modifierToken;
            EliteTier = eliteTier;
        }

        /// <summary>
        /// You can omit the index references for the EliteDef, as those will be filled in automatically by the API.
        /// But don't forget to give it a valid eliteTier so that your custom elite correctly get spawned.
        /// You can also make a totally new tier, by using OverrideCombatDirectorEliteTiers for example.
        /// </summary>
        public CustomElite(EliteDef? eliteDef, int eliteTier) {
            EliteDef = eliteDef;
            EliteTier = eliteTier;
        }
    }
}
