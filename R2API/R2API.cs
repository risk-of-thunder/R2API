using BepInEx;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace R2API {

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    // ReSharper disable once InconsistentNaming
    public class R2API : BaseUnityPlugin {

        // ReSharper disable once InconsistentNaming
        public const string PluginGUID = "com.bepis.r2api";

        public const string PluginName = "R2API";
        public const string PluginVersion = "0.0.1";

        private const int GameBuild = 6427269;

        internal new static ManualLogSource Logger { get; set; }

        internal static DetourModManager ModManager;

        internal static event EventHandler R2APIStart;

        internal static HashSet<string> LoadedSubmodules;

        public void Awake() {
            Logger = base.Logger;
            ModManager = new DetourModManager();
            AddHookLogging();
            CheckForIncompatibleAssemblies();
            CheckR2APIPatch();

            Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", "Cecil");

            On.RoR2.UnitySystemConsoleRedirector.Redirect += orig => { };

            var pluginScanner = new PluginScanner();
            var submoduleHandler = new APISubmoduleHandler(GameBuild, Logger);
            LoadedSubmodules = submoduleHandler.LoadRequested(pluginScanner);
            pluginScanner.ScanPlugins();

            var networkCompatibilityHandler = new NetworkCompatibilityHandler();
            networkCompatibilityHandler.BuildModList();

            RoR2Application.isModded = true;

            SteamworksClientManager.onLoaded += CheckIfUsedOnRightGameVersion;

            VanillaFixes();

            R2APIContentPackProvider.Init();
        }

        private static void CheckIfUsedOnRightGameVersion() {
            var buildId = SteamworksClientManager.instance.steamworksClient.BuildId;

            if (GameBuild == buildId)
                return;

            Logger.LogWarning($"This version of R2API was built for build id \"{GameBuild}\", you are running \"{buildId}\".");
            Logger.LogWarning("Should any problems arise, please check for a new version before reporting issues.");
        }

        private static void VanillaFixes() {
            // Temporary fix until the Eclipse Button in the main menu is correctly set by the game devs.
            // It gets disabled when modded even though this option is currently singleplayer only.
            On.RoR2.DisableIfGameModded.OnEnable += (orig, self) => {
                if (self.name != "GenericMenuButton (Eclipse)") orig(self);
            };

            // Temporary fix until the KVP Foreach properly check for null Value before calling Equals on them
            IL.RoR2.SteamworksLobbyDataGenerator.RebuildLobbyData += il => {
                var c = new ILCursor(il);

                // ReSharper disable once InconsistentNaming
                static void ILFailMessage(int i) {
                    R2API.Logger.LogError(
                        $"Failed finding IL Instructions. Aborting RebuildLobbyData IL Hook ({i})");
                }

                if (c.TryGotoNext(i => i.MatchLdstr("v"))) {
                    if (c.TryGotoPrev(i => i.MatchLdloca(out _),
                        i => i.MatchCallOrCallvirt(out _))) {
                        var labelBeginningForEach = c.MarkLabel();

                        if (c.TryGotoPrev(i => i.MatchCallOrCallvirt<string>("Equals"))) {
                            var kvpLoc = 0;
                            if (c.TryGotoPrev(
                                i => i.MatchLdloc(out _),
                                i => i.MatchLdloca(out kvpLoc),
                                i => i.MatchCallOrCallvirt(out _))) {
                                c.Emit(OpCodes.Ldloc, kvpLoc);
                                c.EmitDelegate<Func<KeyValuePair<string, string>, bool>>(kvp => kvp.Value == null);
                                c.Emit(OpCodes.Brtrue, labelBeginningForEach);
                            }
                            else {
                                ILFailMessage(4);
                            }
                        }
                        else {
                            ILFailMessage(3);
                        }
                    }
                    else {
                        ILFailMessage(2);
                    }
                }
                else {
                    ILFailMessage(1);
                }
            };
        }

        public void Start() {
            R2APIStart?.Invoke(this, null);
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

        // ReSharper disable once InconsistentNaming
        private static void CheckR2APIPatch() {
            // This type is injected by the R2API MonoMod patch with MonoModRules
            const string searchableAttributeScanFixType = "R2API.SearchableAttributeScanFix";
            var isHere = typeof(RoR2Application).Assembly.GetType(searchableAttributeScanFixType, false) != null;

            if (!isHere) {
                var message = new List<string> {
                    "The patch of R2API seems to be missing",
                    "Please make sure that a file called:",
                    "R2API.Patcher.dll",
                    "is present in the Risk of Rain 2\\BepInEx\\patchers\\ folder",
                    "If you don't have this folder or the dll, please download BepInEx again from the",
                    "thunderstore and make sure to follow the installation instructions."
                };
                Logger.LogBlockError(message);
                DirectoryUtilities.LogFolderStructureAsTree(Paths.GameRootPath);
            }
        }
    }
}
