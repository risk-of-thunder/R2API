using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using R2API.Utils;
// ReSharper disable UnusedMember.Global

namespace R2API {
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class OrbAPI {
        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;


        private static bool _orbsAlreadyAdded = false;

        public static ObservableCollection<Type> OrbDefinitions = new ObservableCollection<Type>();

        /// <summary>
        /// Adds an Orb to the orb catalog.
        /// This must be called during plugin Awake() or OnEnable().
        /// The type must be a subclass of RoR2.Orbs.Orb
        /// </summary>
        /// <param name="t">The type of the orb being added</param>
        /// <returns>True if orb will be added</returns>
        public static bool AddOrb(Type t) {
            if(!Loaded) {
                throw new InvalidOperationException($"{nameof(OrbAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(OrbAPI)})]");
            }
            if (_orbsAlreadyAdded) {
                R2API.Logger.LogError($"Tried to add Orb type: {nameof(t)} after orb catalog was generated");
                return false;
            }

            if (t == null || !t.IsSubclassOf(typeof(RoR2.Orbs.Orb))) {
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

        private static void AddOrbs(On.RoR2.Orbs.OrbCatalog.orig_GenerateCatalog orig) {
            _orbsAlreadyAdded = true;
            orig();

            var orbCat = typeof(RoR2.Orbs.OrbCatalog).GetFieldValue<Type[]>("indexToType");
            var typeToIndex = typeof(RoR2.Orbs.OrbCatalog).GetFieldValue<Dictionary<Type, int>>("typeToIndex");

            int origLength = orbCat.Length;
            int extraLength = OrbDefinitions.Count;

            Array.Resize(ref orbCat, origLength + extraLength);

            for (int i = 0; i < extraLength; i++) {
                var temp = i + origLength;
                orbCat[temp] = OrbDefinitions[i];
                typeToIndex.Add(OrbDefinitions[i], temp);
            }

            typeof(RoR2.Orbs.OrbCatalog).SetFieldValue("indexToType", orbCat);
            typeof(RoR2.Orbs.OrbCatalog).SetFieldValue("typeToIndex", typeToIndex);
        }
    }
}
