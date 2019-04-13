using System;
using System.Collections.Generic;
using System.Text;

namespace R2API
{
	public static class Hooks
	{
		internal static void InitializeHooks()
		{
			SurvivorAPI.InitHooks();
            AssetAPI.InitHooks();
			ItemDropAPI.InitHooks();
		}
	}
}
