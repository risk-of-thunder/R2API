using System.Collections.ObjectModel;
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
}
