using BepInEx;
using BepInEx.Logging;
using R2API.ContentManagement;
using System;

namespace R2API;

[BepInDependency(R2APIContentManager.PluginGUID)]
[BepInDependency(Skins.PluginGUID)]
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
