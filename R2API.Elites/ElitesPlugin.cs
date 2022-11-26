using BepInEx;

namespace R2API;

[BepInPlugin(EliteAPI.PluginGUID, EliteAPI.PluginName, EliteAPI.PluginVersion)]
public sealed class ElitesPlugin : BaseUnityPlugin
{
    private void OnDestroy()
    {
        EliteAPI.UnsetHooks();
        EliteRamp.UnsetHooks();
    }
}
