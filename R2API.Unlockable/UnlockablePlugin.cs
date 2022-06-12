using BepInEx;

namespace R2API;

[BepInPlugin(UnlockableAPI.PluginGUID, UnlockableAPI.PluginName, UnlockableAPI.PluginVersion)]
public sealed class ItemsPlugin : BaseUnityPlugin {
    private void OnEnable() {
        UnlockableAPI.SetHooks();
    }

    private void OnDisable() {
        UnlockableAPI.UnsetHooks();
    }
}
