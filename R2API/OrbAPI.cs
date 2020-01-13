using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using R2API.Utils;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class OrbAPI {
        #region Loaded check
        //Maybe best to set up a base class or interface that does this automatically?
        public static bool Loaded {
            get {
                return IsLoaded;
            }
        }
        private static bool IsLoaded = false;
        #endregion


        private static bool orbsAlreadyAdded = false;

        public static ObservableCollection<Type> OrbDefinitions = new ObservableCollection<Type>();

        /// <summary>
        /// Adds an Orb to the orb catalog.
        /// This must be called during plugin Awake() or OnEnable().
        /// The type must be a subclass of RoR2.Orbs.Orb
        /// </summary>
        /// <param name="t">The type of the orb being added</param>
        /// <returns>True if orb will be added</returns>
        public static bool AddOrb(Type t) {
            if( !IsLoaded ) {
                R2API.Logger.LogError( "OrbAPI is not loaded. Please use [R2API.Utils.SubModuleDependency]" );
                return false;
            }
            if (orbsAlreadyAdded) {
                R2API.Logger.LogError($"Tried to add Orb type: {nameof(t)} after orb catalog was generated");
                return false;
            }

            if (t == null || !t.IsSubclassOf( typeof( RoR2.Orbs.Orb ) ) ) {
                R2API.Logger.LogError($"Type: {nameof(t)} is null or not a subclass of RoR2.Orbs.Orb");
                return false;
            }

            OrbDefinitions.Add(t);

            return true;
        }

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.Orbs.OrbCatalog.GenerateCatalog += AddOrbs;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.Orbs.OrbCatalog.GenerateCatalog -= AddOrbs;
        }

        private static void AddOrbs( On.RoR2.Orbs.OrbCatalog.orig_GenerateCatalog orig ) {
            orbsAlreadyAdded = true;
            orig();

            Type[] orbCat = typeof(RoR2.Orbs.OrbCatalog).GetFieldValue<Type[]>("indexToType");
            Dictionary<Type, int> typeToIndex = typeof(RoR2.Orbs.OrbCatalog).GetFieldValue<Dictionary<Type, int>>("typeToIndex");

            int origLength = orbCat.Length;
            int extraLength = OrbDefinitions.Count;

            Array.Resize<Type>( ref orbCat, origLength + extraLength );

            int temp;

            for(int i = 0; i < extraLength; i++ ) {
                temp = i + origLength;
                orbCat[temp] = OrbDefinitions[i];
                typeToIndex.Add( OrbDefinitions[i], temp );
            }

            typeof( RoR2.Orbs.OrbCatalog ).SetFieldValue<Type[]>( "indexToType", orbCat );
            typeof( RoR2.Orbs.OrbCatalog ).SetFieldValue<Dictionary<Type, Int32>>( "typeToIndex", typeToIndex );
        }
    }
}
