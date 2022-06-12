using BepInEx;
using System;

namespace R2API;

[BepInPlugin(LoadoutAPI.PluginGUID, LoadoutAPI.PluginName, LoadoutAPI.PluginVersion)]
[Obsolete(LoadoutAPI.ObsoleteMessage)]
public sealed class LoadoutPlugin : BaseUnityPlugin
{
    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }
}
