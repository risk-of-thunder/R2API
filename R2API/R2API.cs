using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;

namespace R2API {
    [BepInPlugin("com.bepis.r2api", "R2API", "2.0.0")]
    // ReSharper disable once InconsistentNaming
    public class R2API : BaseUnityPlugin {
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
