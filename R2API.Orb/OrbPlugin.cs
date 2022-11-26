using BepInEx;

namespace R2API;

[BepInPlugin(OrbAPI.PluginGUID, OrbAPI.PluginName, OrbAPI.PluginVersion)]
public sealed class OrbPlugin : BaseUnityPlugin
{
    private void OnDestroy()
    {
        OrbAPI.UnsetHooks();
    }
}
