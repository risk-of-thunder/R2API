using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonoMod.RuntimeDetour;
using R2API.Utils;
using RoR2;

namespace R2API
{
	public static class ItemAPI
	{
		public static ObservableCollection<ItemDef> ItemDefinitions { get; private set; }
		internal static void InitHooks()
		{
			
		}

		
	}
	
	public class CustomItem
	{
		public ItemDef Item { get; private set; }
		public List<PlayerStatChange> StatEffects { get; } = new List<PlayerStatChange>();

	}
}
