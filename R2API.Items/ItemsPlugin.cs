using BepInEx;

namespace R2API;

[BepInPlugin(ItemAPI.PluginGUID, ItemAPI.PluginName, ItemAPI.PluginVersion)]
public sealed class ItemsPlugin : BaseUnityPlugin {
    private void OnEnable() {
        ItemAPI.SetHooks();
    }

    private void OnDisable() {
        ItemAPI.UnsetHooks();
    }
}
