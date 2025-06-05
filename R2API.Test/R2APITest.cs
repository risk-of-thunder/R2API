global using UnityObject = UnityEngine.Object;
using BepInEx;
using BepInEx.Logging;
using IL.RoR2;
using UnityEngine;


[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace R2API.Test;

//[BepInDependency(ItemAPI.PluginGUID)]
//[BepInDependency(LanguageAPI.PluginGUID)]
//[BepInDependency(DirectorAPI.PluginGUID)]
//[BepInDependency(EliteAPI.PluginGUID)]
//[BepInDependency(SceneAssetAPI.PluginGUID)]
[BepInDependency(CharacterBodyAPI.PluginGUID)]
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

#if !DEBUG
        throw new System.Exception("R2API.DebugMode is not enabled");
#endif

        //var awakeRunner = new AwakeRunner();
        //awakeRunner.DiscoverAndRun();
    }
    public class TestAssets
    {
        public static CharacterBodyAPI.ModdedBodyFlag krodFlag = CharacterBodyAPI.ReserveBodyFlag();
    }
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            try
            {
                RoR2.CharacterBody characterBody = RoR2.NetworkUser.readOnlyInstancesList[0].master.GetBody();
                characterBody.AddModdedBodyFlag(TestAssets.krodFlag);
                RoR2.Chat.AddMessage("Krod flag has been added");
            }
            catch
            {
                RoR2.Chat.AddMessage("failed to add Krod flag");
            }
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            try
            {
                RoR2.CharacterBody characterBody = RoR2.NetworkUser.readOnlyInstancesList[0].master.GetBody();
                bool removed = characterBody.RemoveModdedBodyFlag(TestAssets.krodFlag);
                RoR2.Chat.AddMessage(removed ? "Krod flag has been remved" : "No Krod flag to remove");
            }
            catch
            {
                RoR2.Chat.AddMessage("failed to remove Krod flag");
            }
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            try
            {
                RoR2.CharacterBody characterBody = RoR2.NetworkUser.readOnlyInstancesList[0].master.GetBody();
                bool removed = characterBody.HasModdedBodyFlag(TestAssets.krodFlag);
                RoR2.Chat.AddMessage(removed ? "Krod" : "No Krod");
            }
            catch
            {
                RoR2.Chat.AddMessage("failed to check Krod flag");
            }
        }
    }
}
