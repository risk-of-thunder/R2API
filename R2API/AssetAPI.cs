using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using MonoMod.Utils;
using MonoMod.RuntimeDetour;
using RoR2;
using UnityEngine;


namespace R2API
{
    public static class AssetAPI
    {
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
        public static List<GameObject> MasterCatalog { get; private set; }

        /// <summary>
        /// List of all character bodies, including both vanilla and modded ones.
        /// </summary>
        public static List<GameObject> BodyCatalog { get; private set; }

        internal static void InitHooks()
        {
            AssetLoaderReady.Invoke(null,null);

            IL.RoR2.MasterCatalog.Init += (orig) =>
            {
                MMILCursor c = orig.At(0);
                c.Remove();
                c.Remove();
                c.EmitDelegate<Func<GameObject[]>>(BuildMasterCatalog);
            };

            IL.RoR2.BodyCatalog.Init += (orig) =>
            {
                MMILCursor c = orig.At(0);
                c.Remove();
                c.Remove();
                c.EmitDelegate<Func<GameObject[]>>(BuildBodyCatalog);
            };
            doneLoading = true;
        }

        internal static GameObject[] BuildMasterCatalog()
        {
            MasterCatalog.AddRange(Resources.LoadAll<GameObject>("Prefabs/CharacterMasters/"));
            return MasterCatalog.ToArray();
        }
        internal static GameObject[] BuildBodyCatalog()
        {
            BodyCatalog.AddRange(Resources.LoadAll<GameObject>("Prefabs/CharacterBodies/"));
            return BodyCatalog.ToArray();
        }
    }
}
