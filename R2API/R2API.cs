﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using R2API.Commands;
using R2API.Utils;
using RoR2;

namespace R2API {
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    // ReSharper disable once InconsistentNaming
    public class R2API : BaseUnityPlugin {
        // ReSharper disable once InconsistentNaming
        public const string PluginGUID = "com.bepis.r2api";
        public const string PluginName = "R2API";
        public const string PluginVersion = "0.0.2";

        public static CommandAPI CommandAPI;
        public static PermissionsAPI PermissionsAPI;

        private const int GameBuild = 3961583;

        internal new static ManualLogSource Logger { get; set; }

        internal static DetourModManager ModManager;

        public R2API() {
            Logger = base.Logger;
            ModManager = new DetourModManager();
            AddHookLogging();

            CheckForIncompatibleAssemblies();

            Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", "Cecil");

            On.RoR2.RoR2Application.UnitySystemConsoleRedirector.Redirect += orig => { };
            var submoduleHandler = new APISubmoduleHandler(GameBuild, Logger);
            submoduleHandler.LoadAll(GetType().Assembly);

            RoR2Application.isModded = true;

            On.RoR2.DisableIfGameModded.OnEnable += (orig, self) => {
                // TODO: If we can enable quick play without regrets, uncomment.
                //if (self.name == "Button, QP")
                //    return;

                self.gameObject.SetActive(false);
            };

            On.RoR2.RoR2Application.OnLoad += (orig, self) => {
                orig(self);

                if (GameBuild == self.steamworksClient.BuildId)
                    return;

                Logger.LogWarning($"This version of R2API was built for build id \"{GameBuild}\", you are running \"{self.steamworksClient.BuildId}\".");
                Logger.LogWarning("Should any problems arise, please check for a new version before reporting issues.");
            };
        }

        public void Awake() {
            PermissionsAPI = new PermissionsAPI();
            CommandAPI = new CommandAPI();
        }

        public static void AddHookLogging() {
            ModManager.OnHook += (assembly, @base, arg3, arg4) => LogMethod(@base);
            ModManager.OnDetour += (assembly, @base, arg3) => LogMethod(@base);
            ModManager.OnNativeDetour += (assembly, @base, arg3, arg4) => LogMethod(@base);

            HookEndpointManager.OnAdd += (@base, @delegate) => LogMethod(@base);
            HookEndpointManager.OnModify += (@base, @delegate) => LogMethod(@base);
        }

        private static bool LogMethod(MemberInfo @base) {
            if (@base == null) {
                return true;
            }
            var declaringType = @base.DeclaringType;
            var name = @base.Name;
            var identifier = declaringType != null ? $"{declaringType}.{name}" : name;
            Logger.LogDebug($"Hook added for: {identifier}");
            return true;
        }

        public static bool SupportsVersion(string version) {
            var own = Version.Parse(PluginVersion);
            var v = Version.Parse(version);

            return own.Major == v.Major && own.Minor <= v.Minor;
        }

        private static void CheckForIncompatibleAssemblies() {
            var dirName = Directory.GetCurrentDirectory();
            var managed = System.IO.Path.Combine(dirName, "Risk of Rain 2_Data", "Managed");
            var dlls = Directory.GetFiles(managed, "*.dll");

            var info = new List<string> {
                "You have incompatible assemblies",
                "Please delete the following files from your managed folder:",
                ""
            };
            var countEmpty = info.Count;

            info.AddRange(dlls
                .Select(x => new FileInfo(x))
                .Where(x => Regex.IsMatch(x.Name
                    , @"(MonoMod*)|(Mono\.Cecil)"
                    , RegexOptions.Compiled | RegexOptions.IgnoreCase))
                .Select(x => x.Name));

            if (info.Count == countEmpty)
                return;

            Logger.LogBlockError(info);
        }
    }
}
