using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API
{
	public static class InventoryAPI
	{
		public static event Action<ItemIcon> OnItemIconAdded;

		public static void InitHooks()
		{
			var inventory_onInventoryChanged = typeof(RoR2.Inventory).GetField("onInventoryChanged");

			IL.RoR2.UI.ItemInventoryDisplay.AllocateIcons += (il) => {
				//il.PrintInstrs();
				var cursor = new ILCursor(il).Goto(0);
				cursor.GotoNext(x => x.MatchStloc(0));
				cursor.Emit(OpCodes.Dup);
				cursor.EmitDelegate<Action<ItemIcon>>(i => OnItemIconAdded?.Invoke(i));
				//il.PrintInstrs();
			};

			//IL.RoR2.UI.ItemInventoryDisplay.Alloc

			Debug.Log("[R2API] Hooked into ItemInventoryDisplay.AllocateIcons");
		}
	}
}
