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
    public class R2API : BaseUnityPlugin {
        internal new static ManualLogSource Logger { get; set; }

        public static ConfigWrapper<bool> IsModded { get; protected set; }

        public R2API() {
            Logger = base.Logger;
            CheckForIncompatibleAssemblies();


            Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", "Cecil");

            InitConfig();

            Hooks.InitializeHooks();

            RoR2Application.isModded = IsModded.Value;
        }

        private void CheckForIncompatibleAssemblies() {
            const int width = 70;

            string CenterText(string text = "") {
                return string.Format(
                    "*{0," + (width / 2 + text.Length / 2) + "}{1," + (width / 2 - text.Length / 2) + "}*", text, " ");
            }


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
            IsModded = Config.Wrap(
                section: "Game",
                key: "IsModded",
                description:
                "Enables or disables the isModded flag in the game, which affects if you will be matched with other modded users.",
                defaultValue: true);
        }
    }
}
