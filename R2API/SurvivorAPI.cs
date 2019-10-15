using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Linq;
using MonoMod.RuntimeDetour;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class SurvivorAPI {

        private static bool eventRegistered = false;
        private static bool survivorsAlreadyAdded = false;

        private static List<SurvivorDef> newSurvivors = new List<SurvivorDef>();
        private static List<string> modInfoList = new List<string>();
        private static List<string> survNameList = new List<string>();

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
        public static bool AddSurvivor(SurvivorDef survivor, string survivorName = "", [CallerFilePath] string file = null, [CallerMemberName] string name = null, [CallerLineNumber] int lineNumber = 0) {

            string modInfo = GetModInfoString(file, name, lineNumber);

            if( survivorsAlreadyAdded ) {
                Debug.Log("Tried to add survivor: " + survivorName + " after survivor list was created at: " + modInfo);
                return false;
            }
            if (!survivor.bodyPrefab) {
                Debug.Log("No prefab defined for survivor: " + survivorName + " in " + modInfo);
                return false;
            }

            newSurvivors.Add(survivor);
            modInfoList.Add(modInfo);
            survNameList.Add(survivorName);
            RegisterEvent();

            return true;
        }

        private static string GetModInfoString(string file, string name, int lineNumber) {
            //Isolate the name
            int start = file.LastIndexOf('/');
            int end = file.LastIndexOf('.');
            end -= start;
            string modName = "";
            if (end > 0) {
                modName = file.Substring(start + 1, end);
            }
            else {
                modName = file.Substring(start + 1);
            }
            //Return a string with the info
            return "Mod: " + modName + " Method: " + name + " Line: " + lineNumber.ToString();
        }

        private static void RegisterEvent() {
            //Make sure we aren't registering the event to add survivors more than once
            if (eventRegistered) {
                return;
            }

            //Actually register the event
            RoR2.SurvivorCatalog.getAdditionalSurvivorDefs += AddSurvivorAction;
            eventRegistered = true;
        }
        private static void UnRegisterEvent() {
            //Unregister the event, it will not be called again anyway but good habits
            RoR2.SurvivorCatalog.getAdditionalSurvivorDefs -= AddSurvivorAction;
        }

        private static void AddSurvivorAction(List<SurvivorDef> obj) {
            //Set this to true so no more survivors can be added to the list while this is happening, or afterwards
            survivorsAlreadyAdded = true;

            //Get the count of the new survivors added, and the number of vanilla survivors
            int count = newSurvivors.Count;
            int baseCount = SurvivorCatalog.idealSurvivorOrder.Length;

            //Increase the size of the order array to accomodate the added survivors
            Array.Resize<SurvivorIndex>(ref SurvivorCatalog.idealSurvivorOrder, baseCount + count);

            //Increase the max survivor count to ensure there is enough space on the char select bar
            SurvivorCatalog.survivorMaxCount += count;

            SurvivorDef curSurvivor;

            //Loop through the new survivors
            for (int i = 0; i < count; i++) {
                curSurvivor = newSurvivors[i];

                //Check if the current survivor has been registered in bodycatalog. Log if it has not, but still add the survivor
                if (BodyCatalog.FindBodyIndex(curSurvivor.bodyPrefab) == -1 || BodyCatalog.GetBodyPrefab(BodyCatalog.FindBodyIndex(curSurvivor.bodyPrefab)) != curSurvivor.bodyPrefab) {
                    Debug.Log("Survivor: " + survNameList[i] + " is not properly registered in bodycatalog by: " + modInfoList[i]);
                }

                //Log that a survivor is being added, and add that survivor
                Debug.Log("Survivor: " + survNameList[i] + " added by: " + modInfoList[i]);
                obj.Add(newSurvivors[i]);

                //Add that new survivor to the order array so the game knows where to put it in character select
                SurvivorCatalog.idealSurvivorOrder[baseCount + i] = (SurvivorIndex)(i + baseCount + 1);
            }

            //Unregister the event because it won't be called again anyway
            UnRegisterEvent();
        }
    }
}
