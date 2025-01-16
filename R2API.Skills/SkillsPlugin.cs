using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(SkillsAPI.PluginGUID, SkillsAPI.PluginName, SkillsAPI.PluginVersion)]
public sealed class SkillsPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
        SkillsAPI.SetHooks();
    }

    private void OnDestroy()
    {
        SkillsAPI.UnsetHooks();
    }
}
