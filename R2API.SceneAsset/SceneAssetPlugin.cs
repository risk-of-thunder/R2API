using BepInEx;

namespace R2API;

[BepInPlugin(SceneAssetAPI.PluginGUID, SceneAssetAPI.PluginName, SceneAssetAPI.PluginVersion)]
public sealed class SceneAssetPlugin : BaseUnityPlugin
{
    private void OnDestroy()
    {
        SceneAssetAPI.UnsetHooks();
    }
}
