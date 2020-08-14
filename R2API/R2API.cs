using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using Facepunch.Steamworks;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace R2API {
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    // ReSharper disable once InconsistentNaming
    public class R2API : BaseUnityPlugin {
        // ReSharper disable once InconsistentNaming
        public const string PluginGUID = "com.bepis.r2api";
        public const string PluginName = "R2API";
        public const string PluginVersion = "0.0.1";


        private const int GameBuild = 4892828;

        internal new static ManualLogSource Logger { get; set; }

        internal static DetourModManager ModManager;

        internal static event EventHandler R2APIStart;

        internal static HashSet<string> loadedSubmodules;

        public R2API() {
            Logger = base.Logger;
            ModManager = new DetourModManager();
            AddHookLogging();
            CheckForIncompatibleAssemblies();
            CheckR2APIMonomodPatch();

            Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", "Cecil");

            On.RoR2.UnitySystemConsoleRedirector.Redirect += orig => { };

            var submoduleHandler = new APISubmoduleHandler(GameBuild, Logger);
            loadedSubmodules = submoduleHandler.LoadRequested();

            //Currently disabled until manifest v2
            //ModListAPI.Init();

            RoR2Application.isModded = true;

            // Temporary fix as the new quickplay button currently don't have the DisableIfGameModded Script attached to it
            On.RoR2.UI.QuickPlayButtonController.Start += (orig, self) => {
                orig(self);
                self.gameObject.SetActive(false);
            };

            On.RoR2.DisableIfGameModded.OnEnable += (orig, self) => {
                // TODO: If we can enable quick play without regrets, uncomment.
                //if (self.name == "Button, QP")
                //    return;

                self.gameObject.SetActive(false);
            };

            On.RoR2.Networking.SteamLobbyFinder.CCSteamQuickplayStart += (orig, args) => {
                Debug.Log("QuickPlay is disabled in mods due to social contracts and lack of general support");
            };

            SteamworksClientManager.onLoaded += () => {
                var buildId =
                    SteamworksClientManager.instance.GetFieldValue<Client>("steamworksClient").BuildId;

                if (GameBuild == buildId)
                    return;

                Logger.LogWarning($"This version of R2API was built for build id \"{GameBuild}\", you are running \"{buildId}\".");
                Logger.LogWarning("Should any problems arise, please check for a new version before reporting issues.");
            };

            // Make sure that modded dedicated servers are recognizable from the server browser
            On.RoR2.SteamworksServerManager.UpdateHostName += (orig, self, hostname) => {
                var server = ((SteamworksServerManager)self).GetFieldValue<Server>("steamworksServer");
                server.GameTags = "mod," + server.GameTags;
            };
        }

        public void Start() {
            if (R2APIStart != null)
                R2APIStart.Invoke(this, null);
        }


        /// <summary>
        /// Return true if the specified submodule is loaded.
        /// </summary>
        /// <param name="submodule">nameof the submodule</param>
        public static bool IsLoaded(string submodule) {
            if (loadedSubmodules == null) {
                Logger.LogWarning("IsLoaded called before submodules were loaded, result may not reflect actual load status.");
                return false;
            }
            return loadedSubmodules.Contains(submodule);
        }

        private static void AddHookLogging() {
            ModManager.OnHook += (hookOwner, @base, arg3, arg4) => LogMethod(@base, hookOwner);
            ModManager.OnDetour += (hookOwner, @base, arg3) => LogMethod(@base, hookOwner);
            ModManager.OnNativeDetour += (hookOwner, @base, arg3, arg4) => LogMethod(@base, hookOwner);

            HookEndpointManager.OnAdd += (@base, @delegate) => LogMethod(@base, @delegate.Method.Module.Assembly);
            HookEndpointManager.OnModify += (@base, @delegate) => LogMethod(@base, @delegate.Method.Module.Assembly);
        }

        private static bool LogMethod(MemberInfo @base, Assembly hookOwnerAssembly) {
            if (@base == null) {
                return true;
            }

            var hookOwnerDllName = "Not Found";
            if (hookOwnerAssembly != null) {
                // Get the dll name instead of assembly manifest name as this one one could be not correctly set by mod maker.
                hookOwnerDllName = System.IO.Path.GetFileName(hookOwnerAssembly.Location);
            }

            var declaringType = @base.DeclaringType;
            var name = @base.Name;
            var identifier = declaringType != null ? $"{declaringType}.{name}" : name;

            Logger.LogDebug($"Hook added by assembly: {hookOwnerDllName} for: {identifier}");
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

        // ReSharper disable once InconsistentNaming
        private static void CheckR2APIMonomodPatch() {
            var isHere = AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.FullName.ToLower().Contains("r2api.mm.monomodrules"));

            if (!isHere) {
                var message = new List<string> {
                    "The Monomod patch of R2API seems to be missing",
                    "Please make sure that a file called:",
                    "Assembly-CSharp.R2API.mm.dll",
                    "is present in the Risk of Rain 2\\BepInEx\\monomod\\ folder",
                };
                Logger.LogBlockError(message);
            }
        }
    }
}
