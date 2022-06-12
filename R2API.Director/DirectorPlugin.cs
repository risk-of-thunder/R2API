using BepInEx;

namespace R2API;

[BepInPlugin(DirectorAPI.PluginGUID, DirectorAPI.PluginName, DirectorAPI.PluginVersion)]
public sealed class DirectorPlugin : BaseUnityPlugin {
    private void OnEnable() {
        DirectorAPI.SetHooks();
    }

    private void OnDisable() {
        DirectorAPI.UnsetHooks();
    }
}
