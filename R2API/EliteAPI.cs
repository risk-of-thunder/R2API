using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
            IL.RoR2.CombatDirector.Init += RetrieveVanillaEliteTierCount;
            On.RoR2.CombatDirector.Init += AddCustomEliteTiers;

            R2APIContentPackProvider.WhenContentPackReady += AddElitesToGame;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            IL.RoR2.CombatDirector.Init -= RetrieveVanillaEliteTierCount;
            On.RoR2.CombatDirector.Init -= AddCustomEliteTiers;

            R2APIContentPackProvider.WhenContentPackReady -= AddElitesToGame;
        }

        [R2APISubmoduleInit(Stage = InitStage.Load)]
        private static void Load() {
            LazyInitVanillaEliteTiers();
        }

        private static void AddCustomEliteTiers(On.RoR2.CombatDirector.orig_Init orig) {
            if (!_eliteTierCatalogInitialized) {
                orig();
                CombatDirector.eliteTiers = CombatDirector.eliteTiers.Concat(CustomEliteTierDefs).ToArray();
            }
        }

        private static void LazyInitVanillaEliteTiers() {
            CombatDirector.Init();
        }

        private static void AddElitesToGame(ContentPack r2apiContentPack) {
            var eliteDefs = new List<EliteDef>();

            LazyInitVanillaEliteTiers();

            foreach (var customElite in EliteDefinitions) {
                eliteDefs.Add(customElite.EliteDef);

                var currentEliteTiers = GetCombatDirectorEliteTiers();

                foreach (var eliteTierDef in customElite.EliteTierDefs) {
                    if (eliteTierDef == VanillaFirstTierDef) {
                        var eliteTypeIndex = VanillaFirstTierDef.eliteTypes.Length;
                        Array.Resize(ref VanillaFirstTierDef.eliteTypes, eliteTypeIndex + 1);
                        VanillaFirstTierDef.eliteTypes[eliteTypeIndex] = customElite.EliteDef;

                        // Copy the elite types to VanillaEliteOnlyFirstTierDef.
                        // VanillaEliteOnlyFirstTierDef stores the same elites as VanillaFirstTierDef
                        // elite-only artifact equivalent
                        VanillaEliteOnlyFirstTierDef.eliteTypes = VanillaFirstTierDef.eliteTypes;
                    }
                    else {
                        var eliteTypeIndex = eliteTierDef.eliteTypes.Length;
                        Array.Resize(ref eliteTierDef.eliteTypes, eliteTypeIndex + 1);
                        eliteTierDef.eliteTypes[eliteTypeIndex] = customElite.EliteDef;
                    }
                }

                OverrideCombatDirectorEliteTiers(currentEliteTiers);

                R2API.Logger.LogInfo($"Custom Elite: {customElite.EliteDef.modifierToken} added");
            }

            r2apiContentPack.eliteDefs.Add(eliteDefs.ToArray());
            _eliteCatalogInitialized = true;
        }

        private static void RetrieveVanillaEliteTierCount(ILContext il) {
            var c = new ILCursor(il);
            if (c.TryGotoNext(
                i => i.MatchLdcI4(out VanillaEliteTierCount),
                i => i.MatchNewarr<CombatDirector.EliteTierDef>())) {
            }
            else {
                R2API.Logger.LogError("Failed finding IL Instructions. Aborting RetrieveVanillaEliteTierCount IL Hook");
            }
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

            if (elite.EliteTierDefs == null || elite.EliteTierDefs.Count() <= 0) {
                throw new ArgumentNullException("CustomElite.EliteTierDefs");

            }

            EliteDefinitions.Add(elite);
            return true;
        }

        #endregion Add Methods

        #region Combat Director Modifications

        private static CombatDirector.EliteTierDef[] RetrieveVanillaEliteTiers() {
            LazyInitVanillaEliteTiers();

            return CombatDirector.eliteTiers;
        }

        private static CombatDirector.EliteTierDef RetrieveFirstVanillaTierDef() {
            LazyInitVanillaEliteTiers();

            return CombatDirector.eliteTiers[1];
        }

        private static CombatDirector.EliteTierDef RetrieveVanillaEliteOnlyFirstTierDef() {
            LazyInitVanillaEliteTiers();

            return CombatDirector.eliteTiers[2];
        }

        public static CombatDirector.EliteTierDef[] VanillaEliteTiers { get; private set; } = RetrieveVanillaEliteTiers();
        public static CombatDirector.EliteTierDef VanillaFirstTierDef { get; private set; } = RetrieveFirstVanillaTierDef();
        public static CombatDirector.EliteTierDef VanillaEliteOnlyFirstTierDef { get; private set; } = RetrieveVanillaEliteOnlyFirstTierDef();

        /// <summary>
        /// Returns the current elite tier definitions used by the Combat Director for doing its elite spawning while doing a run.
        /// </summary>
        public static CombatDirector.EliteTierDef?[]? GetCombatDirectorEliteTiers() => CombatDirector.eliteTiers;
        private static bool _eliteTierCatalogInitialized => CombatDirector.eliteTiers != null && CombatDirector.eliteTiers.Length > 0;

        public static int VanillaEliteTierCount;

        private static readonly List<CombatDirector.EliteTierDef> CustomEliteTierDefs = new List<CombatDirector.EliteTierDef>();
        public static int CustomEliteTierCount => CustomEliteTierDefs.Count;

        /// <summary>
        /// The EliteTierDef array is used by the Combat Director for doing its elite spawning while doing a run.
        /// You can get the current array used by the director with EliteAPI.GetCombatDirectorEliteTiers()
        /// </summary>
        /// <param name="newEliteTiers">The new elite tiers that will be used by the combat director.</param>
        public static void OverrideCombatDirectorEliteTiers(CombatDirector.EliteTierDef?[]? newEliteTiers) {
            CombatDirector.eliteTiers = newEliteTiers;
        }

        /// <summary>
        /// Allows for adding a new elite tier def to the combat director.
        /// When adding a new elite tier,
        /// do not fill the eliteTypes field with your custom elites defs if your goal is to add your custom elite in it right after.
        /// Because when doing EliteAPI.Add, the API will add your elite to the specified tiers <see cref="CustomElite.EliteTierDefs"/>.
        /// </summary>
        /// <param name="eliteTierDef">The new elite tier to add.</param>
        public static int AppendCustomEliteTier(CombatDirector.EliteTierDef? eliteTierDef) {
            return AddCustomEliteTier(eliteTierDef, -1);
        }

        /// <summary>
        /// Allows for adding a new elite tier def to the combat director.
        /// Automatically insert the eliteTierDef at the correct position in the array based on its <see cref="CombatDirector.EliteTierDef.costMultiplier"/>
        /// When adding a new elite tier, do not fill the eliteTypes field with your custom elites defs if your goal is to add your custom elite in it right after.
        /// Because when doing EliteAPI.Add, the API will add your elite to the specified tiers <see cref="CustomElite.EliteTierDefs"/>.
        /// </summary>
        /// <param name="eliteTierDef">The new elite tier to add.</param>
        public static int AddCustomEliteTier(CombatDirector.EliteTierDef? eliteTierDef) {
            var indexToInsertAt = Array.FindIndex(CombatDirector.eliteTiers, x => x.costMultiplier >= eliteTierDef.costMultiplier);
            if (indexToInsertAt >= 0) {
                return AddCustomEliteTier(eliteTierDef, indexToInsertAt);
            }
            else {
                return AppendCustomEliteTier(eliteTierDef);
            }
        }

        // todo : maybe sort the CombatDirector.eliteTiers array based on cost ? the game code isnt the cleanest about this
        /// <summary>
        /// Allows for adding a new elite tier def to the combat director.
        /// When adding a new elite tier, do not fill the eliteTypes field with your custom elites defs if your goal is to add your custom elite in it right after.
        /// Because when doing EliteAPI.Add, the API will add your elite to the specified tiers <see cref="CustomElite.EliteTierDefs"/>.
        /// </summary>
        /// <param name="eliteTierDef">The new elite tier to add.</param>
        /// <param name="indexToInsertAt">Optional index to specify if you want to insert a cheaper elite tier</param>
        public static int AddCustomEliteTier(CombatDirector.EliteTierDef? eliteTierDef, int indexToInsertAt = -1) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(EliteAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(EliteAPI)})]");
            }

            var eliteTiersSize = VanillaEliteTierCount + CustomEliteTierCount;

            var currentEliteTiers = GetCombatDirectorEliteTiers();
            if (currentEliteTiers != null) {
                Array.Resize(ref currentEliteTiers, eliteTiersSize + 1);

                if (indexToInsertAt == -1) {
                    indexToInsertAt = eliteTiersSize;
                }
                else {
                    // Shift right starting from indexToInsertAt
                    Array.Copy(currentEliteTiers, indexToInsertAt, currentEliteTiers, indexToInsertAt + 1, currentEliteTiers.Length - indexToInsertAt - 1);
                }
                currentEliteTiers[indexToInsertAt] = eliteTierDef;
                
                OverrideCombatDirectorEliteTiers(currentEliteTiers);
            }

            CustomEliteTierDefs.Add(eliteTierDef);

            R2API.Logger.LogInfo($"Custom Elite Tier : (Index : {indexToInsertAt}) added");

            return indexToInsertAt;
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
        /// Elite tier(s) that the eliteDef will be on.
        /// </summary>
        public IEnumerable<CombatDirector.EliteTierDef> EliteTierDefs;

        /// <summary>
        /// You can omit the index references for the EliteDef, as those will be filled in automatically by the API.
        /// You can retrieve a vanilla EquipmentDef through RoR2Content.Equipments class
        /// Also, don't forget to give it a valid eliteTier so that your custom elite correctly get spawned.
        /// You can also make a totally new tier, by using OverrideCombatDirectorEliteTiers for example.
        /// </summary>
        public CustomElite(string? name, EquipmentDef equipmentDef, Color32 color, string? modifierToken, IEnumerable<CombatDirector.EliteTierDef> eliteTierDefs) {
            EliteDef = ScriptableObject.CreateInstance<EliteDef>();
            EliteDef.name = name;
            EliteDef.eliteEquipmentDef = equipmentDef;
            EliteDef.color = color;
            EliteDef.modifierToken = modifierToken;
            EliteTierDefs = eliteTierDefs;
        }

        /// <summary>
        /// You can omit the index references for the EliteDef, as those will be filled in automatically by the API.
        /// But don't forget to give it a valid eliteTier so that your custom elite correctly get spawned.
        /// You can also make a totally new tier, by using OverrideCombatDirectorEliteTiers for example.
        /// </summary>
        public CustomElite(EliteDef? eliteDef, IEnumerable<CombatDirector.EliteTierDef> eliteTierDefs) {
            EliteDef = eliteDef;
            EliteTierDefs = eliteTierDefs;
        }
    }
}
