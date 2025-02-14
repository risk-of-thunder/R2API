using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(CostAPI.PluginGUID, CostAPI.PluginName, CostAPI.PluginVersion)]
public sealed class CostTypePlugin : BaseUnityPlugin {
    internal static new ManualLogSource Logger { get; set; }

    private void Awake() {
        Logger = base.Logger;

        CostAPI.SetHooks();
    }

    private void OnDestroy() {
        CostAPI.UnsetHooks();
    }
}