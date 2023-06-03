using BepInEx;
using BepInEx.Logging;
using R2API.ContentManagement;
using System;

namespace R2API;

[BepInDependency(R2APIContentManager.PluginGUID)]
[BepInPlugin(UnlockableAPI.PluginGUID, UnlockableAPI.PluginName, UnlockableAPI.PluginVersion)]
[Obsolete(UnlockableAPI.ObsoleteMessage)]
public sealed class UnlockablePlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnDestroy()
    {
        UnlockableAPI.UnsetHooks();
    }
}
