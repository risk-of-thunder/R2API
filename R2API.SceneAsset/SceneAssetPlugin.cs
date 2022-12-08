using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(SceneAssetAPI.PluginGUID, SceneAssetAPI.PluginName, SceneAssetAPI.PluginVersion)]
public sealed class SceneAssetPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnDestroy()
    {
        SceneAssetAPI.UnsetHooks();
    }
}
