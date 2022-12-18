using BepInEx;
using BepInEx.Logging;
using System;

namespace R2API;

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
