using System;
using System.Collections.Generic;
using UnityEngine;
using MonoMod.Cil;
using R2API.Utils;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    public static class AssetAPI {
        /// <summary>
        /// This event is invoked as soon as the AssetAPI is loaded. This is the perfect time to add assets to the Master and Object Catalogs in the API.
        /// </summary>
        public static event EventHandler AssetLoaderReady;

        /// <summary>
        /// Returns true once assets have been loaded.
        /// </summary>
        public static bool doneLoading { get; private set; }

        /// <summary>
        /// List of all character masters, including both vanilla and modded ones.
        /// </summary>
        // TODO: implement late load for MasterCatalog the same way as for BodyCatalog
        public static List<GameObject> MasterCatalog { get; private set; } = new List<GameObject>();


        #region BodyCatalog

        /// <summary>
        /// List of all character bodies, including both vanilla and modded ones.
        /// </summary>
        public static readonly List<GameObject> BodyCatalog = new List<GameObject>();

        /// <summary>
        /// If BodyCatalog.Init was called already.
        /// </summary>
        private static bool _bodyCatalogReady;

        /// <summary>
        /// Invokes just before BodyCatalog.Init - EventArgs is AssetAPI.BodyCatalog.
        /// </summary>
        public static event EventHandler<List<GameObject>> OnBodyCatalogReady;


        /// <summary>
        /// Add a BodyPrefab to RoR2.BodyCatalog, even after init.
        /// </summary>
        /// <param name="bodyPrefab"></param>
        /// <param name="portraitIcon"></param>
        /// <returns>The index of your BodyPrefab.</returns>
        public static int AddToBodyCatalog(GameObject bodyPrefab, Texture2D portraitIcon = null) {
            BodyCatalog.Add(bodyPrefab);

            if (!_bodyCatalogReady)
                return BodyCatalog.Count - 1;

            var bodyPrefabs =
                typeof(RoR2.BodyCatalog).GetFieldValue<GameObject[]>("bodyPrefabs");
            var bodyPrefabBodyComponents =
                typeof(RoR2.BodyCatalog).GetFieldValue<RoR2.CharacterBody[]>("bodyPrefabBodyComponents");
            var nameToIndexMap =
                typeof(RoR2.BodyCatalog).GetFieldValue<Dictionary<string, int>>("nameToIndexMap");

            var index = bodyPrefabs.Length;
            Array.Resize(ref bodyPrefabs, index + 1);

            bodyPrefabs[index] = bodyPrefab;
            nameToIndexMap.Add(bodyPrefab.name, index);
            nameToIndexMap.Add(bodyPrefab.name + "(Clone)", index);

            Array.Resize(ref bodyPrefabBodyComponents, index + 1);
            bodyPrefabBodyComponents[index] = bodyPrefab.GetComponent<RoR2.CharacterBody>();

            if (portraitIcon != null)
                bodyPrefabBodyComponents[index].portraitIcon = portraitIcon;


            typeof(RoR2.BodyCatalog).SetFieldValue("bodyPrefabs", bodyPrefabs);
            typeof(RoR2.BodyCatalog).SetFieldValue("bodyPrefabBodyComponents", bodyPrefabBodyComponents);

            return index;
        }

        #endregion

        internal static void InitHooks() {
            AssetLoaderReady?.Invoke(null, null);

            IL.RoR2.MasterCatalog.Init += il => {
                var c = new ILCursor(il).Goto(0); //Initialize IL cursor at position 0
                c.Remove(); //Deletes the "Prefabs/CharacterMasters" string being stored in the stack
                c.Goto(0);
                c.Remove(); //Deletes the call Resources.Load<GameObject>() from the stack
                c.Goto(0);
                //Stores the new GameObject[] in the static field MasterCatalog.masterPrefabs.
                //This array contains both vanilla and modded Character Masters
                c.EmitDelegate<Func<GameObject[]>>(BuildMasterCatalog);
            };

            IL.RoR2.BodyCatalog.Init += il => {
                var c = new ILCursor(il).Goto(0); //Initialize IL cursor at position 0
                c.Remove(); //Deletes the "Prefabs/CharacterBodies/" string being stored in the stack
                c.Goto(0);
                c.Remove(); //Deletes the call Resources.Load<GameObject>() from the stack
                c.Goto(0);
                //Stores the new GameObject[] in the static field BodyCatalog.bodyPrefabs
                //This array contains both vanilla and modded Body prefabs.
                //TODO: find a way to also add 2d sprites, as are done on line 113 and have a very hard-coded path
                c.EmitDelegate<Func<GameObject[]>>(BuildBodyCatalog);
            };
            doneLoading = true;

            On.RoR2.BodyCatalog.Init += orig => {
                OnBodyCatalogReady?.Invoke(null, BodyCatalog);

                orig();

                _bodyCatalogReady = true;
            };
        }

        internal static GameObject[] BuildMasterCatalog() {
            MasterCatalog.AddRange(Resources.LoadAll<GameObject>("Prefabs/CharacterMasters/"));
            return MasterCatalog.ToArray();
        }

        internal static GameObject[] BuildBodyCatalog() {
            BodyCatalog.AddRange(Resources.LoadAll<GameObject>("Prefabs/CharacterBodies/"));
            return BodyCatalog.ToArray();
        }
    }
}
