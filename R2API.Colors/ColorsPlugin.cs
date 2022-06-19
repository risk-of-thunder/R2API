using BepInEx;

namespace R2API;

[BepInPlugin(ColorsAPI.PluginGUID, ColorsAPI.PluginName, ColorsAPI.PluginVersion)]
public sealed class ColorsPlugin : BaseUnityPlugin
{
    private void OnEnable()
    {
        ColorsAPI.SetHooks();
    }

    private void OnDisable()
    {
        ColorsAPI.UnsetHooks();
    }
}
