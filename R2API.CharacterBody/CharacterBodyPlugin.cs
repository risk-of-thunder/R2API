using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(CharacterBodyAPI.PluginGUID, CharacterBodyAPI.PluginName, CharacterBodyAPI.PluginVersion)]
public sealed class CharacterBodyPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
        CharacterBodyAPI.SetHooks();
    }

    private void OnDestroy()
    {
        CharacterBodyAPI.UnsetHooks();
    }
}
