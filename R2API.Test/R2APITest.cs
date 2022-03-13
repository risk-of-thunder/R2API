global using UnityObject = UnityEngine.Object;
using BepInEx;
using BepInEx.Logging;
using HG.Reflection;

[assembly: SearchableAttribute.OptIn]

namespace R2API.Test {
    [BepInDependency(R2API.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class R2APITest : BaseUnityPlugin {
        public const string PluginGUID = "com.bepis.r2apitest";
        public const string PluginName = "R2APITest";
        public const string PluginVersion = "0.0.1";

        internal new static ManualLogSource Logger { get; set; }

        private void Awake() {
            Logger = base.Logger;

            var awakeTestRunner = new AwakeTestRunner();
            awakeTestRunner.DiscoverAndRunTests();
        }
    }
}
