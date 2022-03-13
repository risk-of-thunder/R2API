using BepInEx;
using BepInEx.Logging;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace R2API {

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    // ReSharper disable once InconsistentNaming
    public class R2API : BaseUnityPlugin {

        // ReSharper disable once InconsistentNaming
        public const string PluginGUID = "com.bepis.r2api";

        public const string PluginName = "R2API";
        public const string PluginVersion = "0.0.1";

        private const string GameBuildId = "1.2.2.0";

        internal new static ManualLogSource Logger { get; set; }
        public static bool DebugMode { get; private set; } = false;

        internal static DetourModManager ModManager;

        internal static event EventHandler R2APIStart;

        internal static HashSet<string> LoadedSubmodules;

        internal static R2API Instance { get; private set; }

        public void Awake() {
            Instance = this;

            Logger = base.Logger;

            ModManager = new DetourModManager();
            AddHookLogging();

            CheckForIncompatibleAssemblies();

            if (Environment.GetEnvironmentVariable("R2API_DEBUG") == "true") {
                EnableDebug();
            }

            var pluginScanner = new PluginScanner();
            var submoduleHandler = new APISubmoduleHandler(Logger);
            LoadedSubmodules = submoduleHandler.LoadRequested(pluginScanner);
            pluginScanner.ScanPlugins();

            var networkCompatibilityHandler = new NetworkCompatibilityHandler();
            networkCompatibilityHandler.BuildModList();

            SteamworksClientManager.onLoaded += CheckIfUsedOnRightGameVersion;

            R2APIContentPackProvider.Init();
        }

        private static void EnableDebug() {
            DebugMode = true;

            Logger.LogBlockWarning(new[] { "R2API IS IN DEBUG MODE. ONLY USE IF YOU KNOW WHAT YOU ARE DOING" });
        }

        private static void DebugUpdate() {

        }

        /// <summary>
        /// Logs caller information along side debug message
        /// </summary>
        /// <param name="debugText"></param>
        /// <param name="caller"></param>
        public static void LogDebug(object debugText, [CallerMemberName] string caller = "") {
            Logger.LogDebug(caller + " : " + debugText.ToString());
        }

        private static void CheckIfUsedOnRightGameVersion() {
            var buildId = Application.version;

            if (GameBuildId == buildId)
                return;

            Logger.LogWarning($"This version of R2API was built for build id \"{GameBuildId}\", you are running \"{buildId}\".");
            Logger.LogWarning("Should any problems arise, please check for a new version before reporting issues.");
        }

        public void Start() {
            R2APIStart?.Invoke(this, null);
        }

        public void Update() {
            if (DebugMode) {
                DebugUpdate();
            }
        }

        /// <summary>
        /// Return true if the specified submodule is loaded.
        /// </summary>
        /// <param name="submodule">nameof the submodule</param>
        public static bool IsLoaded(string submodule) {
            if (LoadedSubmodules == null) {
                Logger.LogWarning("IsLoaded called before submodules were loaded, result may not reflect actual load status.");
                return false;
            }
            return LoadedSubmodules.Contains(submodule);
        }

        private static void AddHookLogging() {
            ModManager.OnHook += (hookOwner, @base, _, __) => LogMethod(@base, hookOwner);
            ModManager.OnDetour += (hookOwner, @base, _) => LogMethod(@base, hookOwner);
            ModManager.OnNativeDetour += (hookOwner, @base, _, __) => LogMethod(@base, hookOwner);
            ModManager.OnILHook += (hookOwner, @base, _) => LogMethod(@base, hookOwner);

            HookEndpointManager.OnAdd += (@base, @delegate) => LogMethod(@base, @delegate.Method.Module.Assembly);
            HookEndpointManager.OnModify += (@base, @delegate) => LogMethod(@base, @delegate.Method.Module.Assembly);
            HookEndpointManager.OnRemove += (@base, @delegate) => LogMethod(@base, @delegate.Method.Module.Assembly, false);
        }

        private static bool LogMethod(MemberInfo @base, Assembly hookOwnerAssembly, bool added = true) {
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

            Logger.LogDebug($"Hook {(added ? "added" : "removed")} by assembly: {hookOwnerDllName} for: {identifier}");
            return true;
        }

        public static bool SupportsVersion(string? version) {
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
