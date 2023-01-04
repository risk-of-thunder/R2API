using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(EliteAPI.PluginGUID, EliteAPI.PluginName, EliteAPI.PluginVersion)]
public sealed class ElitesPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;

        EliteAPI.SetHooks();
        EliteRamp.SetHooks();
    }

    private void OnDestroy()
    {
        EliteAPI.UnsetHooks();
        EliteRamp.UnsetHooks();
    }
}
