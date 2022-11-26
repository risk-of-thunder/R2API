using BepInEx;

namespace R2API;

[BepInPlugin(DifficultyAPI.PluginGUID, DifficultyAPI.PluginName, DifficultyAPI.PluginVersion)]
public sealed class DifficultyPlugin : BaseUnityPlugin
{
    private void OnDestroy()
    {
        DifficultyAPI.UnsetHooks();
    }
}
