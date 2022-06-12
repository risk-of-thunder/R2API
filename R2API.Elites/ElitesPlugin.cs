using BepInEx;

namespace R2API;

[BepInPlugin(EliteAPI.PluginGUID, EliteAPI.PluginName, EliteAPI.PluginVersion)]
public sealed class ElitesPlugin : BaseUnityPlugin
{
    private void OnEnable()
    {
        EliteAPI.SetHooks();
        EliteRamp.SetHooks();
    }

    private void OnDisable()
    {
        EliteAPI.UnsetHooks();
        EliteRamp.UnsetHooks();
    }
}
