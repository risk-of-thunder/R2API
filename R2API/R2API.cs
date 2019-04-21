using System;
using System.Reflection;
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
            CheckForIncompatibleAssemblies();


            Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", "Cecil");

			InitConfig();

			Hooks.InitializeHooks();

			RoR2Application.isModded = IsModded.Value;
		}

        private void CheckForIncompatibleAssemblies()
        {

            string CenterText(string text, int w)
            {
                return string.Format("*{0," + ((w / 2) + (text.Length / 2)) + "}{ ," + ((w/2) + (text.Length/2)) + "}*", text);
            }

            var assemblies = new[] { "MonoMod.*", "Mono.*" };

            var dir = Assembly.GetCallingAssembly().Location;

            Logger.LogWarning(dir);


            const int width = 50;
            var top = new string('*', width);
            string s = "You have some incompatible assemblies";
            
            Logger.LogWarning(top);
            Logger.LogWarning($"*{CenterText("!WARNING!", width-2)}*");
            Logger.LogWarning($"*{CenterText("You may have incompatible assemblies", width-2)}*");
            Logger.LogWarning("*                                     *");
            Logger.LogWarning("*                                     *");
            Logger.LogWarning("*                                     *");
            Logger.LogWarning("*                                     *");
            Logger.LogWarning(top);


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