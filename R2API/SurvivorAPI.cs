using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using R2API.Utils;
using RoR2;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class SurvivorAPI {
        private static bool survivorsAlreadyAdded = false;

        public static event EventHandler SurvivorCatalogReady;

        public static ObservableCollection<SurvivorDef> SurvivorDefinitions = new ObservableCollection<SurvivorDef>();
        /// <summary>
        /// Add a SurvivorDef to the list of available survivors.
        /// This must be called before the SurvivorCatalog inits, so before plugin.Start()
        /// Value for SurvivorIndex is ignored by game code so can be left blank
        /// If this is called after the SurvivorCatalog inits then this will return false and ignore the survivor
        /// Can optionally specify a name for the survivor that will be logged with the mod info.
        /// The survivor prefab must be non-null
        /// </summary>
        /// <param name="survivor">The survivor to add.</param>
        /// <returns>true if survivor will be added</returns>
        public static bool AddSurvivor(SurvivorDef survivor) {
            if (survivorsAlreadyAdded) {
                R2API.Logger.LogError($"Tried to add survivor: {survivor.displayNameToken} after survivor list was created");
                return false;
            }

            if (!survivor.bodyPrefab) {
                R2API.Logger.LogError($"No prefab defined for survivor: {survivor.displayNameToken}");
                return false;
            }

            SurvivorDefinitions.Add(survivor);

            return true;
        }


        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            SurvivorCatalogReady?.Invoke(null, null);
            SurvivorCatalog.getAdditionalSurvivorDefs += AddSurvivorAction;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            SurvivorCatalog.getAdditionalSurvivorDefs -= AddSurvivorAction;
        }

        private static void AddSurvivorAction(List<SurvivorDef> survivorDefinitions) {
            // Set this to true so no more survivors can be added to the list while this is happening, or afterwards
            survivorsAlreadyAdded = true;

            // Get the count of the new survivors added, and the number of vanilla survivors
            var newSurvivorCount = SurvivorDefinitions.Count;
            var exisitingSurvivorCount = SurvivorCatalog.idealSurvivorOrder.Length;

            // Increase the size of the order array to accomodate the added survivors
            Array.Resize(ref SurvivorCatalog.idealSurvivorOrder, exisitingSurvivorCount + newSurvivorCount);

            // Increase the max survivor count to ensure there is enough space on the char select bar
            SurvivorCatalog.survivorMaxCount += newSurvivorCount;


            foreach (var survivor in SurvivorDefinitions) {

                //Check if the current survivor has been registered in bodycatalog. Log if it has not, but still add the survivor
                if (BodyCatalog.FindBodyIndex(survivor.bodyPrefab) == -1 || BodyCatalog.GetBodyPrefab(BodyCatalog.FindBodyIndex(survivor.bodyPrefab)) != survivor.bodyPrefab) {

                    R2API.Logger.LogWarning($"Survivor: {survivor.displayNameToken} is not properly registered in {nameof(BodyCatalog)}");
                }

                R2API.Logger.LogInfo($"Survivor: {survivor.displayNameToken} added");

                survivorDefinitions.Add(survivor);

                // Add that new survivor to the order array so the game knows where to put it in character select
                SurvivorCatalog.idealSurvivorOrder[exisitingSurvivorCount++] = (SurvivorIndex)exisitingSurvivorCount;
            }
        }
    }
}
