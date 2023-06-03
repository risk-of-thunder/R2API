global using UnityObject = UnityEngine.Object;
using BepInEx;
using BepInEx.Logging;
using HG.Reflection;
using System;

[assembly: SearchableAttribute.OptIn]

namespace R2API.Test;

[BepInDependency(ItemAPI.PluginGUID)]
[BepInDependency(LanguageAPI.PluginGUID)]
[BepInDependency(DirectorAPI.PluginGUID)]
[BepInDependency(EliteAPI.PluginGUID)]
[BepInDependency(SceneAssetAPI.PluginGUID)]
[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class R2APITest : BaseUnityPlugin
{
    public const string PluginGUID = "com.bepis.r2apitest";
    public const string PluginName = "R2APITest";
    public const string PluginVersion = "0.0.1";

    internal new static ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;

        if (!R2API.DebugMode)
        {
            throw new Exception("R2API.DebugMode is not enabled");
        }

        var awakeRunner = new AwakeRunner();
        awakeRunner.DiscoverAndRun();
    }
}
