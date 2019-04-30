using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2.UI;
using System;

namespace R2API
{
	public static class InventoryAPI
	{
		public static event Action<ItemIcon> OnItemIconAdded;
		public static event Action<EquipmentIcon> OnEquipmentIconAdded;

		public static void InitHooks()
		{
			IL.RoR2.UI.ItemInventoryDisplay.AllocateIcons += il =>
			{
				var cursor = new ILCursor(il).Goto(0);
				cursor.GotoNext(x => x.MatchStloc(0));
				cursor.Emit(OpCodes.Dup);
				cursor.EmitDelegate<Action<ItemIcon>>(i => OnItemIconAdded?.Invoke(i));
			};

			var setSubscribedInventory = typeof(ItemInventoryDisplay).GetMethod("SetSubscribedInventory");

			IL.RoR2.UI.ScoreboardStrip.SetMaster += il =>
			{
				var cursor = new ILCursor(il).Goto(0);
				cursor.GotoNext(x => x.MatchCallvirt(setSubscribedInventory));
				cursor.Index += 1;

				cursor.Emit(OpCodes.Ldarg_0);

				cursor.EmitDelegate<Action<ScoreboardStrip>>(eq =>
				{
					if (eq.equipmentIcon != null)
					{
						OnEquipmentIconAdded?.Invoke(eq.equipmentIcon);
					}
				});
			};
		}
	}
}