using BepInEx;
using BepInEx.Logging;
using System;

namespace R2API;

[BepInPlugin(LoadoutAPI.PluginGUID, LoadoutAPI.PluginName, LoadoutAPI.PluginVersion)]
[Obsolete(LoadoutAPI.ObsoleteMessage)]
public sealed class LoadoutPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }
}
