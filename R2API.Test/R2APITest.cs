global using UnityObject = UnityEngine.Object;
using BepInEx;
using BepInEx.Logging;
using IL.RoR2;
using UnityEngine;


[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace R2API.Test;

[BepInDependency(ItemAPI.PluginGUID)]
[BepInDependency(LanguageAPI.PluginGUID)]
[BepInDependency(DirectorAPI.PluginGUID)]
[BepInDependency(EliteAPI.PluginGUID)]
[BepInDependency(SceneAssetAPI.PluginGUID)]
[BepInDependency(CharacterBodyAPI.PluginGUID)]
[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class R2APITest : BaseUnityPlugin
{
    public const string PluginGUID = "com.bepis.r2apitest";
    public const string PluginName = "R2APITest";
    public const string PluginVersion = "0.0.1";
    public static bool enableBodyFlagTesting;
    internal new static ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;

#if !DEBUG
        throw new System.Exception("R2API.DebugMode is not enabled");
#endif

        var awakeRunner = new AwakeRunner();
        awakeRunner.DiscoverAndRun();
        enableBodyFlagTesting = false;
    }
    public class TestAssets
    {
        public static CharacterBodyAPI.ModdedBodyFlag testFlag = CharacterBodyAPI.ReserveBodyFlag();
    }
    public void Update()
    {
        if (!enableBodyFlagTesting) return;
        if (Input.GetKeyDown(KeyCode.Y))
        {
            try
            {
                RoR2.CharacterBody characterBody = RoR2.NetworkUser.readOnlyInstancesList[0].master.GetBody();
                characterBody.AddModdedBodyFlag(TestAssets.testFlag);
                RoR2.Chat.AddMessage("Test flag has been added");
            }
            catch
            {
                RoR2.Chat.AddMessage("failed to add Test flag");
            }
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            try
            {
                RoR2.CharacterBody characterBody = RoR2.NetworkUser.readOnlyInstancesList[0].master.GetBody();
                bool removed = characterBody.RemoveModdedBodyFlag(TestAssets.testFlag);
                RoR2.Chat.AddMessage(removed ? "Test flag has been removed" : "No Test flag to remove");
            }
            catch
            {
                RoR2.Chat.AddMessage("failed to remove Test flag");
            }
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            try
            {
                RoR2.CharacterBody characterBody = RoR2.NetworkUser.readOnlyInstancesList[0].master.GetBody();
                bool removed = characterBody.HasModdedBodyFlag(TestAssets.testFlag);
                RoR2.Chat.AddMessage(removed ? "Has Test flag" : "No Test flag");
            }
            catch
            {
                RoR2.Chat.AddMessage("failed to check Test flag");
            }
        }
    }
}
