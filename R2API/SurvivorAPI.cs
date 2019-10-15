using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using R2API.Utils;
using UnityEngine;
using BepInEx;
using RoR2;

namespace R2API {
    [BepInPlugin("test", "test", "0.0.1")]
    // ReSharper disable once InconsistentNaming
    public class R2APITest : BaseUnityPlugin {
        public void Awake() {
            GameObject body = Resources.Load<GameObject>("Prefabs/CharacterBodies/AncientWispBody");

            //Rename the body so it doesn
            body.name = "WispSurvivor";

            //Queue the body to be added to the BodyCatalog
            //PrefabUtilities.RegisterNewBody(body);

            //Create the survivorDef
            SurvivorDef bodySurvivorDef = new SurvivorDef {
                bodyPrefab = body,
                descriptionToken = "asd",
                displayPrefab = Resources.Load<GameObject>("Prefabs/Characters/CommandoDisplay"),
                primaryColor = new Color(0.15f, 0.15f, 0.15f),
            };

            //Queue the survivorDef to be added to the survivorcatalog
            SurvivorAPI.AddSurvivor(bodySurvivorDef);
        }
    }


    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class SurvivorAPI {
        private static bool survivorsAlreadyAdded = false;

        public static event EventHandler SurvivorCatalogReady;

        public static ObservableCollection<SurvivorDef> SurvivorDefinitions = new ObservableCollection<SurvivorDef>();
        private static readonly Dictionary<SurvivorDef, string> _modInfo = new Dictionary<SurvivorDef, string>();
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
        public static bool AddSurvivor(SurvivorDef survivor, [CallerFilePath] string file = null, [CallerMemberName] string name = null, [CallerLineNumber] int lineNumber = 0) {

            string modInfo = GetModInfoString(file, name, lineNumber);

            if( survivorsAlreadyAdded ) {
                R2API.Logger.LogError($"Tried to add survivor: {survivor.displayNameToken} after survivor list was created at: {modInfo}");
                return false;
            }

            if (!survivor.bodyPrefab) {
                R2API.Logger.LogError($"No prefab defined for survivor: {survivor.displayNameToken} in {modInfo}");
                return false;
            }

            _modInfo[survivor] = modInfo;
            SurvivorDefinitions.Add(survivor);

            return true;
        }

        private static string GetModInfoString(string file, string name, int lineNumber) {
            R2API.Logger.LogError(file);

            //Isolate the name
            int start = file.LastIndexOf('/');
            int end = file.LastIndexOf('.');
            end -= start;
            string modName;
            if (end > 0) {
                modName = file.Substring(start + 1, end);
            }
            else {
                modName = file.Substring(start + 1);
            }
            //Return a string with the info
            return "Mod: " + modName + " Method: " + name + " Line: " + lineNumber.ToString();
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
            int newSurvivorCount = SurvivorDefinitions.Count;
            int exisitingSurvivorCount = SurvivorCatalog.idealSurvivorOrder.Length;

            // Increase the size of the order array to accomodate the added survivors
            Array.Resize(ref SurvivorCatalog.idealSurvivorOrder, exisitingSurvivorCount + newSurvivorCount);

            // Increase the max survivor count to ensure there is enough space on the char select bar
            SurvivorCatalog.survivorMaxCount += newSurvivorCount;


            foreach (var survivor in SurvivorDefinitions) {
                var modName = _modInfo[survivor];

                //Check if the current survivor has been registered in bodycatalog. Log if it has not, but still add the survivor
                if (BodyCatalog.FindBodyIndex(survivor.bodyPrefab) == -1 || BodyCatalog.GetBodyPrefab(BodyCatalog.FindBodyIndex(survivor.bodyPrefab)) != survivor.bodyPrefab) {

                    R2API.Logger.LogWarning($"Survivor: {survivor.displayNameToken} is not properly registered in bodycatalog by: {modName}");
                }

                R2API.Logger.LogInfo($"Survivor: {survivor.displayNameToken} added by: {modName}");

                survivorDefinitions.Add(survivor);

                // Add that new survivor to the order array so the game knows where to put it in character select
                SurvivorCatalog.idealSurvivorOrder[exisitingSurvivorCount++] = (SurvivorIndex)exisitingSurvivorCount;
            }
        }
    }
}
