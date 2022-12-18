using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(LanguageAPI.PluginGUID, LanguageAPI.PluginName, LanguageAPI.PluginVersion)]
public sealed class LanguagePlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnEnable()
    {
        LanguageAPI.SetHooks();
    }

    private void OnDisable()
    {
        LanguageAPI.UnsetHooks();
    }
}
