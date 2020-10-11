using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using R2API.MiscHelpers;
using R2API.Networking;
using RoR2;
using Console = System.Console;

namespace R2API.Utils {
    /// <summary>
    /// Enum used for telling whether or not the mod should be needed by everyone in multiplayer games.
    /// Also can specify if the mod does not work in multiplayer.
    /// </summary>
    public enum CompatibilityLevel {
        NoNeedForSync,
        EveryoneMustHaveMod,
        //BreaksMultiplayer //todo
    }

    /// <summary>
    /// Enum used for telling whether or not the same mod version should be used by both the server and the clients.
    /// This enum is only useful if CompatibilityLevel.EveryoneMustHaveMod was chosen.
    /// </summary>
    public enum VersionStrictness {
        DifferentModVersionsAreOk,
        EveryoneNeedSameModVersion
    }

    /// <summary>
    /// Attribute to have at the top of your BaseUnityPlugin class if
    /// you want to specify if the mod should be installed by everyone in multiplayer games or not.
    /// If the mod is required to be installed by everyone, you'll need to also specify if the same mod version should be used by everyone or not.
    /// By default, it's supposed that everyone needs the mod and the same version.
    /// e.g: [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class NetworkCompatibility : Attribute {
        /// <summary>
        /// Used for telling whether or not the mod should be needed by everyone in multiplayer games.
        /// </summary>
        public CompatibilityLevel CompatibilityLevel { get; internal set; }

        /// <summary>
        /// Enum used for telling whether or not the same mod version should be used by both the server and the clients.
        /// This enum is only useful if CompatibilityLevel.EveryoneMustHaveMod was chosen.
        /// </summary>
        public VersionStrictness VersionStrictness { get; internal set; }

        public NetworkCompatibility(
            CompatibilityLevel compatibility = CompatibilityLevel.EveryoneMustHaveMod,
            VersionStrictness versionStrictness = VersionStrictness.EveryoneNeedSameModVersion) {
            CompatibilityLevel = compatibility;
            VersionStrictness = versionStrictness;
        }
    }

    /// <summary>
    /// Forward declare this attribute in your plugin assembly
    /// and use this one instead if you are already registering your plugin yourself
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ManualNetworkRegistrationAttribute : Attribute { }

    internal class NetworkCompatibilityHandler {
        internal const char ModGuidAndModVersionSeparator = ';';
        internal readonly HashSet<string> ModList = new HashSet<string>();

        internal void BuildModList() {
            R2API.R2APIStart += ScanPluginsForNetworkCompat;
        }

        private void ScanPluginsForNetworkCompat(object? _, EventArgs __) {
            foreach (var (_, pluginInfo) in BepInEx.Bootstrap.Chainloader.PluginInfos) {
                try {
                    var pluginAssembly = pluginInfo.Instance.GetType().Assembly;
                    var modGuid = pluginInfo.Metadata.GUID;
                    var modVer = pluginInfo.Metadata.Version;

                    if (modGuid == R2API.PluginGUID) {
                        continue;
                    }

                    if (AssemblyHasManualRegistration(pluginAssembly)) {
                        continue;
                    }

                    TryGetNetworkCompatibility(pluginInfo.Instance.GetType(), out var networkCompatibility);
                    if (networkCompatibility.CompatibilityLevel == CompatibilityLevel.EveryoneMustHaveMod) {
                        ModList.Add(networkCompatibility.VersionStrictness == VersionStrictness.EveryoneNeedSameModVersion
                            ? modGuid + ModGuidAndModVersionSeparator + modVer
                            : modGuid);
                    }
                }
                catch (Exception e) {
                    R2API.Logger.LogError($"Exception in ScanPluginsForNetworkCompat while scanning plugin {pluginInfo.Metadata.GUID}");
                    R2API.Logger.LogError("R2API Failed to properly scan the assembly." + Environment.NewLine +
                                          "Please make sure you are compiling against net standard 2.0 " +
                                          "and not anything else when making a plugin for Risk of Rain 2 !" +
                                          Environment.NewLine + e);
                }
            }

            AddToNetworkModList();
            R2API.R2APIStart -= ScanPluginsForNetworkCompat;
        }

        private static bool AssemblyHasManualRegistration(Assembly assembly) {
            foreach (var assemblyAttribute in assembly.CustomAttributes) {
                if (assemblyAttribute.AttributeType.FullName == typeof(ManualNetworkRegistrationAttribute).FullName) {
                    return true;
                }
            }

            return false;
        }

        // TODO: Should remove disable of nullable context, but changes here require extra testing.
        #pragma warning disable CS8605 // Unboxing a possibly null value.
        private static void TryGetNetworkCompatibility(Type baseUnityPluginType, out NetworkCompatibility networkCompatibility) {
            networkCompatibility = new NetworkCompatibility();

            foreach (var assemblyAttribute in baseUnityPluginType.Assembly.CustomAttributes) {
                if (assemblyAttribute.AttributeType == typeof(NetworkCompatibility)) {

                    networkCompatibility.CompatibilityLevel = (CompatibilityLevel)assemblyAttribute.ConstructorArguments[0].Value;

                    networkCompatibility.VersionStrictness = (VersionStrictness)assemblyAttribute.ConstructorArguments[1].Value;

                    return;
                }
            }

            foreach (var attribute in baseUnityPluginType.CustomAttributes) {
                if (attribute.AttributeType == typeof(NetworkCompatibility)) {
                    networkCompatibility.CompatibilityLevel = (CompatibilityLevel)attribute.ConstructorArguments[0].Value;
                    networkCompatibility.VersionStrictness = (VersionStrictness)attribute.ConstructorArguments[1].Value;
                }
            }
        }
        #pragma warning restore CS8605 // Unboxing a possibly null value.

        private void AddToNetworkModList() {
            if (ModList.Count != 0) {
                if (IsR2APIAffectingNetwork()) {
                    ModList.Add(R2API.PluginGUID + ModGuidAndModVersionSeparator + R2API.PluginVersion);
                }
                var sortedModList = ModList.ToList();
                sortedModList.Sort();
                R2API.Logger.LogInfo("[NetworkCompatibility] Adding to the networkModList : ");
                foreach (var mod in sortedModList) {
                    R2API.Logger.LogInfo(mod);
                    NetworkModCompatibilityHelper.networkModList = NetworkModCompatibilityHelper.networkModList.Append(mod);
                }
            }
        }

        private static bool IsR2APIAffectingNetwork() {
            return APISubmoduleHandler.IsLoaded(nameof(NetworkingAPI));
        }
    }
}
