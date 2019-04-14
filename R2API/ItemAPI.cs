using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RoR2;

namespace R2API
{
	public static class ItemAPI
	{
		public static ObservableCollection<ItemDef> ItemDefinitions { get; private set; }
		internal static void InitHooks() { }
	}

	public class CustomItem
	{
		public ItemDef Item { get; private set; }
		public List<Action<PlayerStats>> StatEffects { get; } = new List<Action<PlayerStats>>();
	}
}