using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(RecalculateStatsAPI.PluginGUID, RecalculateStatsAPI.PluginName, RecalculateStatsAPI.PluginVersion)]
public sealed class RecalculateStatsPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnDestroy()
    {
        RecalculateStatsAPI.UnsetHooks();
    }
}
