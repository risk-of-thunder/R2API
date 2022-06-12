using BepInEx;

namespace R2API;

[BepInPlugin(PrefabAPI.PluginGUID, PrefabAPI.PluginName, PrefabAPI.PluginVersion)]
public sealed class PrefabPlugin : BaseUnityPlugin {
    private void OnEnable() {
        PrefabAPI.SetHooks();
    }

    private void OnDisable() {
        PrefabAPI.UnsetHooks();
    }
}
