using BepInEx;

namespace R2API;

[BepInPlugin(DotAPI.PluginGUID, DotAPI.PluginName, DotAPI.PluginVersion)]
public sealed class DotPlugin : BaseUnityPlugin
{
    private void OnEnable()
    {
        DotAPI.SetHooks();
    }

    private void OnDisable()
    {
        DotAPI.UnsetHooks();
    }
}
