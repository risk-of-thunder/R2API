using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;

namespace R2API
{
	[BepInPlugin("com.bepis.r2api", "R2API", "1.0")]
	public class R2API : BaseUnityPlugin
	{
		internal new static ManualLogSource Logger { get; set; }

		public static ConfigWrapper<bool> IsModded { get; protected set; }

		public R2API()
		{
			Logger = base.Logger;
			Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", "Cecil");
			Environment.SetEnvironmentVariable("MONOMOD_DMD_DUMP", "dmddump");

			On.RoR2.RoR2Application.UnitySystemConsoleRedirector.Redirect += (orig) => { };

			InitConfig();

			Hooks.InitializeHooks();

			RoR2Application.isModded = IsModded.Value;
		}

		protected void InitConfig()
		{
			IsModded = Config.Wrap(
				section: "Game",
				key: "IsModded",
				description: "Enables or disables the isModded flag in the game, which affects if you will be matched with other modded users.",
				defaultValue: true);
		}
	}
}