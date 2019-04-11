using System;
using System.Collections.Generic;
using UnityEngine;
using MonoMod.Cil;

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
		public static List<GameObject> MasterCatalog { get; private set; } = new List<GameObject>();

		/// <summary>
		/// List of all character bodies, including both vanilla and modded ones.
		/// </summary>
		public static List<GameObject> BodyCatalog { get; private set; } = new List<GameObject>();

		internal static void InitHooks()
		{
			AssetLoaderReady?.Invoke(null, null);

			IL.RoR2.MasterCatalog.Init += (il) =>
			{
				ILCursor c = new ILCursor(il).Goto(0); //Initialize IL cursor at position 0
				c.Remove();  //Deletes the "Prefabs/CharacterMasters" string being stored in the stack
				c.Goto(0);
				c.Remove(); //Deletes the call Resources.Load<GameObjec>() from the stack
				c.Goto(0);
				//Stores the new GameObject[] in the static field MasterCatalog.masterPrefabs.
				//This array contains both vanilla and modded Character Masters
				c.EmitDelegate<Func<GameObject[]>>(BuildMasterCatalog); 
			};

			IL.RoR2.BodyCatalog.Init += (il) =>
			{
				ILCursor c = new ILCursor(il).Goto(0); //Initialize IL cursor at position 0
				c.Remove(); //Deletes the "Prefabs/CharacterBodies/" string being stored in the stack
				c.Goto(0);
				c.Remove(); //Deletes the call Resources.Load<GameObjec>() from the stack
				c.Goto(0);
				//Stores the new GameObject[] in the static field BodyCatalog.bodyPrefabs
				//This array contains both vanilla and modded Body prefabs.
				//TODO: find a way to also add 2d sprites, as are done on line 113 and have a very hard-coded path 
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
