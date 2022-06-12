using BepInEx;

namespace R2API;

[BepInPlugin(LanguageAPI.PluginGUID, LanguageAPI.PluginName, LanguageAPI.PluginVersion)]
public sealed class LanguagePlugin : BaseUnityPlugin
{
    private void OnEnable()
    {
        LanguageAPI.SetHooks();
    }

    private void OnDisable()
    {
        LanguageAPI.UnsetHooks();
    }
}
