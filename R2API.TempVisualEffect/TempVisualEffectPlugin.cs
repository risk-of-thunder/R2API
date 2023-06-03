using BepInEx;
using BepInEx.Logging;
using R2API.ContentManagement;

namespace R2API;

[BepInDependency(R2APIContentManager.PluginGUID)]
[BepInPlugin(TempVisualEffectAPI.PluginGUID, TempVisualEffectAPI.PluginName, TempVisualEffectAPI.PluginVersion)]
public sealed class TempVisualEffectPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnDestroy()
    {
        TempVisualEffectAPI.UnsetHooks();
    }
}
