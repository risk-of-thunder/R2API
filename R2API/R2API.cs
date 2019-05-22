using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using R2API.Utils;
using RoR2;

namespace R2API {
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    // ReSharper disable once InconsistentNaming
    public class R2API : BaseUnityPlugin {

        // ReSharper disable once InconsistentNaming
        public const string PluginGUID = "tristanmcpherson-" + PluginName + "-" + PluginVersion;
        public const string PluginName = "R2API";
        public const string PluginVersion = "2.0.2";

        private const string GameBuild = "3830295";

        internal new static ManualLogSource Logger { get; set; }

        public R2API() {
            Logger = base.Logger;
            CheckForIncompatibleAssemblies();

            Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", "Cecil");

            InitConfig();

            Hooks.InitializeHooks();

            RoR2Application.isModded = true;

            On.RoR2.DisableIfGameModded.OnEnable += (orig, self) => {
                RoR2Application.isModded = true;
                orig(self);
            };

            On.RoR2.RoR2Application.OnLoad += (orig, self) => {
                orig(self);

                var build = typeof(RoR2Application).GetFieldValue<string>("steamBuildId");
                if (GameBuild == build)
                    return;

                Logger.LogWarning($"This version of R2API was built for build id \"{GameBuild}\", you are running \"{build}\".");
                Logger.LogWarning("Should any problems arise, please check for a new version before reporting issues.");
            };
        }

        public static bool SupportsVersion(string version) {
            var own = Version.Parse(PluginVersion);
            var v = Version.Parse(version);

            return own.Major == v.Major && own.Minor <= v.Minor;
        }

        private static void CheckForIncompatibleAssemblies() {
            const int width = 70;

            // ReSharper disable FormatStringProblem
            string CenterText(string text = "") =>
                string.Format("*{0," + (width / 2 + text.Length / 2) + "}{1," + (width / 2 - text.Length / 2) + "}*", text, " ");
            // ReSharper restore FormatStringProblem


            const string assemblies = "(MonoMod*)|(Mono\\.Cecil)";

            var dirName = Directory.GetCurrentDirectory();
            var managed = System.IO.Path.Combine(dirName, "Risk of Rain 2_Data", "Managed");
            var dlls = Directory.GetFiles(managed, "*.dll");

            var incompatibleFiles = new List<string>();

            foreach (var dll in dlls) {
                var file = new FileInfo(dll);

                if (Regex.IsMatch(file.Name, assemblies, RegexOptions.IgnoreCase)) {
                    incompatibleFiles.Add(file.Name);
                }
            }

            if (incompatibleFiles.Count <= 0) {
                return;
            }

            var top = new string('*', width + 2);

            Logger.LogError(top);
            Logger.LogError(CenterText());
            Logger.LogError($"{CenterText("!ERROR!")}");
            Logger.LogError($"{CenterText("You have incompatible assemblies")}");
            Logger.LogError($"{CenterText("Please delete the follow files from your managed folder")}");
            Logger.LogError(CenterText());

            foreach (var file in incompatibleFiles) {
                Logger.LogError($"{CenterText(file)}");
            }

            Logger.LogError(CenterText());
            Logger.LogError(top);
        }

        protected void InitConfig() {
        }
    }
}
