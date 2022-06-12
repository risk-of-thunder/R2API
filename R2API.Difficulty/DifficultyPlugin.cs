using BepInEx;

namespace R2API;

[BepInPlugin(DifficultyAPI.PluginGUID, DifficultyAPI.PluginName, DifficultyAPI.PluginVersion)]
public sealed class DifficultyPlugin : BaseUnityPlugin {
    private void OnEnable() {
        DifficultyAPI.SetHooks();
    }

    private void OnDisable() {
        DifficultyAPI.UnsetHooks();
    }
}
