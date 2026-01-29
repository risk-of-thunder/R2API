using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(BuffsAPI.PluginGUID, BuffsAPI.PluginName, BuffsAPI.PluginVersion)]
public sealed class BuffsPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
        BuffsAPI.SetHooks();
    }

    private void OnDestroy()
    {
        BuffsAPI.UnsetHooks();
    }
}
