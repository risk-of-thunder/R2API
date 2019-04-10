using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using MonoMod.RuntimeDetour;
using RoR2;
using UnityEngine;

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
