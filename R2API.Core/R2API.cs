using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using R2API.AutoVersionGen;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace R2API;

// This is for R2API.Legacy
[BepInDependency(PluginGUID + ".artifactcode", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".commandhelper", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".content_management", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".damagetype", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".deployable", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".difficulty", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".director", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".dot", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".elites", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".items", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".language", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".loadout", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".lobbyconfig", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".networking", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".orb", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".prefab", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".lobbyconfig", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".recalculatestats", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".sceneasset", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".sound", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".tempvisualeffect", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(PluginGUID + ".unlockable", BepInDependency.DependencyFlags.SoftDependency)]

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[AutoVersion]
// ReSharper disable once InconsistentNaming
public partial class R2API : BaseUnityPlugin
{
    // ReSharper disable once InconsistentNaming
    public const string PluginGUID = "com.bepis.r2api";

    public const string PluginName = "R2API";

    private const string GameBuildId = "1.2.4.1";

    internal static new ManualLogSource Logger { get; set; }
    public static bool DebugMode { get; private set; } = false;

    internal static DetourModManager ModManager;

    internal static event EventHandler R2APIStart;

    internal static HashSet<string> LoadedSubmodules;

    internal static R2API Instance { get; private set; }

    private NetworkCompatibilityHandler _networkCompatibilityHandler;

    public void Awake()
    {
        Instance = this;

        Logger = base.Logger;

        _networkCompatibilityHandler = new NetworkCompatibilityHandler();
        _networkCompatibilityHandler.BuildModList();

        ModManager = new DetourModManager();
        AddHookLogging();


        CheckForIncompatibleAssemblies();

        On.RoR2.RoR2Application.Awake += CheckIfUsedOnRightGameVersion;
    }

    public void OnDestroy()
    {
        _networkCompatibilityHandler.CleanupModList();
    }

    private static void DebugUpdate()
    {

    }

    /// <summary>
    /// Logs caller information along side debug message
    /// </summary>
    /// <param name="debugText"></param>
    /// <param name="caller"></param>
    public static void LogDebug(object debugText, [CallerMemberName] string caller = "")
    {
        Logger.LogDebug(caller + " : " + debugText.ToString());
    }

    private void CheckIfUsedOnRightGameVersion(On.RoR2.RoR2Application.orig_Awake orig, RoR2Application self)
    {
        orig(self);

        var buildId = Application.version;

        if (GameBuildId == buildId)
        {
            return;
        }

        Logger.LogWarning($"This version of R2API was built for build id \"{GameBuildId}\", you are running \"{buildId}\".");
        Logger.LogWarning("Should any problems arise, please check for a new version before reporting issues.");
    }

    public void Start()
    {
        R2APIStart?.Invoke(this, null);
    }

    public void Update()
    {
        if (DebugMode)
        {
            DebugUpdate();
        }
    }

    /// <summary>
    /// Return true if the specified submodule is loaded.
    /// </summary>
    /// <param name="submodule">nameof the submodule</param>
    public static bool IsLoaded(string submodule)
    {
        if (LoadedSubmodules == null)
        {
            Logger.LogWarning("IsLoaded called before submodules were loaded, result may not reflect actual load status.");
            return false;
        }
        return LoadedSubmodules.Contains(submodule);
    }

    private static void AddHookLogging()
    {
        ModManager.OnHook += (hookOwner, @base, _, __) => LogMethod(@base, hookOwner);
        ModManager.OnDetour += (hookOwner, @base, _) => LogMethod(@base, hookOwner);
        ModManager.OnNativeDetour += (hookOwner, @base, _, __) => LogMethod(@base, hookOwner);
        ModManager.OnILHook += (hookOwner, @base, _) => LogMethod(@base, hookOwner);

        HookEndpointManager.OnAdd += (@base, @delegate) => LogMethod(@base, @delegate.Method.Module.Assembly);
        HookEndpointManager.OnModify += (@base, @delegate) => LogMethod(@base, @delegate.Method.Module.Assembly);
        HookEndpointManager.OnRemove += (@base, @delegate) => LogMethod(@base, @delegate.Method.Module.Assembly, false);
    }

    private static bool LogMethod(MemberInfo @base, Assembly hookOwnerAssembly, bool added = true)
    {
        if (@base == null)
        {
            return true;
        }

        var hookOwnerDllName = "Not Found";
        if (hookOwnerAssembly != null)
        {
            // Get the dll name instead of assembly manifest name as this one one could be not correctly set by mod maker.
            hookOwnerDllName = System.IO.Path.GetFileName(hookOwnerAssembly.Location);
        }

        var declaringType = @base.DeclaringType;
        var name = @base.Name;
        var identifier = declaringType != null ? $"{declaringType}.{name}" : name;

        Logger.LogDebug($"Hook {(added ? "added" : "removed")} by assembly: {hookOwnerDllName} for: {identifier}");
        return true;
    }

    public static bool SupportsVersion(string? version)
    {
        var own = Version.Parse(PluginVersion);
        var v = Version.Parse(version);

        return own.Major == v.Major && own.Minor <= v.Minor;
    }
}
