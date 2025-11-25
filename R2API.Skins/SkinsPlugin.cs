using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace R2API;

[BepInPlugin(Skins.PluginGUID, Skins.PluginName, Skins.PluginVersion)]
public sealed class SkinsPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    public static Harmony harmonyPatcher;
    private void Awake()
    {
        Logger = base.Logger;
        harmonyPatcher = new Harmony(Skins.PluginGUID);
    }

    private void OnEnable()
    {
        Skins.SetHooks();
        SkinIDRS.SetHooks();
        SkinVFX.SetHooks();
        SkinSkillVariants.SetHooks();
    }

    private void OnDisable()
    {
        Skins.UnsetHooks();
        SkinIDRS.UnsetHooks();
        SkinVFX.UnsetHooks();
        SkinSkillVariants.UnsetHooks();
    }
}
